using EdTechApi.Data;
using EdTechApi.Middleware;
using EdTechApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──
var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException("Connection string 'Supabase' not found");

builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));

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

// ── Controllers ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ──
app.UseRequestId();
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
