using System.Text.Json;
using CrossMonitorDashboard.Api.Models;
using CrossMonitorDashboard.Api.Services;

namespace CrossMonitorDashboard.Tests;

public class CrossMonitorAgentContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void DeserializeSystem_WithHostObject_Succeeds()
    {
        var system = JsonSerializer.Deserialize<CrossMonitorSystem>(SystemJson, JsonOptions);

        Assert.NotNull(system);
        Assert.Equal("m92p1", system!.Host.Name);
        Assert.Equal("linux", system.Host.Os);
        Assert.Equal("ubuntu", system.Host.Platform);
        Assert.Equal("/", system.Disks[0].MountPoint);
        Assert.Equal(59, system.Temperatures[0].TemperatureCelsius);
    }

    [Fact]
    public void DeserializeStatus_WithoutMetrics_Succeeds()
    {
        var status = JsonSerializer.Deserialize<CrossMonitorStatus>(StatusJson, JsonOptions);

        Assert.NotNull(status);
        Assert.True(status!.Ready);
        Assert.Single(status.Collectors);
        Assert.Equal("system", status.Collectors[0].Name);
    }

    [Fact]
    public void DeserializeVersion_Succeeds()
    {
        var version = JsonSerializer.Deserialize<CrossMonitorVersion>(VersionJson, JsonOptions);

        Assert.NotNull(version);
        Assert.Equal("CrossMonitor", version!.Name);
        Assert.Equal("0.1.0", version.Version);
        Assert.Equal("v1", version.ApiVersion);
    }

    [Fact]
    public void BuildNodeState_MapsSummaryFromSystemStatusAndVersion()
    {
        var state = BuildState();

        Assert.Equal("m92p", state.Summary.Id);
        Assert.Equal("linux", state.Summary.Os);
        Assert.Equal("ubuntu 24.04", state.Summary.Platform);
        Assert.Equal(34.5, state.Summary.CpuUsagePercent);
        Assert.Equal(37.8, state.Summary.MemoryUsagePercent);
        Assert.Equal(98.7, state.Summary.PrimaryDiskUsagePercent);
        Assert.Equal(59, state.Summary.PrimaryTemperatureCelsius);
        Assert.Equal(1780549295, state.Summary.LastUpdateUnix);
        Assert.Equal("0.1.0", state.Summary.AgentVersion);
        Assert.Equal("m92p1", state.Details!.Host);
        Assert.Equal("6.8.0-117-generic", state.Details.Kernel);
        Assert.Equal("379717", state.Details.Uptime);
    }

    [Fact]
    public void BuildNodeState_DoesNotExposeTokenInSummaryOrDetails()
    {
        var state = BuildState();
        var summaryJson = JsonSerializer.Serialize(state.Summary);
        var detailsJson = JsonSerializer.Serialize(state.Details);

        Assert.DoesNotContain("SECRET_TOKEN", summaryJson);
        Assert.DoesNotContain("SECRET_TOKEN", detailsJson);
    }

    [Fact]
    public void BuildNodeState_NodeOfflineDoesNotBreakSummaryDefaults()
    {
        var state = new NodeState
        {
            Summary = new NodeSummary { Id = "offline", Online = false, Status = "offline" },
            Details = null,
            History = new List<HistoryDataPoint>()
        };

        Assert.False(state.Summary.Online);
        Assert.Equal("offline", state.Summary.Status);
        Assert.Empty(state.History);
    }

    [Fact]
    public void BuildNodeState_EmptyArraysDoNotBreakMapping()
    {
        var system = JsonSerializer.Deserialize<CrossMonitorSystem>(SystemJson, JsonOptions)! with
        {
            Disks = new List<CrossMonitorDiskData>(),
            Network = new List<CrossMonitorNetworkData>(),
            Temperatures = new List<CrossMonitorTemperatureData>(),
            Errors = new List<CrossMonitorError>()
        };

        var state = NodeMapper.BuildNodeState(Node, system, DeserializeStatus(), DeserializeVersion(), new(), 120, 1);

        Assert.Equal(0, state.Summary.PrimaryDiskUsagePercent);
        Assert.Null(state.Summary.PrimaryTemperatureCelsius);
        Assert.Empty(state.Details!.Disks);
        Assert.Empty(state.Details.NetworkInterfaces);
        Assert.Empty(state.Details.Temperatures);
        Assert.Single(state.History);
    }

    [Fact]
    public void ComputeStatus_CollectorLastErrorGeneratesWarning()
    {
        var status = DeserializeStatus() with
        {
            Collectors = new List<CollectorStatus>
            {
                new() { Name = "temperatures", Enabled = true, LastError = "sensor unavailable" }
            }
        };
        var system = HealthySystem();

        Assert.Equal("warning", NodeMapper.ComputeStatus(system, status));
    }

    [Fact]
    public void ComputeStatus_SystemErrorsGenerateWarning()
    {
        var system = HealthySystem() with
        {
            Errors = new List<CrossMonitorError> { new() { Collector = "network", Message = "partial collection failure" } }
        };

        Assert.Equal("warning", NodeMapper.ComputeStatus(system, DeserializeStatus()));
    }

    [Fact]
    public void ComputeStatus_DiskAbove90GeneratesCritical()
    {
        var system = HealthySystem() with
        {
            Disks = new List<CrossMonitorDiskData> { new() { MountPoint = "/", UsagePercent = 90 } }
        };

        Assert.Equal("critical", NodeMapper.ComputeStatus(system, DeserializeStatus()));
    }

    [Fact]
    public void ComputeStatus_TemperatureAbove70GeneratesWarning()
    {
        var system = HealthySystem() with
        {
            Temperatures = new List<CrossMonitorTemperatureData> { new() { Name = "cpu", TemperatureCelsius = 70 } }
        };

        Assert.Equal("warning", NodeMapper.ComputeStatus(system, DeserializeStatus()));
    }

    [Fact]
    public void ComputeStatus_TemperatureAbove85GeneratesCritical()
    {
        var system = HealthySystem() with
        {
            Temperatures = new List<CrossMonitorTemperatureData> { new() { Name = "cpu", TemperatureCelsius = 85 } }
        };

        Assert.Equal("critical", NodeMapper.ComputeStatus(system, DeserializeStatus()));
    }

    private static NodeState BuildState()
    {
        return NodeMapper.BuildNodeState(
            Node,
            JsonSerializer.Deserialize<CrossMonitorSystem>(SystemJson, JsonOptions)!,
            DeserializeStatus(),
            DeserializeVersion(),
            new List<HistoryDataPoint>(),
            120,
            1780549300);
    }

    private static CrossMonitorSystem HealthySystem()
    {
        return JsonSerializer.Deserialize<CrossMonitorSystem>(SystemJson, JsonOptions)! with
        {
            Cpu = new CrossMonitorCpuData { UsagePercent = 10, CoresLogical = 4 },
            Memory = new CrossMonitorMemoryData { UsagePercent = 10, TotalBytes = 100, UsedBytes = 10 },
            Disks = new List<CrossMonitorDiskData> { new() { MountPoint = "/", UsagePercent = 10 } },
            Temperatures = new List<CrossMonitorTemperatureData> { new() { Name = "cpu", TemperatureCelsius = 40 } },
            Errors = new List<CrossMonitorError>()
        };
    }

    private static CrossMonitorStatus DeserializeStatus()
        => JsonSerializer.Deserialize<CrossMonitorStatus>(StatusJson, JsonOptions)!;

    private static CrossMonitorVersion DeserializeVersion()
        => JsonSerializer.Deserialize<CrossMonitorVersion>(VersionJson, JsonOptions)!;

    private static readonly NodeConfig Node = new()
    {
        Id = "m92p",
        Name = "M92p - Docker",
        Type = "server",
        Url = "http://192.168.10.10:9580",
        Token = "SECRET_TOKEN",
        Enabled = true
    };

    private const string SystemJson = """
    {
      "agent": { "name": "CrossMonitor", "version": "0.1.0", "apiVersion": "v1" },
      "host": {
        "name": "m92p1",
        "os": "linux",
        "platform": "ubuntu",
        "platformVersion": "24.04",
        "kernelVersion": "6.8.0-117-generic",
        "architecture": "amd64",
        "uptimeSeconds": 379717,
        "bootTimeUnix": 1780169579
      },
      "cpu": {
        "model": "Intel(R) Core(TM) i5-3470T CPU @ 2.90GHz",
        "vendor": "GenuineIntel",
        "coresPhysical": 2,
        "coresLogical": 4,
        "usagePercent": 34.5
      },
      "memory": {
        "totalBytes": 16566292480,
        "availableBytes": 9454927872,
        "usedBytes": 6264340480,
        "freeBytes": 248815616,
        "usagePercent": 37.8
      },
      "disks": [
        {
          "device": "/dev/sda2",
          "mountpoint": "/",
          "filesystem": "ext4",
          "totalBytes": 124313583616,
          "usedBytes": 116443172864,
          "freeBytes": 1508335616,
          "usagePercent": 98.7
        }
      ],
      "network": [
        {
          "name": "eno1",
          "rxBytes": 228109103071,
          "txBytes": 36266406975,
          "rxPackets": 207645903,
          "txPackets": 160115100,
          "rxErrors": 0,
          "txErrors": 0
        }
      ],
      "temperatures": [
        {
          "name": "coretemp_package_id_0",
          "type": "cpu",
          "temperatureCelsius": 59,
          "highCelsius": 87,
          "criticalCelsius": 91
        }
      ],
      "errors": [],
      "timestampUnix": 1780549295
    }
    """;

    private const string StatusJson = """
    {
      "ready": true,
      "lastSnapshotUnix": 1780549295,
      "lastCollectionDurationMs": 49,
      "collectorIntervalSeconds": 5,
      "collectors": [
        {
          "name": "system",
          "enabled": true,
          "lastSuccessUnix": 1780549295,
          "lastError": ""
        }
      ]
    }
    """;

    private const string VersionJson = """
    {
      "name": "CrossMonitor",
      "version": "0.1.0",
      "apiVersion": "v1",
      "buildCommit": "dev",
      "buildDate": "2026-06-01",
      "goVersion": "go1.24"
    }
    """;
}
