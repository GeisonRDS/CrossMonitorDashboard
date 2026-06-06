using System.Text.Json.Serialization;

namespace CrossMonitorDashboard.Api.Models;

public record CrossMonitorSystem
{
    public AgentData Agent { get; init; } = new();
    public HostData Host { get; init; } = new();
    public CrossMonitorCpuData Cpu { get; init; } = new();
    public CrossMonitorMemoryData Memory { get; init; } = new();
    public List<CrossMonitorDiskData> Disks { get; init; } = new();
    public List<CrossMonitorNetworkData> Network { get; init; } = new();
    public List<CrossMonitorTemperatureData> Temperatures { get; init; } = new();
    public List<CrossMonitorError> Errors { get; init; } = new();
    public long TimestampUnix { get; init; }
}

public record AgentData
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string ApiVersion { get; init; } = "";
}

public record HostData
{
    public string Name { get; init; } = "";
    public string Os { get; init; } = "";
    public string Platform { get; init; } = "";
    public string PlatformVersion { get; init; } = "";
    public string KernelVersion { get; init; } = "";
    public string Architecture { get; init; } = "";
    public long UptimeSeconds { get; init; }
    public long BootTimeUnix { get; init; }
}

public record CrossMonitorCpuData
{
    public string Model { get; init; } = "";
    public string Vendor { get; init; } = "";
    public int CoresPhysical { get; init; }
    public int CoresLogical { get; init; }
    public double UsagePercent { get; init; }
}

public record CrossMonitorMemoryData
{
    public long TotalBytes { get; init; }
    public long AvailableBytes { get; init; }
    public long UsedBytes { get; init; }
    public long FreeBytes { get; init; }
    public double UsagePercent { get; init; }
}

public record CrossMonitorDiskData
{
    public string Device { get; init; } = "";
    [JsonPropertyName("mountpoint")]
    public string MountPoint { get; init; } = "";
    public string Filesystem { get; init; } = "";
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long FreeBytes { get; init; }
    public double UsagePercent { get; init; }
}

public record CrossMonitorNetworkData
{
    public string Name { get; init; } = "";
    public long RxBytes { get; init; }
    public long TxBytes { get; init; }
    public long RxPackets { get; init; }
    public long TxPackets { get; init; }
    public long RxErrors { get; init; }
    public long TxErrors { get; init; }
}

public record CrossMonitorTemperatureData
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public double TemperatureCelsius { get; init; }
    public double HighCelsius { get; init; }
    public double CriticalCelsius { get; init; }
}

public record CrossMonitorError
{
    public string Collector { get; init; } = "";
    public string Message { get; init; } = "";
}
