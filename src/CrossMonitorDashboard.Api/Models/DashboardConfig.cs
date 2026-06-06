namespace CrossMonitorDashboard.Api.Models;

public record DashboardConfig
{
    public VisualConfig Visual { get; init; } = new();
    public List<NodeConfig> Nodes { get; init; } = new();
}
