namespace CrossMonitorDashboard.Api.Models;

public record AnimationConfig
{
    public bool Enabled { get; init; } = true;
    public string Intensity { get; init; } = "normal";
}
