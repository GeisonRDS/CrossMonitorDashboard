using System.Text.Json;
using CrossMonitorDashboard.Api.Models;
using CrossMonitorDashboard.Api.Services;

var vueDistPath = Environment.GetEnvironmentVariable("DASHBOARD_WEB_ROOT");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = vueDistPath
});

var configPath = Environment.GetEnvironmentVariable("DASHBOARD_CONFIG_PATH")
    ?? Path.Combine("config", "dashboard.json");

builder.Services.AddSingleton(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new ConfigService(configPath, loggerFactory);
});
builder.Services.AddHttpClient("NodePoller");
builder.Services.AddSingleton<NodePollingService>();
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NodePollingService>());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

var apiRoot = app.MapGroup("/api/dashboard");

apiRoot.MapGet("/nodes", (DashboardService ds) =>
{
    return Results.Ok(ds.GetNodeSummaries());
});

apiRoot.MapGet("/nodes/{id}", (string id, DashboardService ds) =>
{
    var details = ds.GetNodeDetails(id);
    if (details == null)
        return Results.NotFound(new { error = "Node not found" });
    return Results.Ok(details);
});

apiRoot.MapGet("/nodes/{id}/history", (string id, DashboardService ds) =>
{
    var history = ds.GetNodeHistory(id);
    return Results.Ok(history);
});

apiRoot.MapGet("/summary", (DashboardService ds) =>
{
    return Results.Ok(ds.ComputeSummary());
});

apiRoot.MapGet("/themes", () =>
{
    return Results.Ok(new[] { "glass-blue", "neon-green", "cyber-red", "terminal-green", "pixel-platformer", "terminal-mono", "terminal-blue", "terminal-red", "terminal-green-matte", "material-slate", "material-graphite", "material-ocean", "material-forest", "hacker-prompt", "code-editor" });
});

apiRoot.MapGet("/config/public", (ConfigService cs) =>
{
    return Results.Ok(cs.GetPublicConfig());
});

apiRoot.MapPost("/config/validate", (JsonElement body) =>
{
    var errors = new List<string>();

    try
    {
        var config = JsonSerializer.Deserialize<DashboardConfig>(body.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            errors.Add("Invalid configuration: could not deserialize");
        }
        else
        {
            if (config.Nodes == null || config.Nodes.Count == 0)
                errors.Add("At least one node is required");

            if (config.Nodes != null)
            {
                foreach (var node in config.Nodes)
                {
                    if (string.IsNullOrWhiteSpace(node.Id))
                        errors.Add("Node missing required field: id");
                    if (string.IsNullOrWhiteSpace(node.Name))
                        errors.Add("Node missing required field: name");
                    if (string.IsNullOrWhiteSpace(node.Url))
                        errors.Add($"Node '{node.Id}' missing required field: url");
                    else if (!Uri.TryCreate(node.Url, UriKind.Absolute, out _))
                        errors.Add($"Node '{node.Id}' has invalid URL: {node.Url}");
                    if (string.IsNullOrWhiteSpace(node.Token))
                        errors.Add($"Node '{node.Id}' missing required field: token");
                }
            }

            if (config.Visual == null)
                errors.Add("Visual configuration is required");
        }
    }
    catch (JsonException ex)
    {
        errors.Add($"JSON parse error: {ex.Message}");
    }

    return Results.Ok(new ValidationResult
    {
        Valid = errors.Count == 0,
        Errors = errors
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Logger.LogInformation("WebRootPath: {Path}", app.Environment.WebRootPath);
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

public record ValidationResult
{
    public bool Valid { get; init; }
    public List<string> Errors { get; init; } = new();
}
