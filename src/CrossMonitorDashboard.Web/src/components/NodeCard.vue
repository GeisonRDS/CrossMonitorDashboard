<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { NodeSummary, NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { useTheme, type MetricKey, type MetricChartType } from '../composables/useTheme'
import MiniChart from './MiniChart.vue'

const props = defineProps<{ node: NodeSummary; history?: HistoryDataPoint[]; details?: NodeDetails | null }>()
const router = useRouter()
const { currentTheme, visualSettings } = useTheme()

const fallbackHistories = {
  cpu: ref<number[]>([]),
  memory: ref<number[]>([]),
  disk: ref<number[]>([]),
  temperature: ref<number[]>([])
}

function clamp(value: number, max = 100) {
  return Math.max(0, Math.min(max, Number.isFinite(value) ? value : 0))
}

function pushPoint(target: typeof fallbackHistories.cpu, value: number, max = 100) {
  const next = [...target.value, Math.round(clamp(value, max) * 10) / 10]
  target.value = next.slice(-30)
}

watch(
  () => [props.node.cpuUsagePercent, props.node.memoryUsagePercent, props.node.primaryDiskUsagePercent, props.node.primaryTemperatureCelsius, props.node.lastUpdateUnix] as const,
  ([cpu, memory, disk, temperature]) => {
    pushPoint(fallbackHistories.cpu, cpu)
    pushPoint(fallbackHistories.memory, memory)
    pushPoint(fallbackHistories.disk, disk)
    pushPoint(fallbackHistories.temperature, temperature ?? 0, 120)
  },
  { immediate: true }
)

const history = computed(() => props.history ?? [])

function historySeries(field: keyof Pick<HistoryDataPoint, 'cpuPercent' | 'memoryPercent' | 'diskPercent' | 'temperatureCelsius'>, fallback: number[]) {
  const values = history.value.map(point => point[field])
  return values.length > 0 ? values : fallback
}

function rates(field: 'rxBytes' | 'txBytes') {
  const points = history.value
  if (points.length < 2) return []
  const result: number[] = []
  for (let index = 1; index < points.length; index++) {
    const previous = points[index - 1]
    const current = points[index]
    const seconds = Math.max(1, current.timestampUnix - previous.timestampUnix)
    result.push(Math.max(0, (current[field] - previous[field]) / seconds))
  }
  return result
}

function formatRate(bytesPerSecond: number | null) {
  if (bytesPerSecond == null || !Number.isFinite(bytesPerSecond)) return '--'
  const units = ['B/s', 'KB/s', 'MB/s', 'GB/s']
  let value = bytesPerSecond
  let unit = 0
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024
    unit++
  }
  return `${value >= 10 ? value.toFixed(0) : value.toFixed(1)} ${units[unit]}`
}

const downloadRates = computed(() => rates('rxBytes'))
const uploadRates = computed(() => rates('txBytes'))
const downloadRate = computed(() => downloadRates.value.at(-1) ?? null)
const uploadRate = computed(() => uploadRates.value.at(-1) ?? null)

const statusClass = computed(() => {
  if (!props.node.online) return 'status-offline'
  switch (props.node.status?.toLowerCase()) {
    case 'critical': return 'status-critical'
    case 'warning': return 'status-warning'
    default: return 'status-ok'
  }
})

const statusLabel = computed(() => {
  if (!props.node.online) return currentTheme.value === 'pixel-platformer' ? 'GAME OVER' : 'OFFLINE'
  switch (props.node.status?.toLowerCase()) {
    case 'critical': return 'CRITICAL'
    case 'warning': return 'WARNING'
    default: return 'ONLINE'
  }
})

function isCpuTemperatureSensor(name: string) {
  const lower = name.toLowerCase()
  return lower.includes('cpu') || lower.includes('core') || lower.includes('tctl') || lower.includes('tdie') || lower.includes('package') || lower.includes('tccd')
}

const metricLabels = computed(() => currentTheme.value === 'pixel-platformer'
  ? { cpu: 'HP', temperature: 'HEAT', memory: 'MP', disk: 'BAG', download: 'DOWN', upload: 'UP' }
  : { cpu: 'CPU', temperature: 'TEMP', memory: 'RAM', disk: 'DISCO', download: 'DOWN', upload: 'UP' })

function visibleSeverity(metric: 'cpu' | 'memory' | 'disk' | 'temperature') {
  const values = {
    cpu: props.node.cpuUsagePercent,
    memory: props.node.memoryUsagePercent,
    disk: Math.max(props.node.primaryDiskUsagePercent, ...(props.details?.disks.map(disk => disk.usagePercent) ?? [])),
    temperature: Math.max(props.node.primaryTemperatureCelsius ?? 0, ...(props.details?.temperatures.map(temp => temp.celsius) ?? []))
  }
  const value = values[metric]
  if (metric === 'disk') return value >= 90 ? 'critical' : value >= 80 ? 'warning' : 'normal'
  if (metric === 'temperature') return value >= 85 ? 'critical' : value >= 70 ? 'warning' : 'normal'
  return value >= 90 ? 'critical' : value >= 75 ? 'warning' : 'normal'
}

function findIssues() {
  const cpu = props.node.cpuUsagePercent
  const memory = props.node.memoryUsagePercent
  const diskCritical = props.details?.disks.find(item => item.usagePercent >= 90) ?? (props.node.primaryDiskUsagePercent >= 90 ? { mountPoint: 'principal', usagePercent: props.node.primaryDiskUsagePercent } : null)
  const warningDisk = props.details?.disks.find(item => item.usagePercent >= 80) ?? (props.node.primaryDiskUsagePercent >= 80 ? { mountPoint: 'principal', usagePercent: props.node.primaryDiskUsagePercent } : null)
  const tempCritical = props.details?.temperatures.find(item => item.celsius >= 85) ?? ((props.node.primaryTemperatureCelsius ?? 0) >= 85 ? { sensor: 'principal', celsius: props.node.primaryTemperatureCelsius ?? 0 } : null)
  const warningTemp = props.details?.temperatures.find(item => item.celsius >= 70) ?? ((props.node.primaryTemperatureCelsius ?? 0) >= 70 ? { sensor: 'principal', celsius: props.node.primaryTemperatureCelsius ?? 0 } : null)
  const collector = props.details?.collectorStatuses.find(item => item.hasError)

  if (cpu >= 90) return { metric: 'cpu', text: 'CPU acima de 90%', level: 'critical' as const }
  if (memory >= 90) return { metric: 'memory', text: 'RAM acima de 90%', level: 'critical' as const }
  if (diskCritical) return { metric: 'disk', text: `Disco ${diskCritical.mountPoint} acima de 90%`, level: 'critical' as const }
  if (tempCritical) return { metric: 'temperature', text: `Temperatura ${tempCritical.sensor} acima de 85°C`, level: 'critical' as const }
  if (cpu >= 75) return { metric: 'cpu', text: 'CPU acima de 75%', level: 'warning' as const }
  if (memory >= 75) return { metric: 'memory', text: 'RAM acima de 75%', level: 'warning' as const }
  if (warningDisk) return { metric: 'disk', text: `Disco ${warningDisk.mountPoint} acima de 80%`, level: 'warning' as const }
  if (warningTemp) return { metric: 'temperature', text: `Temperatura ${warningTemp.sensor} acima de 70°C`, level: 'warning' as const }
  if (collector) return { metric: null, text: `Erro no coletor ${collector.name}`, level: 'warning' as const }
  if (props.node.lastError) return { metric: null, text: props.node.lastError, level: 'warning' as const }
  return { metric: null, text: 'Sem alertas ativos', level: 'ok' as const }
}

const causerMetricId = computed(() => findIssues().metric)

function isCriticalDueToCpuTemp() {
  if (props.node.status?.toLowerCase() !== 'critical') return false
  const cpuTempCritical = (props.details?.temperatures ?? []).some(t => isCpuTemperatureSensor(t.sensor) && t.celsius >= 85)
  return cpuTempCritical
}

const cardStyle = computed(() => {
  if (!props.node.online) return { '--node-tone': 'var(--offline)', '--node-glow': 'var(--glow-accent)' }
  if (isCriticalDueToCpuTemp()) return { '--node-tone': 'var(--critical)', '--node-glow': 'var(--glow-critical)' }
  if (props.node.status?.toLowerCase() === 'critical') return { '--node-tone': 'var(--warning)', '--node-glow': 'var(--glow-warning)' }
  if (props.node.status?.toLowerCase() === 'warning') return { '--node-tone': 'var(--warning)', '--node-glow': 'var(--glow-warning)' }
  return { '--node-tone': 'var(--success)', '--node-glow': 'var(--glow-success)' }
})

const metrics = computed(() => {
  const networkType = visualSettings.value.metricCharts.network as MetricChartType
  const downloadMax = Math.max(1, ...downloadRates.value)
  const uploadMax = Math.max(1, ...uploadRates.value)
  return [
    { key: 'cpu' as MetricKey, id: 'cpu', label: metricLabels.value.cpu, value: `${Math.round(props.node.cpuUsagePercent)}%`, data: historySeries('cpuPercent', fallbackHistories.cpu.value), color: 'var(--accent)', unit: '%', max: 100, severity: visibleSeverity('cpu'), chartType: visualSettings.value.metricCharts.cpu },
    { key: 'temperature' as MetricKey, id: 'temperature', label: metricLabels.value.temperature, value: props.node.primaryTemperatureCelsius == null ? '--' : `${Math.round(props.node.primaryTemperatureCelsius)}°C`, data: historySeries('temperatureCelsius', fallbackHistories.temperature.value), color: 'var(--critical)', unit: '°', max: 120, severity: visibleSeverity('temperature'), chartType: visualSettings.value.metricCharts.temperature },
    { key: 'memory' as MetricKey, id: 'memory', label: metricLabels.value.memory, value: `${Math.round(props.node.memoryUsagePercent)}%`, data: historySeries('memoryPercent', fallbackHistories.memory.value), color: 'var(--warning)', unit: '%', max: 100, severity: visibleSeverity('memory'), chartType: visualSettings.value.metricCharts.memory },
    { key: 'disk' as MetricKey, id: 'disk', label: metricLabels.value.disk, value: `${Math.round(props.node.primaryDiskUsagePercent)}%`, data: historySeries('diskPercent', fallbackHistories.disk.value), color: 'var(--success)', unit: '%', max: 100, severity: visibleSeverity('disk'), chartType: visualSettings.value.metricCharts.disk },
    { key: 'network' as MetricKey, id: 'download', label: metricLabels.value.download, value: formatRate(downloadRate.value), data: downloadRates.value, color: 'var(--accent-light)', unit: '', max: downloadMax, severity: 'normal', chartType: networkType },
    { key: 'network' as MetricKey, id: 'upload', label: metricLabels.value.upload, value: formatRate(uploadRate.value), data: uploadRates.value, color: 'var(--text-secondary)', unit: '', max: uploadMax, severity: 'normal', chartType: networkType }
  ]
})

const lastUpdate = computed(() => {
  if (!props.node.lastUpdateUnix) return '--'
  const diff = Math.max(0, Date.now() / 1000 - props.node.lastUpdateUnix)
  if (diff < 60) return `${Math.round(diff)}s`
  if (diff < 3600) return `${Math.round(diff / 60)}m`
  if (diff < 86400) return `${Math.round(diff / 3600)}h`
  return `${Math.round(diff / 86400)}d`
})

function goToDetails() {
  router.push(`/nodes/${props.node.id}`)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault()
    goToDetails()
  }
}
</script>

<template>
  <article
    class="node-card glass-card fade-in"
    :class="[statusClass, { offline: !node.online }]"
    :style="cardStyle"
    role="button"
    tabindex="0"
    :aria-label="`Open details for ${node.name}`"
    @click="goToDetails"
    @keydown="handleKeydown"
  >
    <div class="card-topline"></div>
    <div class="card-header">
      <div class="node-info">
        <span class="node-type text-mono">{{ node.type }}</span>
        <h2 class="node-name">{{ node.name }}</h2>
      </div>
      <div class="status-stack">
        <span class="status-badge" :class="statusClass">
          <span class="status-light"></span>
          {{ statusLabel }}
        </span>
      </div>
    </div>

    <div class="metrics-grid">
      <section v-for="metric in metrics" :key="metric.id" class="metric-tile" :class="[`severity-${metric.severity}`, { 'is-causer': metric.id === causerMetricId }]">
        <div class="metric-copy">
          <span class="metric-label text-mono">{{ metric.label }}</span>
          <span class="metric-value-wrap">
            <strong>{{ metric.value }}</strong>
            <span v-if="metric.id === causerMetricId" class="metric-alert-chip" :class="statusClass">{{ statusLabel }}</span>
          </span>
        </div>
        <MiniChart
          :data="metric.data"
          :color="metric.color"
          :height="metric.chartType === 'radial-gauge' ? 54 : 42"
          :chart-type="metric.chartType"
          :unit="metric.unit"
          :max="metric.max"
        />
      </section>
    </div>

    <div v-if="node.lastError" class="node-error text-mono">{{ node.lastError }}</div>

    <div class="card-footer text-mono">
      <span>v{{ node.agentVersion || '--' }}</span>
      <span>updated {{ lastUpdate }}</span>
    </div>
  </article>
</template>

<style scoped>
.node-card {
  --node-tone: var(--success);
  --node-glow: var(--glow-success);
  position: relative;
  min-height: 380px;
  padding: 0.75rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  overflow: hidden;
  border-color: color-mix(in srgb, var(--node-tone) 32%, var(--border-color));
  box-shadow: var(--card-shadow), 0 0 18px color-mix(in srgb, var(--node-glow) 55%, transparent);
}

.node-card::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(circle at 92% 8%, color-mix(in srgb, var(--node-tone) 20%, transparent), transparent 28%),
    linear-gradient(180deg, rgba(255,255,255,0.045), transparent 44%);
}

.card-topline {
  position: absolute;
  left: 0;
  right: 0;
  top: 0;
  height: 3px;
  background: linear-gradient(90deg, transparent, var(--node-tone), transparent);
  opacity: 0.9;
}

.node-card:hover {
  transform: translateY(-3px);
  border-color: color-mix(in srgb, var(--node-tone) 58%, var(--border-color));
  box-shadow: var(--card-shadow-strong), 0 0 26px var(--node-glow);
}

.node-card:active {
  transform: translateY(-1px) scale(0.997);
}

.node-card:focus-visible {
  outline: 2px solid var(--accent-light);
  outline-offset: 3px;
}

.node-card.offline {
  filter: saturate(0.5);
}

.card-header,
.metrics-grid,
.node-error,
.card-footer {
  position: relative;
  z-index: 1;
}

.card-header {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(112px, auto);
  gap: 0.8rem;
  align-items: start;
}

.node-info {
  min-width: 0;
}

.node-type {
  display: block;
  margin-bottom: 0.24rem;
  color: var(--text-muted);
  font-size: 0.64rem;
  text-transform: uppercase;
  letter-spacing: 0.12em;
}

.node-name {
  font-size: 0.95rem;
  line-height: 1.15;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-stack {
  display: grid;
  justify-items: end;
  gap: 0.35rem;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 5px 8px;
  border-radius: 999px;
  font-size: 0.62rem;
  font-family: var(--font-mono);
  font-weight: 800;
  white-space: nowrap;
  color: var(--node-tone);
  background: color-mix(in srgb, var(--node-tone) 13%, rgba(0,0,0,0.34));
  border: 1px solid color-mix(in srgb, var(--node-tone) 44%, transparent);
}

.status-light {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: currentColor;
  box-shadow: 0 0 12px currentColor;
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem;
}

.metric-tile {
  min-width: 0;
  min-height: 92px;
  padding: 0.5rem;
  border-radius: 12px;
  background: var(--metric-tile-bg);
  border: 1px solid rgba(255,255,255,0.055);
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.05);
  transition: border-color var(--transition-speed), box-shadow var(--transition-speed), background var(--transition-speed);
}

.metric-tile.severity-warning {
  border-color: color-mix(in srgb, var(--warning) 62%, transparent);
  background: color-mix(in srgb, var(--warning) 10%, var(--metric-tile-bg));
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.05), 0 0 16px var(--glow-warning);
}

.metric-tile.severity-critical {
  border-color: color-mix(in srgb, var(--critical) 68%, transparent);
  background: color-mix(in srgb, var(--critical) 12%, var(--metric-tile-bg));
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.05), 0 0 18px var(--glow-critical);
}

.metric-copy {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.35rem;
  margin-bottom: 0.2rem;
}

.metric-label {
  color: var(--text-muted);
  font-size: 0.6rem;
  letter-spacing: 0.1em;
  flex-shrink: 0;
}

.metric-value-wrap {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  min-width: 0;
}

.metric-copy strong {
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.82rem;
}

.metric-alert-chip {
  font-family: var(--font-mono);
  font-size: 0.5rem;
  font-weight: 800;
  padding: 2px 5px;
  border-radius: 4px;
  white-space: nowrap;
  color: var(--node-tone);
  background: color-mix(in srgb, var(--node-tone) 18%, transparent);
  border: 1px solid color-mix(in srgb, var(--node-tone) 40%, transparent);
  animation: chip-pulse 2s ease-in-out infinite;
}

@keyframes chip-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

.metric-tile.is-causer {
  border-color: color-mix(in srgb, var(--node-tone) 60%, transparent);
  background: color-mix(in srgb, var(--node-tone) 14%, var(--metric-tile-bg));
}

.node-error {
  max-height: 3.2em;
  overflow: hidden;
  color: var(--warning);
  font-size: 0.68rem;
  line-height: 1.5;
  padding: 0.5rem 0.6rem;
  border: 1px solid color-mix(in srgb, var(--warning) 32%, transparent);
  background: color-mix(in srgb, var(--warning) 8%, transparent);
  border-radius: 10px;
}

.card-footer {
  margin-top: auto;
  display: flex;
  justify-content: space-between;
  gap: 0.6rem;
  padding-top: 0.2rem;
  color: var(--text-muted);
  font-size: 0.66rem;
  border-top: 1px solid var(--border-color);
}

[data-theme="pixel-platformer"] .node-card {
  border-width: 3px;
  box-shadow: 6px 6px 0 rgba(0,0,0,0.5);
}

[data-theme="pixel-platformer"] .metric-tile {
  border-radius: 2px;
  border-width: 2px;
}

@media (max-width: 390px) {
  .metrics-grid,
  .card-header {
    grid-template-columns: 1fr;
  }

  .status-stack {
    justify-items: start;
  }
}
</style>
