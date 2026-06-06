namespace CrossMonitorDashboard.Api.Models;

public record CrossMonitorSystem
{
    public string Host { get; init; } = "";
    public string Os { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Kernel { get; init; } = "";
    public string Architecture { get; init; } = "";
    public string Uptime { get; init; } = "";
}
