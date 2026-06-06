namespace CrossMonitorDashboard.Api.Models;

public record DiskInfo
{
    public string MountPoint { get; init; } = "";
    public string Filesystem { get; init; } = "";
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsagePercent { get; init; }
}
