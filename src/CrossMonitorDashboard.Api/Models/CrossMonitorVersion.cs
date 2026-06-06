namespace CrossMonitorDashboard.Api.Models;

public record CrossMonitorVersion
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string ApiVersion { get; init; } = "";
    public string BuildCommit { get; init; } = "";
    public string BuildDate { get; init; } = "";
    public string GoVersion { get; init; } = "";
}
