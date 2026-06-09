namespace CrossMonitorDashboard.Api.Models;

public record NetworkInterfaceInfo
{
    public string Name { get; init; } = "";
    public long RxBytes { get; init; }
    public long TxBytes { get; init; }
    public bool IsPrimary { get; init; }
}
