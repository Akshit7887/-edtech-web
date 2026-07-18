using System.Reflection;
using System.Text.Json.Serialization;
using Dapper;
using EdTechApi.Data;
using EdTechApi.Hubs;
using EdTechApi.Middleware;
using EdTechApi.Services;
using EdTechApi.Models;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// ── Observability: Sentry ──
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? builder.Configuration["Sentry:Dsn"] ?? "";
if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = sentryDsn;
        o.TracesSampleRate = 0.1;
        o.ProfilesSampleRate = 0.1;
        o.Environment = builder.Environment.EnvironmentName;
    });
}

// ── App metrics ──
var appMeter = new Meter("EdTechApi", "1.0.0");
var requestCounter = appMeter.CreateCounter<long>("http.requests.total", description: "Total HTTP requests");
var requestDuration = appMeter.CreateHistogram<double>("http.requests.duration_ms", "ms", "Request duration");
var rateLimitHits = appMeter.CreateCounter<long>("ratelimit.hits.total", description: "Rate limit rejection count");

// ── Secrets from environment variables (override placeholder config) ──
string GetConfigOrEnv(string configKey, string envKey, string? defaultValue = null)
{
    var val = builder.Configuration[configKey];
    if (!string.IsNullOrEmpty(val)) return val;
    val = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrEmpty(val)) return val;
    return defaultValue ?? throw new InvalidOperationException($"Missing configuration: set {envKey} env var or {configKey} in config.");
}

// Fallback from old Supabase-era env var name for migration period
var dbConnectionString = GetConfigOrEnv("ConnectionStrings:Neon", "NEON_CONNECTION_STRING", null);
if (string.IsNullOrEmpty(dbConnectionString))
{
    dbConnectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Missing connection string: set NEON_CONNECTION_STRING env var or ConnectionStrings:Neon in config.");
}
var dbReplicaConnectionString = builder.Configuration["ConnectionStrings:NeonReplica"]
    ?? Environment.GetEnvironmentVariable("NEON_REPLICA_CONNECTION_STRING");
if (string.IsNullOrEmpty(dbReplicaConnectionString))
    dbReplicaConnectionString = null;
var jwtSecret = GetConfigOrEnv("Jwt:Secret", "JWT_SECRET");
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? builder.Configuration["Gemini:ApiKey"] ?? "";
var sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? builder.Configuration["SendGrid:ApiKey"] ?? "";
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? builder.Configuration["Google:ClientId"] ?? "";
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["Google:ClientSecret"] ?? "";

// Override config with env var values for downstream services
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Gemini:ApiKey"] = geminiApiKey;
builder.Configuration["SendGrid:ApiKey"] = sendGridApiKey;
builder.Configuration["ConnectionStrings:Neon"] = dbConnectionString;
builder.Configuration["Google:ClientId"] = googleClientId;
builder.Configuration["Google:ClientSecret"] = googleClientSecret;

// ── Dapper type maps ──
SqlMapper.SetTypeMap(typeof(SyllabusFile), new CustomPropertyTypeMap(
    typeof(SyllabusFile),
    (type, columnName) => type.GetProperties().FirstOrDefault(p =>
        p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == columnName)
        ?? throw new InvalidOperationException($"No property mapped to column '{columnName}'")));

// ── Database ──
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(dbConnectionString, dbReplicaConnectionString));

// ── Services ──
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<ISyllabusService, SyllabusService>();
builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
// ── SignalR ──
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 128 * 1024;
});
builder.Services.AddSingleton<IHubService, HubService>();

builder.Services.AddHttpContextAccessor();

// ── Redis Cache ──
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

// ── CORS ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:8081", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Controllers with validation ──
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
    .ConfigureApiBehaviorOptions(opt => opt.SuppressModelStateInvalidFilter = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

var app = builder.Build();
var _startTime = DateTime.UtcNow;

// ── Metrics middleware (captures duration + count for every request) ──
app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        sw.Stop();
        requestCounter.Add(1);
        requestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("path", context.Request.Path),
            new KeyValuePair<string, object?>("status", context.Response.StatusCode));
    }
});

// ── Middleware pipeline ──
app.UseRequestId();
app.UseDistributedRateLimiter();
app.UseCors();
app.UseErrorHandler();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseJwtMiddleware();
app.MapControllers();

// ── SignalR Hubs ──
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<ExamHub>("/hubs/exam");
app.MapHub<NotificationHub>("/hubs/notification");

// ── Health checks ──
app.MapGet("/api/health/live", () => Results.Ok(new { status = "alive" }));

app.MapGet("/api/health/ready", async (HttpContext context, IDbConnectionFactory db) =>
{
    var checks = new Dictionary<string, string>();
    var healthy = true;

    try
    {
        using var conn = db.CreateConnection();
        await conn.OpenAsync();
        checks["database"] = "ok";
    }
    catch (Exception ex)
    {
        checks["database"] = $"error: {ex.Message}";
        healthy = false;
    }

    try
    {
        var cache = context.RequestServices.GetService<IRedisCacheService>();
        if (cache != null)
            checks["redis"] = cache.IsConnected ? "ok" : "not configured";
    }
    catch (Exception ex)
    {
        checks["redis"] = $"error: {ex.Message}";
    }

    return healthy
        ? Results.Ok(new { status = "healthy", checks, timestamp = DateTime.UtcNow })
        : Results.StatusCode(503);
});

// ── Metrics endpoint ──
app.MapGet("/api/metrics", () =>
{
    var meters = new Dictionary<string, object>
    {
        ["app"] = "EdTechApi",
        ["version"] = "1.0.0",
        ["uptime"] = (DateTime.UtcNow - _startTime).TotalSeconds
    };
    return Results.Ok(meters);
});

// Legacy health endpoint (alias)
app.MapGet("/api/health", async (HttpContext context, IDbConnectionFactory db) =>
{
    try
    {
        using var conn = db.CreateConnection();
        await conn.OpenAsync();
        return Results.Ok(new
        {
            status = "ok",
            message = "EdTech Examination API is running (ASP.NET Core)",
            timestamp = DateTime.UtcNow,
            requestId = context.Items["RequestId"]?.ToString()
        });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// ── Run migrations ──
using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<IMigrationService>();
    await migration.ApplyMigrationsAsync();
}

// ── Startup info ──
app.Logger.LogInformation("EdTech API starting on {Urls}", string.Join(", ", app.Urls));
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

app.Run();
