namespace CrossMonitorDashboard.Api.Models;

public record TemperatureInfo
{
    public string Sensor { get; init; } = "";
    public double Celsius { get; init; }
}
