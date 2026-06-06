namespace CrossMonitorDashboard.Api.Models;

public record CrossMonitorStatus
{
    public bool Ready { get; init; }
    public long LastSnapshotUnix { get; init; }
    public long LastCollectionDurationMs { get; init; }
    public int CollectorIntervalSeconds { get; init; }
    public List<CollectorStatus> Collectors { get; init; } = new();
}

public record CollectorStatus
{
    public string Name { get; init; } = "";
    public bool Enabled { get; init; }
    public long LastSuccessUnix { get; init; }
    public string LastError { get; init; } = "";
}
