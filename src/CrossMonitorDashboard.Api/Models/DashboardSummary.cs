namespace CrossMonitorDashboard.Api.Models;

public record DashboardSummary
{
    public int TotalNodes { get; init; }
    public int OnlineNodes { get; init; }
    public int OfflineNodes { get; init; }
    public int WarningNodes { get; init; }
    public int CriticalNodes { get; init; }
    public double AverageCpuPercent { get; init; }
    public double AverageMemoryPercent { get; init; }
    public double HighestDiskUsagePercent { get; init; }
    public double HighestTemperatureCelsius { get; init; }
}
