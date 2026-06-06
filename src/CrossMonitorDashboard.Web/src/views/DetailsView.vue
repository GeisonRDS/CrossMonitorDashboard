<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { getNodeDetails, getNodeHistory } from '../api/dashboard'
import { useTheme, type MetricKey } from '../composables/useTheme'
import MiniChart from '../components/MiniChart.vue'

const route = useRoute()
const router = useRouter()
const nodeId = route.params.id as string
const { visualSettings } = useTheme()

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
  if (history.value.length === 0) return []
  const base = history.value[0]
  return history.value.map(p => Math.max(0, ((p.rxBytes - base.rxBytes) + (p.txBytes - base.txBytes)) / 1024 / 1024))
})

const chartMetrics = computed(() => {
  if (!details.value) return []
  return [
    { key: 'cpu' as MetricKey, label: 'CPU', value: `${Math.round(details.value.cpuUsagePercent)}%`, data: series('cpuPercent'), color: 'var(--accent)', unit: '%', max: 100 },
    { key: 'memory' as MetricKey, label: 'RAM', value: `${Math.round(details.value.memoryUsagePercent)}%`, data: series('memoryPercent'), color: 'var(--warning)', unit: '%', max: 100 },
    { key: 'disk' as MetricKey, label: 'Disk', value: `${Math.round(details.value.primaryDiskUsagePercent)}%`, data: series('diskPercent'), color: 'var(--success)', unit: '%', max: 100 },
    { key: 'temperature' as MetricKey, label: 'Temperature', value: details.value.primaryTemperatureCelsius == null ? '--' : `${Math.round(details.value.primaryTemperatureCelsius)}°C`, data: series('temperatureCelsius'), color: 'var(--critical)', unit: '°', max: 120 },
    { key: 'network' as MetricKey, label: 'Network delta', value: humanBytes((details.value.networkInterfaces[0]?.rxBytes ?? 0) + (details.value.networkInterfaces[0]?.txBytes ?? 0)), data: networkSeries.value, color: 'var(--accent-light)', unit: 'M', max: Math.max(10, ...networkSeries.value) }
  ]
})

function humanBytes(bytes: number): string {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1)
  return `${Number((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

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
    <button class="back-btn" aria-label="Back to dashboard" @click="router.push('/')">← Back</button>

    <div v-if="loading" class="loading glass-card">Loading telemetry...</div>
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

      <div v-if="!hasHistory" class="empty-history glass-card">
        Nenhum dado histórico disponível ainda. Aguarde alguns ciclos de polling.
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
        <h3>System Info</h3>
        <div class="info-grid">
          <div class="info-item"><span>Host</span><strong>{{ details.host || '--' }}</strong></div>
          <div class="info-item"><span>Kernel</span><strong>{{ details.kernel || '--' }}</strong></div>
          <div class="info-item"><span>Arch</span><strong>{{ details.architecture || '--' }}</strong></div>
          <div class="info-item"><span>Uptime</span><strong>{{ humanUptime(details.uptime) }}</strong></div>
        </div>
      </section>

      <div class="inventory-grid">
        <section class="detail-section glass-card">
          <h3>Disks</h3>
          <div v-if="details.disks.length === 0" class="empty-inline">No disks reported.</div>
          <div v-for="disk in details.disks" :key="disk.mountPoint" class="list-row">
            <span>{{ disk.mountPoint }} / {{ disk.filesystem }}</span>
            <strong>{{ humanBytes(disk.usedBytes) }} / {{ humanBytes(disk.totalBytes) }} ({{ Math.round(disk.usagePercent) }}%)</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Network</h3>
          <div v-if="details.networkInterfaces.length === 0" class="empty-inline">No network interfaces reported.</div>
          <div v-for="ni in details.networkInterfaces" :key="ni.name" class="list-row">
            <span>{{ ni.name }}</span>
            <strong>RX {{ humanBytes(ni.rxBytes) }} / TX {{ humanBytes(ni.txBytes) }}</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Temperatures</h3>
          <div v-if="details.temperatures.length === 0" class="empty-inline">No temperature sensors reported.</div>
          <div v-for="t in details.temperatures" :key="t.sensor" class="list-row">
            <span>{{ t.sensor }}</span>
            <strong :class="t.celsius >= 85 ? 'status-critical' : t.celsius >= 70 ? 'status-warning' : 'status-ok'">{{ Math.round(t.celsius) }}°C</strong>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Collectors</h3>
          <div v-if="details.collectorStatuses.length === 0" class="empty-inline">No collectors reported.</div>
          <div v-for="c in details.collectorStatuses" :key="c.name" class="list-row">
            <span>{{ c.name }}</span>
            <strong :class="c.hasError ? 'status-critical' : 'status-ok'">{{ c.enabled ? (c.hasError ? 'Error' : 'OK') : 'Disabled' }}</strong>
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
