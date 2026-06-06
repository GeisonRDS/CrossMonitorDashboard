using System.Text.Json;
using CrossMonitorDashboard.Api.Models;

namespace CrossMonitorDashboard.Api.Services;

public class ConfigService
{
    private DashboardConfig _config;
    private readonly string _configPath;
    private readonly ILogger _logger;

    public ConfigService(string configPath, ILoggerFactory loggerFactory)
    {
        _configPath = configPath;
        _logger = loggerFactory.CreateLogger<ConfigService>();
        _config = new DashboardConfig();
        LoadConfig();
    }

    private void LoadConfig()
    {
        var fallbackPath = Path.Combine(
            Path.GetDirectoryName(_configPath) ?? "config",
            "dashboard.example.json"
        );

        var path = _configPath;
        if (!File.Exists(path))
        {
            _logger.LogWarning("Config file not found at {Path}, trying fallback: {Fallback}", path, fallbackPath);
            path = fallbackPath;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("No config file found at {Path} or {Fallback}. Using defaults.", _configPath, fallbackPath);
            _config = new DashboardConfig();
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            _config = JsonSerializer.Deserialize<DashboardConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DashboardConfig();

            _logger.LogInformation("Loaded config from {Path} with {NodeCount} node(s)", path, _config.Nodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config from {Path}", path);
            _config = new DashboardConfig();
        }
    }

    public VisualConfig GetPublicConfig()
    {
        return _config.Visual;
    }

    public List<NodeConfig> GetNodes()
    {
        return _config.Nodes.Where(n => n.Enabled).ToList();
    }
}
