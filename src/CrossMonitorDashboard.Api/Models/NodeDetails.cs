namespace CrossMonitorDashboard.Api.Models;

public record NodeDetails : NodeSummary
{
    public string Host { get; init; } = "";
    public string Kernel { get; init; } = "";
    public string Architecture { get; init; } = "";
    public string Uptime { get; init; } = "";
    public CpuInfo Cpu { get; init; } = new();
    public MemoryInfo Memory { get; init; } = new();
    public List<DiskInfo> Disks { get; init; } = new();
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; init; } = new();
    public List<TemperatureInfo> Temperatures { get; init; } = new();
    public List<CollectorStatusInfo> CollectorStatuses { get; init; } = new();
}
