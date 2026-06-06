namespace CrossMonitorDashboard.Api.Models;

public record NodeConfig
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Url { get; init; } = "";
    public string Token { get; init; } = "";
    public bool Enabled { get; init; } = true;
}
