using System.Globalization;
using System.Text;
using CrossMonitorDashboard.Api.Models;

namespace CrossMonitorDashboard.Api.Services;

public static class NodeMapper
{
    public static NodeState BuildNodeState(
        NodeConfig node,
        CrossMonitorSystem system,
        CrossMonitorStatus? status,
        CrossMonitorVersion? version,
        List<HistoryDataPoint> existingHistory,
        int maxHistoryPoints,
        long nowUnix)
    {
        var disks = system.Disks ?? new List<CrossMonitorDiskData>();
        var network = system.Network ?? new List<CrossMonitorNetworkData>();
        var temperatures = system.Temperatures ?? new List<CrossMonitorTemperatureData>();
        var systemErrors = system.Errors ?? new List<CrossMonitorError>();
        var collectors = status?.Collectors ?? new List<CollectorStatus>();
        var timestampUnix = system.TimestampUnix > 0
            ? system.TimestampUnix
            : status?.LastSnapshotUnix > 0 ? status.LastSnapshotUnix : nowUnix;

        var primaryDisk = disks.FirstOrDefault();
        var primaryNetwork = SelectPrimaryNetworkInterface(network);
        var primaryTemperature = temperatures.FirstOrDefault();
        var lastError = BuildLastError(systemErrors, collectors);
        var severity = ComputeStatus(system, status);

        var summary = new NodeSummary
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.Type,
            Online = true,
            Status = severity,
            Os = system.Host.Os,
            Platform = string.IsNullOrWhiteSpace(system.Host.PlatformVersion)
                ? system.Host.Platform
                : $"{system.Host.Platform} {system.Host.PlatformVersion}".Trim(),
            CpuUsagePercent = system.Cpu.UsagePercent,
            MemoryUsagePercent = system.Memory.UsagePercent,
            PrimaryDiskUsagePercent = primaryDisk?.UsagePercent ?? 0,
            PrimaryTemperatureCelsius = primaryTemperature?.TemperatureCelsius,
            LastUpdateUnix = timestampUnix,
            LastError = lastError,
            AgentVersion = version?.Version ?? system.Agent.Version
        };

        var details = new NodeDetails
        {
            Id = summary.Id,
            Name = summary.Name,
            Type = summary.Type,
            Online = summary.Online,
            Status = summary.Status,
            Os = summary.Os,
            Platform = summary.Platform,
            CpuUsagePercent = summary.CpuUsagePercent,
            MemoryUsagePercent = summary.MemoryUsagePercent,
            PrimaryDiskUsagePercent = summary.PrimaryDiskUsagePercent,
            PrimaryTemperatureCelsius = summary.PrimaryTemperatureCelsius,
            LastUpdateUnix = summary.LastUpdateUnix,
            LastError = summary.LastError,
            AgentVersion = summary.AgentVersion,
            Host = system.Host.Name,
            Kernel = system.Host.KernelVersion,
            Architecture = system.Host.Architecture,
            Uptime = system.Host.UptimeSeconds.ToString(),
            Cpu = new CpuInfo
            {
                Cores = system.Cpu.CoresLogical,
                UsagePercent = system.Cpu.UsagePercent
            },
            Memory = new MemoryInfo
            {
                TotalBytes = system.Memory.TotalBytes,
                UsedBytes = system.Memory.UsedBytes,
                UsagePercent = system.Memory.UsagePercent
            },
            Disks = disks.Select(d => new DiskInfo
            {
                MountPoint = d.MountPoint,
                Filesystem = d.Filesystem,
                TotalBytes = d.TotalBytes,
                UsedBytes = d.UsedBytes,
                UsagePercent = d.UsagePercent
            }).ToList(),
            NetworkInterfaces = network.Select(n => new NetworkInterfaceInfo
            {
                Name = n.Name,
                RxBytes = n.RxBytes,
                TxBytes = n.TxBytes,
                IsPrimary = primaryNetwork is not null && string.Equals(n.Name, primaryNetwork.Name, StringComparison.OrdinalIgnoreCase)
            }).ToList(),
            Temperatures = temperatures.Select(t => new TemperatureInfo
            {
                Sensor = t.Name,
                Celsius = t.TemperatureCelsius
            }).ToList(),
            CollectorStatuses = collectors.Select(c => new CollectorStatusInfo
            {
                Name = c.Name,
                Enabled = c.Enabled,
                HasError = !string.IsNullOrWhiteSpace(c.LastError),
                LastError = c.LastError
            }).ToList()
        };

        var historyPoint = new HistoryDataPoint
        {
            TimestampUnix = timestampUnix,
            CpuPercent = system.Cpu.UsagePercent,
            MemoryPercent = system.Memory.UsagePercent,
            DiskPercent = primaryDisk?.UsagePercent ?? 0,
            TemperatureCelsius = primaryTemperature?.TemperatureCelsius ?? 0,
            RxBytes = primaryNetwork?.RxBytes ?? 0,
            TxBytes = primaryNetwork?.TxBytes ?? 0,
            NetworkInterfaceName = primaryNetwork?.Name ?? ""
        };

        var history = existingHistory.ToList();
        history.Add(historyPoint);
        if (history.Count > maxHistoryPoints)
            history = history.Skip(history.Count - maxHistoryPoints).ToList();

        return new NodeState
        {
            Summary = summary,
            Details = details,
            History = history,
            LastPoll = DateTime.UtcNow,
            HasError = false,
            ErrorMessage = ""
        };
    }

    public static string ComputeStatus(CrossMonitorSystem system, CrossMonitorStatus? status)
    {
        var disks = system.Disks ?? new List<CrossMonitorDiskData>();
        var temperatures = system.Temperatures ?? new List<CrossMonitorTemperatureData>();
        var errors = system.Errors ?? new List<CrossMonitorError>();
        var collectors = status?.Collectors ?? new List<CollectorStatus>();

        if (system.Cpu.UsagePercent >= 90
            || system.Memory.UsagePercent >= 90
            || disks.Any(d => d.UsagePercent >= 90)
            || temperatures.Any(t => t.TemperatureCelsius >= 85))
            return "critical";

        if (system.Cpu.UsagePercent >= 75
            || system.Memory.UsagePercent >= 75
            || disks.Any(d => d.UsagePercent >= 80)
            || temperatures.Any(t => t.TemperatureCelsius >= 70)
            || errors.Count > 0
            || collectors.Any(c => !string.IsNullOrWhiteSpace(c.LastError)))
            return "warning";

        return "healthy";
    }

    public static CrossMonitorNetworkData? SelectPrimaryNetworkInterface(IEnumerable<CrossMonitorNetworkData>? interfaces)
    {
        var items = (interfaces ?? Enumerable.Empty<CrossMonitorNetworkData>()).ToList();
        if (items.Count == 0) return null;

        var usefulWithTraffic = items
            .Where(n => HasTraffic(n) && IsUsefulNetworkInterface(n.Name))
            .OrderByDescending(TotalTraffic)
            .ThenByDescending(n => IsKnownPhysicalNetworkInterface(n.Name))
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (usefulWithTraffic is not null) return usefulWithTraffic;

        var anyWithTraffic = items
            .Where(HasTraffic)
            .OrderByDescending(TotalTraffic)
            .ThenByDescending(n => IsKnownPhysicalNetworkInterface(n.Name))
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (anyWithTraffic is not null) return anyWithTraffic;

        return items
            .OrderByDescending(n => IsUsefulNetworkInterface(n.Name))
            .ThenByDescending(n => IsKnownPhysicalNetworkInterface(n.Name))
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool HasTraffic(CrossMonitorNetworkData item) => TotalTraffic(item) > 0;

    private static long TotalTraffic(CrossMonitorNetworkData item)
        => Math.Max(0, item.RxBytes) + Math.Max(0, item.TxBytes);

    private static bool IsUsefulNetworkInterface(string name)
    {
        var normalized = NormalizeNetworkName(name);
        if (string.IsNullOrWhiteSpace(normalized)) return false;

        var ignoredExactNames = new[] { "lo" };
        if (ignoredExactNames.Any(exact => string.Equals(normalized, exact, StringComparison.OrdinalIgnoreCase)))
            return false;

        var ignoredPrefixes = new[]
        {
            "loopback",
            "loopback pseudo-interface",
            "bluetooth network connection",
            "conexao de rede bluetooth",
            "teredo",
            "isatap",
            "veth",
            "vethernet",
            "br-",
            "docker",
            "virbr"
        };

        return !ignoredPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsKnownPhysicalNetworkInterface(string name)
    {
        var normalized = NormalizeNetworkName(name);
        var physicalPrefixes = new[]
        {
            "ethernet",
            "wi-fi",
            "wifi",
            "rede ethernet",
            "conexao local",
            "local area connection",
            "eth",
            "enp",
            "eno",
            "ens",
            "wlan",
            "igb",
            "em",
            "re"
        };

        return physicalPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeNetworkName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static string BuildLastError(List<CrossMonitorError> systemErrors, List<CollectorStatus> collectors)
    {
        var errors = new List<string>();

        errors.AddRange(systemErrors
            .Where(e => !string.IsNullOrWhiteSpace(e.Message))
            .Select(e => string.IsNullOrWhiteSpace(e.Collector) ? e.Message : $"{e.Collector}: {e.Message}"));

        errors.AddRange(collectors
            .Where(c => !string.IsNullOrWhiteSpace(c.LastError))
            .Select(c => string.IsNullOrWhiteSpace(c.Name) ? c.LastError : $"{c.Name}: {c.LastError}"));

        return string.Join("; ", errors);
    }
}
