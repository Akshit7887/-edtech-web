using StackExchange.Redis;

namespace EdTechApi.Services;

public interface IRedisCacheService
{
    Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window);
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<T?> GetValueAsync<T>(string key) where T : struct;
    Task SetAsync<T>(string key, T value, TimeSpan expiry) where T : class;
    Task SetValueAsync<T>(string key, T value, TimeSpan expiry) where T : struct;
    Task RemoveAsync(string key);
    Task EnqueueAsync(string queue, string message);
    Task<string?> DequeueAsync(string queue, TimeSpan? timeout = null);
    Task<long> GetQueueLengthAsync(string queue);
    bool IsConnected { get; }
}

public class RedisCacheService : IRedisCacheService, IDisposable
{
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisCacheService> _logger;
    public bool IsConnected => _redis?.IsConnected == true;

    public RedisCacheService(IConfiguration config, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        var connStr = config["Redis:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connStr))
        {
            logger.LogWarning("Redis not configured — rate limiting will fall back to in-memory");
            _redis = null;
            _db = null;
            return;
        }
        try
        {
            _redis = ConnectionMultiplexer.Connect(connStr);
            _db = _redis.GetDatabase();
            logger.LogInformation("Connected to Redis");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Redis — rate limiting will fall back to in-memory");
            _redis = null;
            _db = null;
        }
    }

    public async Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window)
    {
        if (_db == null) return true;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowKey = $"ratelimit:{key}";
        var cleanupKey = $"ratelimit:{key}:cleanup";

        var result = await _db.ScriptEvaluateAsync(@"
            local key = KEYS[1]
            local cleanup = KEYS[2]
            local now = tonumber(ARGV[1])
            local window = tonumber(ARGV[2])
            local limit = tonumber(ARGV[3])
            local cutoff = now - window

            redis.call('ZREMRANGEBYSCORE', cleanup, 0, cutoff)
            local count = redis.call('ZCARD', cleanup)
            if count >= limit then
                return 0
            end
            redis.call('ZADD', cleanup, now, now .. ':' .. math.random())
            redis.call('EXPIRE', cleanup, window)
            return limit - count - 1
        ", new RedisKey[] { windowKey, cleanupKey }, new RedisValue[] { now, (long)window.TotalSeconds, limit });

        var remaining = (int)(long)result;
        return remaining >= 0;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_db == null) return null;
        var val = await _db.StringGetAsync($"cache:{key}");
        return val.HasValue ? System.Text.Json.JsonSerializer.Deserialize<T>(val.ToString()) : null;
    }

    public async Task<T?> GetValueAsync<T>(string key) where T : struct
    {
        if (_db == null) return null;
        var val = await _db.StringGetAsync($"cache:{key}");
        if (!val.HasValue) return null;
        return System.Text.Json.JsonSerializer.Deserialize<T>(val.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry) where T : class
    {
        if (_db == null) return;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await _db.StringSetAsync($"cache:{key}", json, expiry);
    }

    public async Task SetValueAsync<T>(string key, T value, TimeSpan expiry) where T : struct
    {
        if (_db == null) return;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await _db.StringSetAsync($"cache:{key}", json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        if (_db == null) return;
        await _db.KeyDeleteAsync($"cache:{key}");
    }

    public async Task EnqueueAsync(string queue, string message)
    {
        if (_db == null) return;
        await _db.ListRightPushAsync(queue, message);
    }

    public async Task<string?> DequeueAsync(string queue, TimeSpan? timeout = null)
    {
        if (_db == null) return null;
        var result = await _db.ListLeftPopAsync(queue);
        return result.HasValue ? result.ToString() : null;
    }

    public async Task<long> GetQueueLengthAsync(string queue)
    {
        if (_db == null) return 0;
        return await _db.ListLengthAsync(queue);
    }

    public void Dispose() => _redis?.Dispose();
}
