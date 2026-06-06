namespace CrossMonitorDashboard.Api.Models;

public record NodeSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public bool Online { get; init; }
    public string Status { get; init; } = "offline";
    public string Os { get; init; } = "";
    public string Platform { get; init; } = "";
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public double PrimaryDiskUsagePercent { get; init; }
    public double? PrimaryTemperatureCelsius { get; init; }
    public long LastUpdateUnix { get; init; }
    public string LastError { get; init; } = "";
    public string AgentVersion { get; init; } = "";
}
