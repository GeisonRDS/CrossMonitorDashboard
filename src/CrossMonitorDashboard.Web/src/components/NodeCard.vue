<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { NodeSummary, NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { useTheme, type MetricKey, type MetricChartType } from '../composables/useTheme'
import { useI18n } from '../composables/useI18n'
import MiniChart from './MiniChart.vue'
import { formatNetworkRateMBps, networkChartSeries, sanitizeNetworkRateMBps } from '../utils/networkChart'

const props = defineProps<{ node: NodeSummary; history?: HistoryDataPoint[]; details?: NodeDetails | null; editing?: boolean }>()
const router = useRouter()
const { currentTheme, visualSettings } = useTheme()
const { translate } = useI18n()

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

function networkSeries(field: 'downloadMBps' | 'uploadMBps') {
  return history.value.map(point => sanitizeNetworkRateMBps(point[field]))
}

const downloadRates = computed(() => networkSeries('downloadMBps'))
const uploadRates = computed(() => networkSeries('uploadMBps'))
const downloadVisualRates = computed(() => networkChartSeries(downloadRates.value))
const uploadVisualRates = computed(() => networkChartSeries(uploadRates.value))
const downloadRate = computed(() => downloadRates.value.at(-1) ?? 0)
const uploadRate = computed(() => uploadRates.value.at(-1) ?? 0)

const statusClass = computed(() => {
  if (!props.node.online) return 'status-offline'
  switch (props.node.status?.toLowerCase()) {
    case 'critical': return 'status-critical'
    case 'warning': return 'status-warning'
    default: return 'status-ok'
  }
})

const statusLabel = computed(() => {
  if (!props.node.online) return currentTheme.value === 'pixel-platformer' ? translate('card.gameOver') : translate('card.offline')
  return translate('card.online')
})

const hasDetailsAlert = computed(() => {
  const d = props.details
  const cpu = props.node.cpuUsagePercent
  const memory = props.node.memoryUsagePercent
  if (cpu >= 75 || memory >= 75) return true
  if (props.node.primaryDiskUsagePercent >= 80) return true
  if ((props.node.primaryTemperatureCelsius ?? 0) >= 70) return true
  if (d?.disks.some(item => item.usagePercent >= 80)) return true
  if (d?.temperatures.some(item => item.celsius >= 70)) return true
  if (d?.collectorStatuses.some(item => item.hasError)) return true
  if (props.node.lastError) return true
  return false
})

const metricLabels = computed(() => currentTheme.value === 'pixel-platformer'
  ? { cpu: translate('pixel.cpu'), temperature: translate('pixel.temperature'), memory: translate('pixel.memory'), disk: translate('pixel.disk'), download: translate('pixel.download'), upload: translate('pixel.upload') }
  : { cpu: translate('metrics.cpu'), temperature: translate('metrics.temperature'), memory: translate('metrics.memory'), disk: translate('metrics.disk'), download: translate('metrics.download'), upload: translate('metrics.upload') })

function visibleSeverity(metric: 'cpu' | 'memory' | 'disk' | 'temperature') {
  const value = ({
    cpu: props.node.cpuUsagePercent,
    memory: props.node.memoryUsagePercent,
    disk: props.node.primaryDiskUsagePercent,
    temperature: props.node.primaryTemperatureCelsius ?? 0
  })[metric]
  if (metric === 'disk') return value >= 90 ? 'critical' : value >= 80 ? 'warning' : 'normal'
  if (metric === 'temperature') return value >= 85 ? 'critical' : value >= 70 ? 'warning' : 'normal'
  return value >= 90 ? 'critical' : value >= 75 ? 'warning' : 'normal'
}

function findIssues() {
  const cpu = props.node.cpuUsagePercent
  const memory = props.node.memoryUsagePercent
  const disk = props.node.primaryDiskUsagePercent
  const temp = props.node.primaryTemperatureCelsius ?? 0
  const collector = props.details?.collectorStatuses.find(item => item.hasError)

  if (cpu >= 90) return { metric: 'cpu', textKey: 'details.cause.cpuCritical', textParams: { value: Math.round(cpu) }, level: 'critical' as const }
  if (memory >= 90) return { metric: 'memory', textKey: 'details.cause.memoryCritical', textParams: { value: Math.round(memory) }, level: 'critical' as const }
  if (disk >= 90) return { metric: 'disk', textKey: 'details.cause.diskCritical', textParams: { mount: 'primary', value: Math.round(disk) }, level: 'critical' as const }
  if (temp >= 85) return { metric: 'temperature', textKey: 'details.cause.tempCritical', textParams: { sensor: 'CPU', value: Math.round(temp) }, level: 'critical' as const }
  if (cpu >= 75) return { metric: 'cpu', textKey: 'details.cause.cpuWarning', textParams: { value: Math.round(cpu) }, level: 'warning' as const }
  if (memory >= 75) return { metric: 'memory', textKey: 'details.cause.memoryWarning', textParams: { value: Math.round(memory) }, level: 'warning' as const }
  if (disk >= 80) return { metric: 'disk', textKey: 'details.cause.diskWarning', textParams: {}, level: 'warning' as const }
  if (temp >= 70) return { metric: 'temperature', textKey: 'details.cause.tempWarning', textParams: { sensor: 'CPU', value: Math.round(temp) }, level: 'warning' as const }
  if (collector) return { metric: null, textKey: 'card.collectorError', textParams: {}, level: 'warning' as const }
  if (props.node.lastError) return { metric: null, textKey: null, textParams: {}, customText: props.node.lastError, level: 'warning' as const }
  return { metric: null, textKey: 'card.noAlerts', textParams: {}, level: 'ok' as const }
}

const causerMetricId = computed(() => findIssues().metric)
const findIssuesResult = computed(() => findIssues())

const cardSeverity = computed(() => {
  const severities = metrics.value.map(m => m.severity)
  if (severities.includes('critical')) return 'critical'
  if (severities.includes('warning')) return 'warning'
  return 'success'
})

const cardStyle = computed(() => {
  if (!props.node.online) return { '--node-tone': 'var(--offline)', '--node-glow': 'var(--glow-accent)' }
  const s = cardSeverity.value
  if (s === 'critical') return { '--node-tone': 'var(--critical)', '--node-glow': 'var(--glow-critical)' }
  if (s === 'warning') return { '--node-tone': 'var(--warning)', '--node-glow': 'var(--glow-warning)' }
  return { '--node-tone': 'var(--success)', '--node-glow': 'var(--glow-success)' }
})

const metrics = computed(() => {
  const networkType = visualSettings.value.metricCharts.network as MetricChartType
  return [
    { key: 'cpu' as MetricKey, id: 'cpu', label: metricLabels.value.cpu, value: `${Math.round(props.node.cpuUsagePercent)}%`, data: historySeries('cpuPercent', fallbackHistories.cpu.value), color: 'var(--accent)', unit: '%', max: 100, severity: visibleSeverity('cpu'), chartType: visualSettings.value.metricCharts.cpu },
    { key: 'temperature' as MetricKey, id: 'temperature', label: metricLabels.value.temperature, value: props.node.primaryTemperatureCelsius == null ? '--' : `${Math.round(props.node.primaryTemperatureCelsius)}°C`, data: historySeries('temperatureCelsius', fallbackHistories.temperature.value), color: 'var(--critical)', unit: '°', max: 120, severity: visibleSeverity('temperature'), chartType: visualSettings.value.metricCharts.temperature },
    { key: 'memory' as MetricKey, id: 'memory', label: metricLabels.value.memory, value: `${Math.round(props.node.memoryUsagePercent)}%`, data: historySeries('memoryPercent', fallbackHistories.memory.value), color: 'var(--warning)', unit: '%', max: 100, severity: visibleSeverity('memory'), chartType: visualSettings.value.metricCharts.memory },
    { key: 'disk' as MetricKey, id: 'disk', label: metricLabels.value.disk, value: `${Math.round(props.node.primaryDiskUsagePercent)}%`, data: historySeries('diskPercent', fallbackHistories.disk.value), color: 'var(--success)', unit: '%', max: 100, severity: visibleSeverity('disk'), chartType: visualSettings.value.metricCharts.disk },
    { key: 'network' as MetricKey, id: 'download', label: metricLabels.value.download, value: formatNetworkRateMBps(downloadRate.value), data: downloadVisualRates.value, color: 'var(--accent-light)', unit: '%', max: 100, severity: 'normal', chartType: networkType, chartLabel: formatNetworkRateMBps(downloadRate.value) },
    { key: 'network' as MetricKey, id: 'upload', label: metricLabels.value.upload, value: formatNetworkRateMBps(uploadRate.value), data: uploadVisualRates.value, color: 'var(--text-secondary)', unit: '%', max: 100, severity: 'normal', chartType: networkType, chartLabel: formatNetworkRateMBps(uploadRate.value) }
  ]
})

const lastUpdate = computed(() => {
  if (!props.node.lastUpdateUnix) return '--'
  const diff = Math.max(0, Date.now() / 1000 - props.node.lastUpdateUnix)
  if (diff < 60) return `${Math.round(diff)}${translate('settings.second')}`
  if (diff < 3600) return `${Math.round(diff / 60)}${translate('settings.minute')}`
  if (diff < 86400) return `${Math.round(diff / 3600)}${translate('settings.hour')}`
  return `${Math.round(diff / 86400)}${translate('settings.day')}`
})

function goToDetails() {
  if (props.editing) return
  router.push(`/nodes/${props.node.id}`)
}

function handleKeydown(event: KeyboardEvent) {
  if (props.editing) return
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault()
    goToDetails()
  }
}
</script>

<template>
  <article
    class="node-card glass-card fade-in"
    :class="[statusClass, { offline: !node.online, editing }]"
    :style="cardStyle"
    role="button"
    :tabindex="editing ? -1 : 0"
    :aria-disabled="editing ? 'true' : 'false'"
    :aria-label="editing ? translate('dashboard.layoutCardLocked', { name: node.name }) : translate('dashboard.openDetails', { name: node.name })"
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
        <svg v-if="hasDetailsAlert && props.node.online" class="card-alert-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
          <path d="M8 1L1 15h14L8 1z" fill="currentColor"/>
          <path d="M8 5v4" stroke="var(--bg-primary)" stroke-width="1.5" stroke-linecap="round" fill="none"/>
          <circle cx="8" cy="11.5" r="1" fill="var(--bg-primary)"/>
        </svg>
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
            <span v-if="metric.id === causerMetricId" class="metric-alert-chip" :class="statusClass">{{ findIssuesResult.level === 'critical' ? 'CRITICAL' : 'WARNING' }}</span>
          </span>
        </div>
        <MiniChart
          :data="metric.data"
          :color="metric.color"
          :height="metric.chartType === 'radial-gauge' ? 54 : 42"
          :chart-type="metric.chartType"
          :unit="metric.unit"
          :max="metric.max"
          :display-value="metric.chartLabel"
        />
      </section>
    </div>

    <div v-if="node.lastError" class="node-error text-mono">{{ node.lastError }}</div>

    <div class="card-footer text-mono">
      <span>v{{ node.agentVersion || '--' }}</span>
      <span>{{ translate('card.updated') }} {{ lastUpdate }}</span>
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

.node-card.editing {
  cursor: grab;
  outline: 1px dashed color-mix(in srgb, var(--accent) 70%, transparent);
  outline-offset: -0.35rem;
  user-select: none;
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
  display: flex;
  align-items: center;
  gap: 6px;
  justify-content: flex-end;
}

.card-alert-icon {
  flex-shrink: 0;
  color: var(--warning);
  filter: drop-shadow(0 0 4px var(--glow-warning));
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
