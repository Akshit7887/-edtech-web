using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using EdTechApi.Data;
using EdTechApi.Middleware;
using EdTechApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Secrets from environment variables (override placeholder config) ──
string GetConfigOrEnv(string configKey, string envKey, string? defaultValue = null)
{
    var val = builder.Configuration[configKey];
    if (!string.IsNullOrEmpty(val)) return val;
    val = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrEmpty(val)) return val;
    return defaultValue ?? throw new InvalidOperationException($"Missing configuration: set {envKey} env var or {configKey} in config.");
}

var dbConnectionString = GetConfigOrEnv("ConnectionStrings:Supabase", "SUPABASE_CONNECTION_STRING");
var jwtSecret = GetConfigOrEnv("Jwt:Secret", "JWT_SECRET");
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? builder.Configuration["Gemini:ApiKey"] ?? "";
var sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? builder.Configuration["SendGrid:ApiKey"] ?? "";

// Override config with env var values for downstream services
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Gemini:ApiKey"] = geminiApiKey;
builder.Configuration["SendGrid:ApiKey"] = sendGridApiKey;
builder.Configuration["ConnectionStrings:Supabase"] = dbConnectionString;

// ── Database ──
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(dbConnectionString));

// ── Services ──
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

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

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict policy for auth endpoints
    options.AddFixedWindowLimiter("AuthPolicy", cfg =>
    {
        cfg.PermitLimit = 5;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });

    // Moderate policy for general API
    options.AddFixedWindowLimiter("ApiPolicy", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 5;
    });

    // Strict policy for OTP verification
    options.AddFixedWindowLimiter("OtpPolicy", cfg =>
    {
        cfg.PermitLimit = 10;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
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

// ── Middleware pipeline ──
app.UseRequestId();
app.UseRateLimiter();
app.UseCors();
app.UseErrorHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseJwtMiddleware();
app.MapControllers();

// ── Health check ──
app.MapGet("/api/health", (HttpContext context) =>
{
    return Results.Ok(new
    {
        status = "ok",
        message = "EdTech Examination API is running (ASP.NET Core)",
        timestamp = DateTime.UtcNow,
        requestId = context.Items["RequestId"]?.ToString()
    });
});

// ── Startup info ──
app.Logger.LogInformation("EdTech API starting on {Urls}", string.Join(", ", app.Urls));
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

app.Run();
