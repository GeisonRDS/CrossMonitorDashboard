namespace CrossMonitorDashboard.Api.Models;

public record CrossMonitorStatus
{
    public CpuData Cpu { get; init; } = new();
    public MemoryData Memory { get; init; } = new();
    public List<DiskData> Disks { get; init; } = new();
    public List<NetworkData> Network { get; init; } = new();
    public List<TemperatureData> Temperatures { get; init; } = new();
    public List<CollectorData> Collectors { get; init; } = new();
    public string LastError { get; init; } = "";
}

public record CpuData
{
    public int Cores { get; init; }
    public double UsagePercent { get; init; }
}

public record MemoryData
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsagePercent { get; init; }
}

public record DiskData
{
    public string MountPoint { get; init; } = "";
    public string Filesystem { get; init; } = "";
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsagePercent { get; init; }
}

public record NetworkData
{
    public string Name { get; init; } = "";
    public long RxBytes { get; init; }
    public long TxBytes { get; init; }
}

public record TemperatureData
{
    public string Sensor { get; init; } = "";
    public double Celsius { get; init; }
}

public record CollectorData
{
    public string Name { get; init; } = "";
    public bool Enabled { get; init; }
    public bool HasError { get; init; }
    public string LastError { get; init; } = "";
}
