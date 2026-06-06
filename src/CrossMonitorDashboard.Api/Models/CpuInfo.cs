namespace CrossMonitorDashboard.Api.Models;

public record CpuInfo
{
    public int Cores { get; init; }
    public double UsagePercent { get; init; }
}
