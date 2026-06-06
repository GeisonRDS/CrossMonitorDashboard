namespace CrossMonitorDashboard.Api.Models;

public record VisualConfig
{
    public string Theme { get; init; } = "glass-blue";
    public int RefreshSeconds { get; init; } = 5;
    public string CardSize { get; init; } = "normal";
    public AnimationConfig Animations { get; init; } = new();
    public BackgroundConfig Background { get; init; } = new();
}
