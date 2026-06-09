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
    public void BuildNodeState_WindowsEthernetZeroWifiTrafficChoosesWifi()
    {
        var state = BuildStateWithNetwork(new()
        {
            Network("Ethernet", 0, 0),
            Network("Wi-Fi", 203488489, 30276788),
            Network("Loopback Pseudo-Interface 1", 0, 0)
        });

        Assert.Equal("Wi-Fi", state.History.Single().NetworkInterfaceName);
        Assert.Equal(203488489, state.History.Single().RxBytes);
        Assert.True(state.Details!.NetworkInterfaces.Single(n => n.Name == "Wi-Fi").IsPrimary);
    }

    [Fact]
    public void BuildNodeState_WindowsEthernetTrafficWifiZeroChoosesEthernet()
    {
        var state = BuildStateWithNetwork(new()
        {
            Network("Ethernet", 5000, 1000),
            Network("Wi-Fi", 0, 0)
        });

        Assert.Equal("Ethernet", state.History.Single().NetworkInterfaceName);
        Assert.Equal(5000, state.History.Single().RxBytes);
        Assert.True(state.Details!.NetworkInterfaces.Single(n => n.Name == "Ethernet").IsPrimary);
    }

    [Fact]
    public void BuildNodeState_LinuxMainInterfaceStillWorks()
    {
        var state = BuildStateWithNetwork(new()
        {
            Network("eno1", 100, 200)
        });

        Assert.Equal("eno1", state.History.Single().NetworkInterfaceName);
        Assert.Equal(100, state.History.Single().RxBytes);
        Assert.Equal(200, state.History.Single().TxBytes);
    }

    [Fact]
    public void BuildNodeState_FreeBsdActiveInterfaceStillWorks()
    {
        var state = BuildStateWithNetwork(new()
        {
            Network("igb0", 9000, 3000)
        });

        Assert.Equal("igb0", state.History.Single().NetworkInterfaceName);
    }

    [Fact]
    public void SelectPrimaryNetworkInterface_DoesNotChooseLoopbackWhenValidInterfaceExists()
    {
        var selected = NodeMapper.SelectPrimaryNetworkInterface(new List<CrossMonitorNetworkData>
        {
            Network("Loopback Pseudo-Interface 1", 999999, 999999),
            Network("Wi-Fi", 1000, 500)
        });

        Assert.NotNull(selected);
        Assert.Equal("Wi-Fi", selected!.Name);
    }

    [Fact]
    public void SelectPrimaryNetworkInterface_DoesNotChooseBluetoothWhenValidInterfaceExists()
    {
        var selected = NodeMapper.SelectPrimaryNetworkInterface(new List<CrossMonitorNetworkData>
        {
            Network("Conexão de Rede Bluetooth 2", 999999, 999999),
            Network("Ethernet", 1000, 500)
        });

        Assert.NotNull(selected);
        Assert.Equal("Ethernet", selected!.Name);
    }

    [Fact]
    public void SelectPrimaryNetworkInterface_EmptyListReturnsNull()
    {
        Assert.Null(NodeMapper.SelectPrimaryNetworkInterface(new List<CrossMonitorNetworkData>()));
    }

    [Fact]
    public void DeserializeSystem_NetworkMissingCountersDoesNotBreakMapping()
    {
        var system = JsonSerializer.Deserialize<CrossMonitorSystem>(SystemWithPartialNetworkJson, JsonOptions)!;

        var state = NodeMapper.BuildNodeState(Node, system, DeserializeStatus(), DeserializeVersion(), new(), 120, 1);

        Assert.Single(state.Details!.NetworkInterfaces);
        Assert.Equal("Wi-Fi", state.History.Single().NetworkInterfaceName);
        Assert.Equal(0, state.History.Single().RxBytes);
        Assert.Equal(0, state.History.Single().TxBytes);
        Assert.Equal(0, state.History.Single().DownloadMBps);
        Assert.Equal(0, state.History.Single().UploadMBps);
    }

    [Fact]
    public void BuildNodeState_FirstNetworkCollectionGeneratesZeroRates()
    {
        var state = BuildStateWithNetwork(new() { Network("Wi-Fi", 203488489, 30276788) }, new(), 1780549300);

        Assert.Equal(0, state.History.Single().DownloadMBps);
        Assert.Equal(0, state.History.Single().UploadMBps);
    }

    [Fact]
    public void BuildNodeState_SecondNetworkCollectionCalculatesMBpsRates()
    {
        var previous = new List<HistoryDataPoint>
        {
            HistoryNetwork("Wi-Fi", 203488489, 30276788, 1780549300)
        };

        var state = BuildStateWithNetwork(new()
        {
            Network("Wi-Fi", 204537065, 30381664)
        }, previous, 1780549301);

        var point = state.History.Last();
        Assert.Equal(1.0, point.DownloadMBps, 3);
        Assert.Equal(0.1, point.UploadMBps, 3);
        Assert.Equal(1.0, state.Details!.NetworkInterfaces.Single(n => n.Name == "Wi-Fi").DownloadMBps, 3);
    }

    [Fact]
    public void BuildNodeState_DoesNotUseAccumulatedBytesAsRate()
    {
        var state = BuildStateWithNetwork(new() { Network("Wi-Fi", 204537065, 30381664) }, new(), 1780549301);

        Assert.Equal(0, state.History.Single().DownloadMBps);
        Assert.Equal(204537065, state.History.Single().RxBytes);
    }

    [Fact]
    public void CalculateNetworkRate_NegativeDeltaReturnsZero()
    {
        var previous = HistoryNetwork("Wi-Fi", 2000, 2000, 10);
        var rate = NodeMapper.CalculateNetworkRate(Network("Wi-Fi", 1000, 1000), previous, 11);

        Assert.Equal(0, rate.DownloadMBps);
        Assert.Equal(0, rate.UploadMBps);
    }

    [Fact]
    public void CalculateNetworkRate_InvalidIntervalReturnsZero()
    {
        var previous = HistoryNetwork("Wi-Fi", 1000, 1000, 10);
        var rate = NodeMapper.CalculateNetworkRate(Network("Wi-Fi", 2000, 2000), previous, 10);

        Assert.Equal(0, rate.DownloadMBps);
        Assert.Equal(0, rate.UploadMBps);
    }

    [Fact]
    public void CalculateNetworkRate_InterfaceChangeReturnsZero()
    {
        var previous = HistoryNetwork("Ethernet", 1000, 1000, 10);
        var rate = NodeMapper.CalculateNetworkRate(Network("Wi-Fi", 2098152, 1053576), previous, 11);

        Assert.Equal(0, rate.DownloadMBps);
        Assert.Equal(0, rate.UploadMBps);
    }

    [Fact]
    public void BuildNodeState_HistoryKeepsMaxPointsAndAddsNewestOnRight()
    {
        var history = Enumerable.Range(1, 10)
            .Select(i => HistoryNetwork("Wi-Fi", i * 1000, i * 1000, i))
            .ToList();

        var state = BuildStateWithNetwork(new() { Network("Wi-Fi", 11000, 11000) }, history, 11, maxHistoryPoints: 10);

        Assert.Equal(10, state.History.Count);
        Assert.Equal(2, state.History.First().TimestampUnix);
        Assert.Equal(11, state.History.Last().TimestampUnix);
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

    private static NodeState BuildStateWithNetwork(List<CrossMonitorNetworkData> network)
        => BuildStateWithNetwork(network, new(), 1780549300);

    private static NodeState BuildStateWithNetwork(
        List<CrossMonitorNetworkData> network,
        List<HistoryDataPoint> existingHistory,
        long timestampUnix,
        int maxHistoryPoints = 120)
    {
        var system = JsonSerializer.Deserialize<CrossMonitorSystem>(SystemJson, JsonOptions)! with
        {
            Network = network,
            TimestampUnix = timestampUnix
        };

        return NodeMapper.BuildNodeState(Node, system, DeserializeStatus(), DeserializeVersion(), existingHistory, maxHistoryPoints, timestampUnix);
    }

    private static CrossMonitorNetworkData Network(string name, long rxBytes, long txBytes)
        => new() { Name = name, RxBytes = rxBytes, TxBytes = txBytes };

    private static HistoryDataPoint HistoryNetwork(string name, long rxBytes, long txBytes, long timestampUnix)
        => new() { NetworkInterfaceName = name, RxBytes = rxBytes, TxBytes = txBytes, TimestampUnix = timestampUnix };

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

    private const string SystemWithPartialNetworkJson = """
    {
      "agent": { "name": "CrossMonitor", "version": "0.1.0", "apiVersion": "v1" },
      "host": { "name": "pc", "os": "windows", "platform": "windows", "platformVersion": "11", "kernelVersion": "", "architecture": "amd64", "uptimeSeconds": 1, "bootTimeUnix": 1 },
      "cpu": { "model": "", "vendor": "", "coresPhysical": 1, "coresLogical": 1, "usagePercent": 1 },
      "memory": { "totalBytes": 1, "availableBytes": 1, "usedBytes": 0, "freeBytes": 1, "usagePercent": 1 },
      "disks": [],
      "network": [{ "name": "Wi-Fi" }],
      "temperatures": [],
      "errors": [],
      "timestampUnix": 1780549295
    }
    """;
}
