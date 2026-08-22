namespace EdTechApi.Services;

public enum CircuitState { Closed, Open, HalfOpen }

public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> action, int failureThreshold = 3, TimeSpan? openDuration = null, int halfOpenMaxRequests = 1);
    Task<CircuitState> GetStateAsync(string circuitName);
    Task<IReadOnlyList<object>> GetDeadLetteredAsync(string queueName, int count = 50);
    Task RequeueDeadLetteredAsync(string queueName, int count = 10);
}

public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<CircuitBreakerService> _logger;
    private static readonly TimeSpan DefaultOpenDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HalfOpenTimeout = TimeSpan.FromSeconds(60);

    public CircuitBreakerService(IRedisCacheService cache, ILogger<CircuitBreakerService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> action, int failureThreshold = 3, TimeSpan? openDuration = null, int halfOpenMaxRequests = 1)
    {
        var state = await GetStateAsync(circuitName);
        var openFor = openDuration ?? DefaultOpenDuration;

        if (state == CircuitState.Open)
        {
            var openedAt = await _cache.GetValueAsync<long>($"cb:{circuitName}:opened_at");
            if (openedAt.HasValue)
            {
                var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - openedAt.Value;
                if (elapsed < openFor.TotalSeconds)
                {
                    throw new CircuitBreakerOpenException($"Circuit '{circuitName}' is open. Retry after {openFor.TotalSeconds - elapsed:F0}s");
                }
                
                // Open duration expired - transition to HalfOpen
                await _cache.RemoveAsync($"cb:{circuitName}:opened_at");
                await _cache.SetValueAsync($"cb:{circuitName}:half_open", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), HalfOpenTimeout);
                await _cache.SetValueAsync($"cb:{circuitName}:half_open_requests", 0, HalfOpenTimeout);
                state = CircuitState.HalfOpen;
                _logger.LogInformation("Circuit '{Circuit}' transitioned to HalfOpen", circuitName);
            }
        }

        if (state == CircuitState.HalfOpen)
        {
            var halfOpenStarted = await _cache.GetValueAsync<long>($"cb:{circuitName}:half_open");
            if (!halfOpenStarted.HasValue)
            {
                // HalfOpen expired, reset to Closed
                await ResetCircuitAsync(circuitName);
            }
            else
            {
                var requests = await _cache.GetValueAsync<int>($"cb:{circuitName}:half_open_requests") ?? 0;
                if (requests >= halfOpenMaxRequests)
                {
                    throw new CircuitBreakerOpenException($"Circuit '{circuitName}' is in HalfOpen state. Max test requests ({halfOpenMaxRequests}) reached.");
                }
                
                // Increment half-open request counter
                await _cache.SetValueAsync($"cb:{circuitName}:half_open_requests", requests + 1, HalfOpenTimeout);
            }
        }

        try
        {
            var result = await action();
            
            if (state == CircuitState.HalfOpen)
            {
                // Success in HalfOpen - close the circuit
                await ResetCircuitAsync(circuitName);
                _logger.LogInformation("Circuit '{Circuit}' closed after successful HalfOpen request", circuitName);
            }
            else
            {
                // Success in Closed state - reset failure count
                await _cache.RemoveAsync($"cb:{circuitName}:failures");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Circuit '{Circuit}' failed", circuitName);
            
            if (state == CircuitState.HalfOpen)
            {
                // Failure in HalfOpen - reopen circuit
                await _cache.RemoveAsync($"cb:{circuitName}:half_open");
                await _cache.RemoveAsync($"cb:{circuitName}:half_open_requests");
                await _cache.SetValueAsync($"cb:{circuitName}:opened_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), openFor.Multiply(2));
                _logger.LogWarning("Circuit '{Circuit}' reopened after HalfOpen failure", circuitName);
            }
            else
            {
                // Failure in Closed state - increment failure count
                var failures = 1;
                var existingFailures = await _cache.GetValueAsync<int>($"cb:{circuitName}:failures");
                if (existingFailures.HasValue) failures = existingFailures.Value + 1;

                await _cache.SetValueAsync($"cb:{circuitName}:failures", failures, openFor.Multiply(2));
                
                if (failures >= failureThreshold)
                {
                    await _cache.SetValueAsync($"cb:{circuitName}:opened_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), openFor.Multiply(2));
                    await _cache.RemoveAsync($"cb:{circuitName}:failures");
                    _logger.LogWarning("Circuit '{Circuit}' opened ({Failures}/{Threshold} failures)", circuitName, failures, failureThreshold);
                }
            }
            throw;
        }
    }

    private async Task ResetCircuitAsync(string circuitName)
    {
        await _cache.RemoveAsync($"cb:{circuitName}:failures");
        await _cache.RemoveAsync($"cb:{circuitName}:opened_at");
        await _cache.RemoveAsync($"cb:{circuitName}:half_open");
        await _cache.RemoveAsync($"cb:{circuitName}:half_open_requests");
    }

    public async Task<CircuitState> GetStateAsync(string circuitName)
    {
        var openedAt = await _cache.GetValueAsync<long>($"cb:{circuitName}:opened_at");
        if (openedAt.HasValue)
        {
            // Check if open duration has expired (would transition to HalfOpen on next call)
            return CircuitState.Open;
        }
        
        var halfOpenStarted = await _cache.GetValueAsync<long>($"cb:{circuitName}:half_open");
        if (halfOpenStarted.HasValue)
        {
            return CircuitState.HalfOpen;
        }
        
        return CircuitState.Closed;
    }

    public async Task<IReadOnlyList<object>> GetDeadLetteredAsync(string queueName, int count = 50)
    {
        if (!_cache.IsConnected) return Array.Empty<object>();
        var items = new List<object>();
        for (int i = 0; i < count; i++)
        {
            var val = await _cache.GetAsync<string>($"dlq:{queueName}:{i}");
            if (val == null) break;
            items.Add(new { index = i, payload = val });
        }
        return items;
    }

    public async Task RequeueDeadLetteredAsync(string queueName, int count = 10)
    {
        if (!_cache.IsConnected) return;
        for (int i = 0; i < count; i++)
        {
            var val = await _cache.GetAsync<string>($"dlq:{queueName}:{i}");
            if (val == null) break;
            await _cache.EnqueueAsync($"queue:{queueName}", val);
            await _cache.RemoveAsync($"dlq:{queueName}:{i}");
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string msg) : base(msg) { }
}

internal static class TimeSpanExtensions
{
    internal static TimeSpan Multiply(this TimeSpan span, int factor) => TimeSpan.FromTicks(span.Ticks * factor);
}
