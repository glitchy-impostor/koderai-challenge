using DebateScoringEngine.Api.Services;
using DebateScoringEngine.Core.Scoring;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Serialize/deserialize enums as strings (e.g. "AFF" not 0)
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Core engine services — singletons (stateless, thread-safe)
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<ScoringEngine>(_ => new ScoringEngine());

// LLM services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILlmProvider, AnthropicProvider>();
builder.Services.AddSingleton<ILlmProvider, OpenAiProvider>();
builder.Services.AddSingleton<LlmEnrichmentService>();

// CORS — allow localhost frontend (React dev server on :5173 or :3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Startup validation ────────────────────────────────────────────────────────

var configService = app.Services.GetRequiredService<ConfigService>();
var (ok, errors) = configService.ValidateConfigFiles();
if (!ok)
{
    foreach (var e in errors)
        app.Logger.LogWarning("Config warning: {Error}", e);
}
else
{
    app.Logger.LogInformation("All config files loaded successfully.");
}

// ── Middleware ────────────────────────────────────────────────────────────────

app.UseCors("LocalFrontend");
app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status  = "ok",
    version = "1.0.0",
    configs = ok ? "loaded" : "missing"
}));

app.Run();
