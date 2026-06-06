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

            if (system == null)
            {
                MarkNodeOffline(node.Id, "Invalid response from node");
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var existing = _nodeStates.GetOrAdd(node.Id, _ => new NodeState());
            _nodeStates[node.Id] = NodeMapper.BuildNodeState(node, system, status, version, existing.History, MaxHistoryPoints, now);
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
