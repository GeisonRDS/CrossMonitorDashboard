namespace CrossMonitorDashboard.Api.Models;

public record BackgroundConfig
{
    public string Type { get; init; } = "gradient";
    public string ImagePath { get; init; } = "";
    public double Opacity { get; init; } = 0.35;
    public int Blur { get; init; } = 2;
    public double Overlay { get; init; } = 0.45;
}
