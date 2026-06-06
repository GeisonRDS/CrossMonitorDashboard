namespace CrossMonitorDashboard.Api.Models;

public record CollectorStatusInfo
{
    public string Name { get; init; } = "";
    public bool Enabled { get; init; }
    public bool HasError { get; init; }
    public string LastError { get; init; } = "";
}
