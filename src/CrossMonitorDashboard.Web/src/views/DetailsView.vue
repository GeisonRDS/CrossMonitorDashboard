<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { getNodeDetails, getNodeHistory } from '../api/dashboard'
import { useTheme, type MetricKey } from '../composables/useTheme'
import { useI18n } from '../composables/useI18n'
import MiniChart from '../components/MiniChart.vue'

const route = useRoute()
const router = useRouter()
const nodeId = route.params.id as string
const { visualSettings } = useTheme()
const { translate } = useI18n()

const details = ref<NodeDetails | null>(null)
const history = ref<HistoryDataPoint[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
let refreshTimer: ReturnType<typeof setInterval> | null = null

async function load(silent = false) {
  if (!silent) loading.value = true
  error.value = null
  try {
    const [d, h] = await Promise.all([getNodeDetails(nodeId), getNodeHistory(nodeId)])
    details.value = d
    history.value = h.slice(-120)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  load()
  refreshTimer = setInterval(() => load(true), visualSettings.value.refreshSeconds * 1000)
  window.addEventListener('keydown', onKeydown)
})

onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer)
  window.removeEventListener('keydown', onKeydown)
})

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') router.push('/')
}

const hasHistory = computed(() => history.value.length > 1)

function series(field: keyof Pick<HistoryDataPoint, 'cpuPercent' | 'memoryPercent' | 'diskPercent' | 'temperatureCelsius'>) {
  return history.value.map(p => p[field])
}

const networkSeries = computed(() => {
  if (history.value.length < 2) return []
  const result: number[] = []
  for (let index = 1; index < history.value.length; index++) {
    const previous = history.value[index - 1]
    const current = history.value[index]
    if (previous.networkInterfaceName && current.networkInterfaceName && previous.networkInterfaceName !== current.networkInterfaceName) {
      result.push(0)
      continue
    }
    result.push(Math.max(0, ((current.rxBytes - previous.rxBytes) + (current.txBytes - previous.txBytes)) / 1024 / 1024))
  }
  return result
})

const primaryNetworkInterface = computed(() => details.value?.networkInterfaces.find(item => item.isPrimary) ?? details.value?.networkInterfaces[0] ?? null)

const chartMetrics = computed(() => {
  if (!details.value) return []
  return [
    { key: 'cpu' as MetricKey, label: translate('metrics.cpuFull'), value: `${Math.round(details.value.cpuUsagePercent)}%`, data: series('cpuPercent'), color: 'var(--accent)', unit: '%', max: 100 },
    { key: 'memory' as MetricKey, label: translate('metrics.memoryFull'), value: `${Math.round(details.value.memoryUsagePercent)}%`, data: series('memoryPercent'), color: 'var(--warning)', unit: '%', max: 100 },
    { key: 'disk' as MetricKey, label: translate('metrics.diskFull'), value: `${Math.round(details.value.primaryDiskUsagePercent)}%`, data: series('diskPercent'), color: 'var(--success)', unit: '%', max: 100 },
    { key: 'temperature' as MetricKey, label: translate('metrics.temperatureFull'), value: details.value.primaryTemperatureCelsius == null ? '--' : `${Math.round(details.value.primaryTemperatureCelsius)}°C`, data: series('temperatureCelsius'), color: 'var(--critical)', unit: '°', max: 120 },
    { key: 'network' as MetricKey, label: translate('metrics.networkDelta'), value: humanBytes((primaryNetworkInterface.value?.rxBytes ?? 0) + (primaryNetworkInterface.value?.txBytes ?? 0)), data: networkSeries.value, color: 'var(--accent-light)', unit: 'M', max: Math.max(10, ...networkSeries.value) }
  ]
})

function humanBytes(bytes: number): string {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1)
  return `${Number((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

const statusCause = computed(() => {
  const d = details.value
  if (!d) return null
  const cpu = d.cpuUsagePercent
  const memory = d.memoryUsagePercent
  const diskCritical = d.disks.find(item => item.usagePercent >= 90)
  const tempCritical = d.temperatures.find(item => item.celsius >= 85)
  const warningTemp = d.temperatures.find(item => item.celsius >= 70)
  const collector = d.collectorStatuses.find(item => item.hasError)

  if (cpu >= 90) return { text: translate('details.cause.cpuCritical', { value: Math.round(cpu) }), level: 'critical' as const, type: 'cpu' as const }
  if (memory >= 90) return { text: translate('details.cause.memoryCritical', { value: Math.round(memory) }), level: 'critical' as const, type: 'memory' as const }
  if (diskCritical) return { text: translate('details.cause.diskCritical', { mount: diskCritical.mountPoint, value: Math.round(diskCritical.usagePercent) }), level: 'critical' as const, type: 'disk' as const }
  if (tempCritical) return { text: translate('details.cause.tempCritical', { sensor: tempCritical.sensor, value: Math.round(tempCritical.celsius) }), level: 'critical' as const, type: 'temperature' as const }
  if (cpu >= 75) return { text: translate('details.cause.cpuWarning', { value: Math.round(cpu) }), level: 'warning' as const, type: 'cpu' as const }
  if (memory >= 75) return { text: translate('details.cause.memoryWarning', { value: Math.round(memory) }), level: 'warning' as const, type: 'memory' as const }
  if (d.disks.some(item => item.usagePercent >= 80)) return { text: translate('details.cause.diskWarning', {}), level: 'warning' as const, type: 'disk' as const }
  if (warningTemp) return { text: translate('details.cause.tempWarning', { sensor: warningTemp.sensor, value: Math.round(warningTemp.celsius) }), level: 'warning' as const, type: 'temperature' as const }
  if (collector) return { text: translate('details.cause.collectorError', { name: collector.name }), level: 'warning' as const, type: 'collector' as const }
  if (d.lastError) return { text: d.lastError, level: 'warning' as const, type: 'collector' as const }
  return null
})

function humanUptime(uptime: string): string {
  if (!uptime) return '--'
  const seconds = parseInt(uptime)
  if (isNaN(seconds)) return uptime
  if (seconds < 60) return `${seconds}s`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`
  return `${Math.floor(seconds / 86400)}d ${Math.floor((seconds % 86400) / 3600)}h`
}
</script>

<template>
  <div class="details-page fade-in">
    <button class="back-btn" aria-label="Back to dashboard" @click="router.push('/')">{{ translate('details.back') }}</button>

    <div v-if="loading" class="loading glass-card">{{ translate('details.loading') }}</div>
    <div v-else-if="error" class="error glass-card">{{ error }}</div>

    <div v-else-if="details" class="details-content">
      <section class="node-title glass-card">
        <div>
          <span class="eyebrow text-mono">{{ details.type }} / {{ details.status }}</span>
          <h1>{{ details.name }}</h1>
          <p class="text-mono">{{ details.os }} / {{ details.platform }}</p>
        </div>
        <div class="node-meta text-mono">
          <span>{{ details.host || '--' }}</span>
          <span>v{{ details.agentVersion || '--' }}</span>
        </div>
      </section>

      <div v-if="statusCause" class="cause-banner" :class="`cause-${statusCause.level}`">
        <span class="cause-label">{{ statusCause.level === 'critical' ? 'CRITICAL' : 'WARNING' }}</span>
        <span class="cause-text">{{ statusCause.text }}</span>
      </div>

      <div v-if="!hasHistory" class="empty-history glass-card">
        {{ translate('details.noHistory') }}
      </div>

      <section v-else class="charts-grid">
        <article v-for="metric in chartMetrics" :key="metric.key" class="chart-card glass-card">
          <div class="chart-title">
            <span class="text-mono">{{ metric.label }}</span>
            <strong>{{ metric.value }}</strong>
          </div>
          <MiniChart
            :data="metric.data"
            :color="metric.color"
            :height="220"
            :chart-type="visualSettings.metricCharts[metric.key]"
            :unit="metric.unit"
            :max="metric.max"
            :compact="false"
          />
        </article>
      </section>

      <section class="detail-section glass-card">
        <h3>{{ translate('details.systemInfo') }}</h3>
        <div class="info-grid">
          <div class="info-item"><span>{{ translate('details.host') }}</span><strong>{{ details.host || '--' }}</strong></div>
          <div class="info-item"><span>{{ translate('details.kernel') }}</span><strong>{{ details.kernel || '--' }}</strong></div>
          <div class="info-item"><span>{{ translate('details.arch') }}</span><strong>{{ details.architecture || '--' }}</strong></div>
          <div class="info-item"><span>{{ translate('details.uptime') }}</span><strong>{{ humanUptime(details.uptime) }}</strong></div>
        </div>
      </section>

      <div class="inventory-grid">
        <section class="detail-section glass-card">
          <h3>{{ translate('details.disks') }}</h3>
          <div v-if="details.disks.length === 0" class="empty-inline">{{ translate('details.noDisks') }}</div>
          <div v-for="disk in details.disks" :key="disk.mountPoint" class="list-row" :class="{ 'item-offending': disk.usagePercent >= 90 }">
            <span>{{ disk.mountPoint }} / {{ disk.filesystem }}</span>
            <strong>{{ humanBytes(disk.usedBytes) }} / {{ humanBytes(disk.totalBytes) }} ({{ Math.round(disk.usagePercent) }}%)</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>{{ translate('details.network') }}</h3>
          <div v-if="details.networkInterfaces.length === 0" class="empty-inline">{{ translate('details.noNetwork') }}</div>
          <div v-for="ni in details.networkInterfaces" :key="ni.name" class="list-row" :class="{ 'item-primary': ni.isPrimary }">
            <span>{{ ni.name }} <em v-if="ni.isPrimary" class="primary-chip">{{ translate('details.primaryNetwork') }}</em></span>
            <strong>{{ translate('details.rx') }} {{ humanBytes(ni.rxBytes) }} / {{ translate('details.tx') }} {{ humanBytes(ni.txBytes) }}</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>{{ translate('details.temperatures') }}</h3>
          <div v-if="details.temperatures.length === 0" class="empty-inline">{{ translate('details.noTemperatures') }}</div>
          <div v-for="t in details.temperatures" :key="t.sensor" class="list-row" :class="{ 'item-offending': t.celsius >= 85 }">
            <span>{{ t.sensor }}</span>
            <strong :class="t.celsius >= 85 ? 'status-critical' : t.celsius >= 70 ? 'status-warning' : 'status-ok'">{{ Math.round(t.celsius) }}°C</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>{{ translate('details.collectors') }}</h3>
          <div v-if="details.collectorStatuses.length === 0" class="empty-inline">{{ translate('details.noCollectors') }}</div>
          <div v-for="c in details.collectorStatuses" :key="c.name" class="list-row" :class="{ 'item-offending': c.hasError }">
            <span>{{ c.name }}</span>
            <strong :class="c.hasError ? 'status-critical' : 'status-ok'">{{ c.enabled ? (c.hasError ? translate('details.collectorError2') : translate('details.collectorOk')) : translate('details.collectorDisabled') }}</strong>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<style scoped>
.details-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.back-btn {
  align-self: flex-start;
  padding: 0.58rem 0.9rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-radius: 12px;
  transition: all var(--transition-speed);
}

.back-btn:hover {
  border-color: var(--accent);
  box-shadow: 0 0 18px var(--glow-accent);
}

.details-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.node-title {
  padding: 1rem;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: center;
}

.node-title h1 {
  font-size: clamp(1.35rem, 2vw, 2rem);
  line-height: 1;
}

.node-title p,
.node-meta {
  color: var(--text-secondary);
  font-size: 0.75rem;
}

.node-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.35rem;
}

.eyebrow {
  display: block;
  margin-bottom: 0.35rem;
  color: var(--accent-light);
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.14em;
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1rem;
}

.chart-card,
.detail-section,
.empty-history {
  padding: 1rem;
}

.chart-title {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 0.75rem;
  margin-bottom: 0.35rem;
}

.chart-title span,
.detail-section h3 {
  color: var(--text-secondary);
  font-size: 0.74rem;
  text-transform: uppercase;
  letter-spacing: 0.1em;
}

.chart-title strong {
  color: var(--text-primary);
  font-family: var(--font-mono);
}

.info-grid,
.inventory-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 0.75rem;
}

.info-item,
.list-row {
  padding: 0.7rem;
  border-radius: 12px;
  background: var(--metric-tile-bg);
  border: 1px solid rgba(255,255,255,0.055);
}

.info-item span,
.list-row span {
  display: block;
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.68rem;
  margin-bottom: 0.32rem;
}

.info-item strong,
.list-row strong {
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.78rem;
  overflow-wrap: anywhere;
}

.cause-banner {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.65rem 1rem;
  border-radius: 12px;
  font-family: var(--font-mono);
  font-size: 0.78rem;
}

.cause-banner.cause-critical {
  background: color-mix(in srgb, var(--critical) 14%, var(--bg-card));
  border: 1px solid color-mix(in srgb, var(--critical) 50%, transparent);
  color: var(--critical);
}

.cause-banner.cause-warning {
  background: color-mix(in srgb, var(--warning) 12%, var(--bg-card));
  border: 1px solid color-mix(in srgb, var(--warning) 45%, transparent);
  color: var(--warning);
}

.cause-label {
  font-weight: 800;
  font-size: 0.65rem;
  padding: 3px 7px;
  border-radius: 6px;
  background: color-mix(in srgb, currentColor 18%, transparent);
  border: 1px solid color-mix(in srgb, currentColor 35%, transparent);
}

.list-row.item-offending {
  border-color: var(--critical);
  background: color-mix(in srgb, var(--critical) 10%, var(--metric-tile-bg));
  box-shadow: 0 0 14px var(--glow-critical);
}

.list-row.item-primary {
  border-color: color-mix(in srgb, var(--accent) 55%, var(--border-color));
  background: color-mix(in srgb, var(--accent) 9%, var(--metric-tile-bg));
}

.primary-chip {
  display: inline-flex;
  margin-left: 0.35rem;
  padding: 2px 6px;
  border-radius: 999px;
  color: var(--accent);
  border: 1px solid color-mix(in srgb, var(--accent) 40%, transparent);
  font-style: normal;
  font-size: 0.58rem;
}

.empty-history,
.empty-inline,
.loading,
.error {
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.loading,
.error {
  padding: 2rem;
  text-align: center;
}

@media (max-width: 620px) {
  .node-title {
    align-items: flex-start;
    flex-direction: column;
  }

  .node-meta {
    align-items: flex-start;
  }
}
</style>
