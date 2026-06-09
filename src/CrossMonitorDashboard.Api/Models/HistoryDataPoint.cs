namespace CrossMonitorDashboard.Api.Models;

public record HistoryDataPoint
{
    public long TimestampUnix { get; init; }
    public double CpuPercent { get; init; }
    public double MemoryPercent { get; init; }
    public double DiskPercent { get; init; }
    public double TemperatureCelsius { get; init; }
    public long RxBytes { get; init; }
    public long TxBytes { get; init; }
    public double DownloadMBps { get; init; }
    public double UploadMBps { get; init; }
    public string NetworkInterfaceName { get; init; } = "";
}
