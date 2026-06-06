namespace CrossMonitorDashboard.Api.Models;

public record NodeState
{
    public NodeSummary Summary { get; init; } = new();
    public NodeDetails? Details { get; init; }
    public List<HistoryDataPoint> History { get; init; } = new();
    public DateTime LastPoll { get; init; }
    public bool HasError { get; init; }
    public string ErrorMessage { get; init; } = "";
}
