<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import VChart from 'vue-echarts'
import 'echarts'
import type { NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { getNodeDetails, getNodeHistory } from '../api/dashboard'

const route = useRoute()
const router = useRouter()
const nodeId = route.params.id as string

const details = ref<NodeDetails | null>(null)
const history = ref<HistoryDataPoint[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const chartRefreshKey = ref(0)
let refreshTimer: ReturnType<typeof setInterval> | null = null

function cssVar(name: string) {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim()
}

function chartColor(name: string, fallback: string) {
  return cssVar(name) || fallback
}

function hexToRgba(hex: string, alpha: number) {
  if (!hex.startsWith('#')) return hex
  const normalized = hex.length === 4 ? hex.slice(1).split('').map(c => c + c).join('') : hex.slice(1)
  const value = Number.parseInt(normalized, 16)
  const r = (value >> 16) & 255
  const g = (value >> 8) & 255
  const b = value & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

async function load(silent = false) {
  if (!silent) loading.value = true
  error.value = null
  try {
    const [d, h] = await Promise.all([
      getNodeDetails(nodeId),
      getNodeHistory(nodeId)
    ])
    details.value = d
    history.value = h
    await nextTick()
    chartRefreshKey.value++
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  load()
  refreshTimer = setInterval(() => load(true), 5000)
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

function percentChartOptions(label: string, field: keyof Pick<HistoryDataPoint, 'cpuPercent' | 'memoryPercent' | 'diskPercent' | 'temperatureCelsius'>, color: string) {
  const lineColor = chartColor(color, '#3c8ce6')
  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'axis', backgroundColor: 'rgba(4,10,22,0.94)', borderColor: lineColor, textStyle: { color: '#eef6ff' } },
    grid: { left: 42, right: 16, top: 18, bottom: 28 },
    xAxis: {
      type: 'time',
      axisLine: { lineStyle: { color: 'rgba(255,255,255,0.12)' } },
      axisTick: { show: false },
      axisLabel: { color: chartColor('--text-muted', '#718096'), fontSize: 10 }
    },
    yAxis: {
      type: 'value', min: 0, max: field === 'temperatureCelsius' ? undefined : 100,
      splitLine: { lineStyle: { color: 'rgba(255,255,255,0.07)' } },
      axisLabel: { color: chartColor('--text-muted', '#718096'), fontSize: 10, formatter: field === 'temperatureCelsius' ? '{value}°' : '{value}%' }
    },
    series: [{
      name: label,
      type: 'line',
      data: history.value.map(p => [p.timestampUnix * 1000, p[field]]),
      smooth: true,
      showSymbol: history.value.length < 8,
      symbolSize: 5,
      lineStyle: { color: lineColor, width: 2.4, shadowColor: hexToRgba(lineColor, 0.4), shadowBlur: 10 },
      itemStyle: { color: lineColor },
      areaStyle: {
        color: {
          type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: hexToRgba(lineColor, 0.28) },
            { offset: 1, color: hexToRgba(lineColor, 0.02) }
          ]
        }
      }
    }]
  }
}

const cpuChartOptions = computed(() => percentChartOptions('CPU', 'cpuPercent', '--accent'))
const memChartOptions = computed(() => percentChartOptions('Memory', 'memoryPercent', '--warning'))
const diskChartOptions = computed(() => percentChartOptions('Disk', 'diskPercent', '--success'))
const tempChartOptions = computed(() => percentChartOptions('Temperature', 'temperatureCelsius', '--critical'))

const networkChartOptions = computed(() => {
  const rxColor = chartColor('--accent', '#3c8ce6')
  const txColor = chartColor('--success', '#00c853')
  return {
    tooltip: { trigger: 'axis', backgroundColor: 'rgba(4,10,22,0.94)', textStyle: { color: '#eef6ff' } },
    legend: { top: 0, right: 8, textStyle: { color: chartColor('--text-secondary', '#9aa6b2') } },
    grid: { left: 46, right: 16, top: 34, bottom: 28 },
    xAxis: { type: 'time', axisTick: { show: false }, axisLabel: { color: chartColor('--text-muted', '#718096'), fontSize: 10 } },
    yAxis: { type: 'value', splitLine: { lineStyle: { color: 'rgba(255,255,255,0.07)' } }, axisLabel: { color: chartColor('--text-muted', '#718096'), fontSize: 10, formatter: humanBytesShort } },
    series: [
      { name: 'RX', type: 'line', smooth: true, showSymbol: false, data: history.value.map(p => [p.timestampUnix * 1000, p.rxBytes]), lineStyle: { color: rxColor, width: 2 }, itemStyle: { color: rxColor } },
      { name: 'TX', type: 'line', smooth: true, showSymbol: false, data: history.value.map(p => [p.timestampUnix * 1000, p.txBytes]), lineStyle: { color: txColor, width: 2 }, itemStyle: { color: txColor } }
    ]
  }
})

function humanBytes(bytes: number): string {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1)
  return `${Number((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

function humanBytesShort(bytes: number): string {
  if (!bytes) return '0'
  const k = 1024
  const sizes = ['', 'K', 'M', 'G', 'T']
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1)
  return `${Number((bytes / Math.pow(k, i)).toFixed(1))}${sizes[i]}`
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
    <div class="page-header">
      <button class="back-btn" aria-label="Back to dashboard" @click="router.push('/')">←</button>
      <div>
        <p class="eyebrow text-mono">Node telemetry</p>
        <h1 v-if="details">{{ details.name }} <span>/ {{ details.type }}</span></h1>
        <h1 v-else>Node Details</h1>
      </div>
    </div>

    <div v-if="loading" class="loading glass-card">Loading telemetry...</div>
    <div v-else-if="error" class="error glass-card">{{ error }}</div>
    <div v-else-if="details" class="details-content">
      <section class="hero-panel glass-card" :class="details.status">
        <div>
          <p class="eyebrow text-mono">{{ details.os }} / {{ details.platform }}</p>
          <h2>{{ details.host || details.name }}</h2>
          <p v-if="details.lastError" class="last-error text-mono">{{ details.lastError }}</p>
        </div>
        <div class="hero-metrics">
          <div><span>CPU</span><strong>{{ Math.round(details.cpuUsagePercent) }}%</strong></div>
          <div><span>MEM</span><strong>{{ Math.round(details.memoryUsagePercent) }}%</strong></div>
          <div><span>DSK</span><strong>{{ Math.round(details.primaryDiskUsagePercent) }}%</strong></div>
          <div><span>TEMP</span><strong>{{ details.primaryTemperatureCelsius == null ? '--' : `${Math.round(details.primaryTemperatureCelsius)}°` }}</strong></div>
        </div>
      </section>

      <section class="detail-section glass-card">
        <h3>System Info</h3>
        <div class="info-grid">
          <div class="info-item"><span class="label">Host</span><span class="value">{{ details.host || '--' }}</span></div>
          <div class="info-item"><span class="label">Kernel</span><span class="value">{{ details.kernel || '--' }}</span></div>
          <div class="info-item"><span class="label">Arch</span><span class="value">{{ details.architecture || '--' }}</span></div>
          <div class="info-item"><span class="label">Uptime</span><span class="value">{{ humanUptime(details.uptime) }}</span></div>
          <div class="info-item"><span class="label">Agent</span><span class="value">v{{ details.agentVersion || '--' }}</span></div>
          <div class="info-item"><span class="label">Status</span><span class="value" :class="details.status === 'healthy' ? 'status-ok' : `status-${details.status}`">{{ details.status }}</span></div>
        </div>
      </section>

      <div class="empty-history glass-card" v-if="!hasHistory">
        Nenhum dado histórico disponível ainda. Aguarde alguns ciclos de polling.
      </div>

      <div class="charts-grid" v-else>
        <section class="chart-card glass-card">
          <h3>CPU Usage</h3>
          <div class="big-stat">{{ Math.round(details.cpu.usagePercent) }}%</div>
          <VChart :key="`cpu-${chartRefreshKey}`" :option="cpuChartOptions" class="chart" autoresize />
        </section>
        <section class="chart-card glass-card">
          <h3>Memory Usage</h3>
          <div class="big-stat">{{ humanBytes(details.memory.usedBytes) }} / {{ humanBytes(details.memory.totalBytes) }}</div>
          <VChart :key="`mem-${chartRefreshKey}`" :option="memChartOptions" class="chart" autoresize />
        </section>
        <section class="chart-card glass-card">
          <h3>Disk Usage</h3>
          <VChart :key="`disk-${chartRefreshKey}`" :option="diskChartOptions" class="chart" autoresize />
        </section>
        <section class="chart-card glass-card">
          <h3>Temperature</h3>
          <VChart :key="`temp-${chartRefreshKey}`" :option="tempChartOptions" class="chart" autoresize />
        </section>
        <section class="chart-card glass-card wide">
          <h3>Network RX/TX</h3>
          <VChart :key="`net-${chartRefreshKey}`" :option="networkChartOptions" class="chart" autoresize />
        </section>
      </div>

      <div class="inventory-grid">
        <section class="detail-section glass-card">
          <h3>Disks</h3>
          <div v-if="details.disks.length === 0" class="empty-inline">No disks reported.</div>
          <div v-for="disk in details.disks" :key="disk.mountPoint" class="disk-item">
            <div class="disk-label">
              <span>{{ disk.mountPoint }} ({{ disk.filesystem }})</span>
              <span>{{ humanBytes(disk.usedBytes) }} / {{ humanBytes(disk.totalBytes) }} ({{ Math.round(disk.usagePercent) }}%)</span>
            </div>
            <div class="bar-track"><div class="bar-fill" :style="{ width: `${Math.min(disk.usagePercent, 100)}%` }"></div></div>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Network</h3>
          <div v-if="details.networkInterfaces.length === 0" class="empty-inline">No network interfaces reported.</div>
          <div class="info-grid compact">
            <div v-for="ni in details.networkInterfaces" :key="ni.name" class="info-item">
              <span class="label">{{ ni.name }}</span>
              <span class="value">RX {{ humanBytes(ni.rxBytes) }}</span>
              <span class="value">TX {{ humanBytes(ni.txBytes) }}</span>
            </div>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Temperatures</h3>
          <div v-if="details.temperatures.length === 0" class="empty-inline">No temperature sensors reported.</div>
          <div class="info-grid compact">
            <div v-for="t in details.temperatures" :key="t.sensor" class="info-item">
              <span class="label">{{ t.sensor }}</span>
              <span class="value" :class="t.celsius >= 85 ? 'status-critical' : t.celsius >= 70 ? 'status-warning' : 'status-ok'">{{ Math.round(t.celsius) }}°C</span>
            </div>
          </div>
        </section>

        <section class="detail-section glass-card">
          <h3>Collectors</h3>
          <div v-if="details.collectorStatuses.length === 0" class="empty-inline">No collectors reported.</div>
          <div v-for="c in details.collectorStatuses" :key="c.name" class="collector-item">
            <span>{{ c.name }}</span>
            <span :class="c.hasError ? 'status-critical' : 'status-ok'">{{ c.enabled ? (c.hasError ? 'Error' : 'OK') : 'Disabled' }}</span>
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
  gap: 1.1rem;
}

.page-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.page-header h1 {
  font-size: clamp(1.45rem, 2vw, 2rem);
  line-height: 1;
}

.page-header h1 span {
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.82rem;
}

.eyebrow {
  margin-bottom: 0.32rem;
  color: var(--accent-light);
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.16em;
}

.back-btn {
  width: 42px;
  height: 42px;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-radius: 14px;
  font-size: 1.2rem;
  transition: all var(--transition-speed);
}

.back-btn:hover {
  background: var(--bg-card-hover);
  border-color: var(--accent);
  box-shadow: 0 0 18px var(--glow-accent);
}

.details-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.hero-panel {
  position: relative;
  overflow: hidden;
  padding: 1.35rem;
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 1rem;
  align-items: center;
}

.hero-panel::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: radial-gradient(circle at 80% 15%, var(--glow-accent), transparent 42%);
}

.hero-panel h2 {
  position: relative;
  font-size: clamp(1.4rem, 2vw, 2.4rem);
}

.last-error {
  position: relative;
  margin-top: 0.75rem;
  color: var(--warning);
  font-size: 0.74rem;
}

.hero-metrics {
  position: relative;
  display: grid;
  grid-template-columns: repeat(4, minmax(72px, 1fr));
  gap: 0.65rem;
}

.hero-metrics div {
  padding: 0.75rem;
  min-width: 76px;
  border-radius: 14px;
  border: 1px solid var(--border-color);
  background: rgba(0,0,0,0.18);
}

.hero-metrics span {
  display: block;
  color: var(--text-muted);
  font-size: 0.65rem;
  font-family: var(--font-mono);
}

.hero-metrics strong {
  display: block;
  margin-top: 0.25rem;
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 1.1rem;
}

.detail-section,
.chart-card,
.empty-history {
  padding: 1.15rem;
}

.detail-section h3,
.chart-card h3 {
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-secondary);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.1em;
  margin-bottom: 0.8rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
  gap: 0.75rem;
}

.info-grid.compact {
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 0.65rem;
  border-radius: 12px;
  background: rgba(0,0,0,0.16);
  border: 1px solid rgba(255,255,255,0.04);
}

.label {
  font-size: 0.68rem;
  color: var(--text-muted);
  font-family: var(--font-mono);
}

.value {
  font-size: 0.86rem;
  color: var(--text-primary);
  font-family: var(--font-mono);
  overflow-wrap: anywhere;
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.chart-card.wide {
  grid-column: 1 / -1;
}

.chart {
  width: 100%;
  height: 245px;
}

.big-stat {
  font-family: var(--font-mono);
  font-size: 1rem;
  font-weight: 800;
  color: var(--accent);
  margin-bottom: 0.35rem;
}

.empty-history,
.empty-inline {
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.inventory-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.bar-track {
  width: 100%;
  height: 7px;
  background: rgba(255, 255, 255, 0.06);
  border-radius: 999px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: var(--accent);
  border-radius: 999px;
  transition: width 0.5s ease;
  box-shadow: 0 0 12px var(--glow-accent);
}

.disk-item {
  margin-bottom: 0.75rem;
}

.disk-label,
.collector-item {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
  font-size: 0.75rem;
  font-family: var(--font-mono);
  margin-bottom: 5px;
}

.collector-item {
  padding: 7px 0;
  border-bottom: 1px solid var(--border-color);
}

.collector-item:last-child {
  border-bottom: none;
}

.loading,
.error {
  padding: 2rem;
  text-align: center;
  color: var(--text-muted);
}

@media (max-width: 1040px) {
  .hero-panel,
  .charts-grid,
  .inventory-grid {
    grid-template-columns: 1fr;
  }

  .hero-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 520px) {
  .hero-metrics {
    grid-template-columns: 1fr;
  }
}
</style>
