namespace EdTechApi.Services;

public enum CircuitState { Closed, Open, HalfOpen }

public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> action, int failureThreshold = 3, TimeSpan? openDuration = null);
    Task<CircuitState> GetStateAsync(string circuitName);
    Task<IReadOnlyList<object>> GetDeadLetteredAsync(string queueName, int count = 50);
    Task RequeueDeadLetteredAsync(string queueName, int count = 10);
}

public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<CircuitBreakerService> _logger;
    private static readonly TimeSpan DefaultOpenDuration = TimeSpan.FromSeconds(30);
    private static readonly Random _random = new();

    public CircuitBreakerService(IRedisCacheService cache, ILogger<CircuitBreakerService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> action, int failureThreshold = 3, TimeSpan? openDuration = null)
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
                    throw new CircuitBreakerOpenException($"Circuit '{circuitName}' is open. Retry after {openFor.TotalSeconds - elapsed:F0}s");
                await _cache.RemoveAsync($"cb:{circuitName}:opened_at");
            }
        }

        try
        {
            var result = await action();
            await _cache.RemoveAsync($"cb:{circuitName}:failures");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Circuit '{Circuit}' failed", circuitName);
            var failures = 1;
            var existingFailures = await _cache.GetValueAsync<int>($"cb:{circuitName}:failures");
            if (existingFailures.HasValue) failures = existingFailures.Value + 1;

            await _cache.SetValueAsync($"cb:{circuitName}:failures", failures, openFor.Multiply(2));
            if (failures >= failureThreshold)
            {
                await _cache.SetValueAsync($"cb:{circuitName}:opened_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), openFor.Multiply(2));
                _logger.LogWarning("Circuit '{Circuit}' opened ({Failures}/{Threshold} failures)", circuitName, failures, failureThreshold);
            }
            throw;
        }
    }

    public async Task<CircuitState> GetStateAsync(string circuitName)
    {
        var openedAt = await _cache.GetValueAsync<long>($"cb:{circuitName}:opened_at");
        return openedAt.HasValue ? CircuitState.Open : CircuitState.Closed;
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
