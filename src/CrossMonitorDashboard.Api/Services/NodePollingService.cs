using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using CrossMonitorDashboard.Api.Models;

namespace CrossMonitorDashboard.Api.Services;

public class NodePollingService : BackgroundService
{
    private readonly ConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NodePollingService> _logger;
    private readonly ConcurrentDictionary<string, NodeState> _nodeStates = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MaxHistoryPoints = 120;
    private const int HttpTimeoutSeconds = 5;

    public NodePollingService(
        ConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<NodePollingService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public NodeState? GetNodeState(string id)
    {
        _nodeStates.TryGetValue(id, out var state);
        return state;
    }

    public List<NodeSummary> GetAllNodeSummaries()
    {
        return _nodeStates.Values
            .Select(ns => ns.Summary)
            .ToList();
    }

    public NodeDetails? GetNodeDetails(string id)
    {
        if (_nodeStates.TryGetValue(id, out var state))
            return state.Details;
        return null;
    }

    public List<HistoryDataPoint> GetNodeHistory(string id)
    {
        if (_nodeStates.TryGetValue(id, out var state))
            return state.History.ToList();
        return new List<HistoryDataPoint>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NodePollingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nodes = _configService.GetNodes();
            var refreshSeconds = _configService.GetPublicConfig().RefreshSeconds;

            var pollingTasks = nodes.Select(node => PollNodeAsync(node, stoppingToken));
            await Task.WhenAll(pollingTasks);

            await Task.Delay(TimeSpan.FromSeconds(refreshSeconds), stoppingToken);
        }
    }

    private async Task PollNodeAsync(NodeConfig node, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("NodePoller");
            client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
            client.BaseAddress = new Uri(node.Url.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", node.Token);

            var systemTask = FetchJsonAsync<CrossMonitorSystem>(client, "/api/v1/system", node.Id, ct);
            var statusTask = FetchJsonAsync<CrossMonitorStatus>(client, "/api/v1/status", node.Id, ct);
            var versionTask = FetchJsonAsync<CrossMonitorVersion>(client, "/api/v1/version", node.Id, ct);

            await Task.WhenAll(systemTask, statusTask, versionTask);

            var system = systemTask.Result;
            var status = statusTask.Result;
            var version = versionTask.Result;

            if (system == null || status == null)
            {
                MarkNodeOffline(node.Id, "Invalid response from node");
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var summary = new NodeSummary
            {
                Id = node.Id,
                Name = node.Name,
                Type = node.Type,
                Online = true,
                Status = ComputeStatus(status),
                Os = system.Os,
                Platform = system.Platform,
                CpuUsagePercent = status.Cpu.UsagePercent,
                MemoryUsagePercent = status.Memory.UsagePercent,
                PrimaryDiskUsagePercent = status.Disks.Count > 0 ? status.Disks[0].UsagePercent : 0,
                PrimaryTemperatureCelsius = status.Temperatures.Count > 0 ? status.Temperatures[0].Celsius : 0,
                LastUpdateUnix = now,
                LastError = status.LastError,
                AgentVersion = version?.Version ?? ""
            };

            var details = new NodeDetails
            {
                Id = summary.Id,
                Name = summary.Name,
                Type = summary.Type,
                Online = summary.Online,
                Status = summary.Status,
                Os = summary.Os,
                Platform = summary.Platform,
                CpuUsagePercent = summary.CpuUsagePercent,
                MemoryUsagePercent = summary.MemoryUsagePercent,
                PrimaryDiskUsagePercent = summary.PrimaryDiskUsagePercent,
                PrimaryTemperatureCelsius = summary.PrimaryTemperatureCelsius,
                LastUpdateUnix = summary.LastUpdateUnix,
                LastError = summary.LastError,
                AgentVersion = summary.AgentVersion,
                Host = system.Host,
                Kernel = system.Kernel,
                Architecture = system.Architecture,
                Uptime = system.Uptime,
                Cpu = new CpuInfo { Cores = status.Cpu.Cores, UsagePercent = status.Cpu.UsagePercent },
                Memory = new MemoryInfo { TotalBytes = status.Memory.TotalBytes, UsedBytes = status.Memory.UsedBytes, UsagePercent = status.Memory.UsagePercent },
                Disks = status.Disks.Select(d => new DiskInfo { MountPoint = d.MountPoint, Filesystem = d.Filesystem, TotalBytes = d.TotalBytes, UsedBytes = d.UsedBytes, UsagePercent = d.UsagePercent }).ToList(),
                NetworkInterfaces = status.Network.Select(n => new NetworkInterfaceInfo { Name = n.Name, RxBytes = n.RxBytes, TxBytes = n.TxBytes }).ToList(),
                Temperatures = status.Temperatures.Select(t => new TemperatureInfo { Sensor = t.Sensor, Celsius = t.Celsius }).ToList(),
                CollectorStatuses = status.Collectors.Select(c => new CollectorStatusInfo { Name = c.Name, Enabled = c.Enabled, HasError = c.HasError, LastError = c.LastError }).ToList()
            };

            var historyPoint = new HistoryDataPoint
            {
                TimestampUnix = now,
                CpuPercent = status.Cpu.UsagePercent,
                MemoryPercent = status.Memory.UsagePercent,
                DiskPercent = status.Disks.Count > 0 ? status.Disks[0].UsagePercent : 0,
                TemperatureCelsius = status.Temperatures.Count > 0 ? status.Temperatures[0].Celsius : 0,
                RxBytes = status.Network.Count > 0 ? status.Network[0].RxBytes : 0,
                TxBytes = status.Network.Count > 0 ? status.Network[0].TxBytes : 0
            };

            var existing = _nodeStates.GetOrAdd(node.Id, _ => new NodeState());

            var history = existing.History.ToList();
            history.Add(historyPoint);
            if (history.Count > MaxHistoryPoints)
                history = history.Skip(history.Count - MaxHistoryPoints).ToList();

            _nodeStates[node.Id] = new NodeState
            {
                Summary = summary,
                Details = details,
                History = history,
                LastPoll = DateTime.UtcNow,
                HasError = false,
                ErrorMessage = ""
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Polling node {NodeId} ({NodeName}) timed out", node.Id, node.Name);
            MarkNodeOffline(node.Id, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("HTTP error polling node {NodeId} ({NodeName}): {Message}", node.Id, node.Name, ex.Message);
            MarkNodeOffline(node.Id, $"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error polling node {NodeId} ({NodeName})", node.Id, node.Name);
            MarkNodeOffline(node.Id, ex.Message);
        }
    }

    private void MarkNodeOffline(string nodeId, string errorMessage)
    {
        if (_nodeStates.TryGetValue(nodeId, out var existing))
        {
            var summary = existing.Summary with
            {
                Online = false,
                Status = "offline",
                LastError = errorMessage
            };

            _nodeStates[nodeId] = existing with
            {
                Summary = summary,
                HasError = true,
                ErrorMessage = errorMessage,
                LastPoll = DateTime.UtcNow
            };
        }
        else
        {
            var nodes = _configService.GetNodes();
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            _nodeStates[nodeId] = new NodeState
            {
                Summary = new NodeSummary
                {
                    Id = nodeId,
                    Name = node?.Name ?? nodeId,
                    Type = node?.Type ?? "",
                    Online = false,
                    Status = "offline",
                    LastError = errorMessage
                },
                HasError = true,
                ErrorMessage = errorMessage,
                LastPoll = DateTime.UtcNow
            };
        }
    }

    private static string ComputeStatus(CrossMonitorStatus status)
    {
        if (status.Cpu.UsagePercent >= 90 || status.Memory.UsagePercent >= 90)
            return "critical";
        if (status.Cpu.UsagePercent >= 70 || status.Memory.UsagePercent >= 70
            || status.Disks.Any(d => d.UsagePercent >= 90)
            || status.Temperatures.Any(t => t.Celsius >= 80))
            return "warning";
        return "healthy";
    }

    private async Task<T?> FetchJsonAsync<T>(HttpClient client, string url, string nodeId, CancellationToken ct) where T : class
    {
        try
        {
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to fetch {Url} for node {NodeId}: {Message}", url, nodeId, ex.Message);
            return null;
        }
    }
}
