using CrossMonitorDashboard.Api.Models;

namespace CrossMonitorDashboard.Api.Services;

public class DashboardService
{
    private readonly NodePollingService _pollingService;

    public DashboardService(NodePollingService pollingService)
    {
        _pollingService = pollingService;
    }

    public List<NodeSummary> GetNodeSummaries()
    {
        return _pollingService.GetAllNodeSummaries();
    }

    public NodeDetails? GetNodeDetails(string id)
    {
        return _pollingService.GetNodeDetails(id);
    }

    public List<HistoryDataPoint> GetNodeHistory(string id)
    {
        return _pollingService.GetNodeHistory(id);
    }

    public DashboardSummary ComputeSummary()
    {
        var nodes = GetNodeSummaries();

        if (nodes.Count == 0)
            return new DashboardSummary();

        var total = nodes.Count;
        var online = nodes.Count(n => n.Online);
        var offline = total - online;
        var warning = nodes.Count(n => n.Status == "warning");
        var critical = nodes.Count(n => n.Status == "critical");
        var avgCpu = nodes.Where(n => n.Online).Average(n => n.CpuUsagePercent);
        var avgMem = nodes.Where(n => n.Online).Average(n => n.MemoryUsagePercent);
        var highestDisk = nodes.Where(n => n.Online).Select(n => n.PrimaryDiskUsagePercent).DefaultIfEmpty(0).Max();
        var highestTemp = nodes.Where(n => n.Online).Select(n => n.PrimaryTemperatureCelsius).DefaultIfEmpty(0).Max();

        return new DashboardSummary
        {
            TotalNodes = total,
            OnlineNodes = online,
            OfflineNodes = offline,
            WarningNodes = warning,
            CriticalNodes = critical,
            AverageCpuPercent = Math.Round(avgCpu, 1),
            AverageMemoryPercent = Math.Round(avgMem, 1),
            HighestDiskUsagePercent = Math.Round(highestDisk, 1),
            HighestTemperatureCelsius = Math.Round(highestTemp, 1)
        };
    }
}
