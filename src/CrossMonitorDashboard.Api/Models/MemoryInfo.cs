namespace CrossMonitorDashboard.Api.Models;

public record MemoryInfo
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsagePercent { get; init; }
}
