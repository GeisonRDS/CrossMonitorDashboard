export interface NodeSummary {
  id: string
  name: string
  type: string
  online: boolean
  status: string
  os: string
  platform: string
  cpuUsagePercent: number
  memoryUsagePercent: number
  primaryDiskUsagePercent: number
  primaryTemperatureCelsius: number | null
  lastUpdateUnix: number
  lastError: string
  agentVersion: string
}

export interface NodeDetails extends NodeSummary {
  host: string
  kernel: string
  architecture: string
  uptime: string
  cpu: CpuInfo
  memory: MemoryInfo
  disks: DiskInfo[]
  networkInterfaces: NetworkInterfaceInfo[]
  temperatures: TemperatureInfo[]
  collectorStatuses: CollectorStatusInfo[]
}

export interface CpuInfo {
  cores: number
  usagePercent: number
}

export interface MemoryInfo {
  totalBytes: number
  usedBytes: number
  usagePercent: number
}

export interface DiskInfo {
  mountPoint: string
  filesystem: string
  totalBytes: number
  usedBytes: number
  usagePercent: number
}

export interface NetworkInterfaceInfo {
  name: string
  rxBytes: number
  txBytes: number
}

export interface TemperatureInfo {
  sensor: string
  celsius: number
}

export interface CollectorStatusInfo {
  name: string
  enabled: boolean
  hasError: boolean
  lastError: string
}

export interface DashboardSummary {
  totalNodes: number
  onlineNodes: number
  offlineNodes: number
  warningNodes: number
  criticalNodes: number
  averageCpuPercent: number
  averageMemoryPercent: number
  highestDiskUsagePercent: number
  highestTemperatureCelsius: number
}

export interface HistoryDataPoint {
  timestampUnix: number
  cpuPercent: number
  memoryPercent: number
  diskPercent: number
  temperatureCelsius: number
  rxBytes: number
  txBytes: number
}

export interface VisualConfig {
  theme: string
  refreshSeconds: number
  cardSize: string
  animations: AnimationConfig
  background: BackgroundConfig
}

export interface AnimationConfig {
  enabled: boolean
  intensity: string
}

export interface BackgroundConfig {
  type: string
  imagePath: string
  opacity: number
  blur: number
  overlay: number
}
