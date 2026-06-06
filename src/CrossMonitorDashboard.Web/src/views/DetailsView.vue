<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
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

onMounted(async () => {
  try {
    const [d, h] = await Promise.all([
      getNodeDetails(nodeId),
      getNodeHistory(nodeId)
    ])
    details.value = d
    history.value = h
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

const cpuChartOptions = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 50, right: 15, top: 15, bottom: 30 },
  xAxis: {
    type: 'time',
    axisLabel: { color: 'var(--text-muted)', fontSize: 11 }
  },
  yAxis: {
    type: 'value', min: 0, max: 100,
    axisLabel: { color: 'var(--text-muted)', fontSize: 11, formatter: '{value}%' }
  },
  series: [{
    type: 'line',
    data: history.value.map(p => [p.timestampUnix * 1000, p.cpuPercent]),
    smooth: true, showSymbol: false,
    lineStyle: { color: 'var(--accent)', width: 2 },
    areaStyle: {
      color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [
        { offset: 0, color: 'var(--glow-accent)' },
        { offset: 1, color: 'transparent' }
      ]}
    }
  }]
}))

const memChartOptions = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 50, right: 15, top: 15, bottom: 30 },
  xAxis: {
    type: 'time',
    axisLabel: { color: 'var(--text-muted)', fontSize: 11 }
  },
  yAxis: {
    type: 'value', min: 0, max: 100,
    axisLabel: { color: 'var(--text-muted)', fontSize: 11, formatter: '{value}%' }
  },
  series: [{
    type: 'line',
    data: history.value.map(p => [p.timestampUnix * 1000, p.memoryPercent]),
    smooth: true, showSymbol: false,
    lineStyle: { color: 'var(--warning)', width: 2 },
    areaStyle: {
      color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [
        { offset: 0, color: 'var(--glow-warning)' },
        { offset: 1, color: 'transparent' }
      ]}
    }
  }]
}))

const diskChartOptions = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 50, right: 15, top: 15, bottom: 30 },
  xAxis: {
    type: 'time',
    axisLabel: { color: 'var(--text-muted)', fontSize: 11 }
  },
  yAxis: {
    type: 'value', min: 0, max: 100,
    axisLabel: { color: 'var(--text-muted)', fontSize: 11, formatter: '{value}%' }
  },
  series: [{
    type: 'line',
    data: history.value.map(p => [p.timestampUnix * 1000, p.diskPercent]),
    smooth: true, showSymbol: false,
    lineStyle: { color: 'var(--success)', width: 2 },
    areaStyle: {
      color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [
        { offset: 0, color: 'var(--glow-success)' },
        { offset: 1, color: 'transparent' }
      ]}
    }
  }]
}))

function humanBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
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
      <button class="back-btn" @click="router.push('/')">← Back</button>
      <h1 v-if="details">{{ details.name }} <span class="text-muted text-mono" style="font-size:0.8rem;">/ {{ details.type }}</span></h1>
      <h1 v-else class="text-muted">Node Details</h1>
    </div>

    <div v-if="loading" class="loading glass-card">Loading...</div>
    <div v-else-if="error" class="error glass-card">{{ error }}</div>
    <div v-else-if="details" class="details-content">
      <section class="detail-section glass-card">
        <h3>System Info</h3>
        <div class="info-grid">
          <div class="info-item"><span class="label">Host</span><span class="value">{{ details.host }}</span></div>
          <div class="info-item"><span class="label">OS</span><span class="value">{{ details.os }}</span></div>
          <div class="info-item"><span class="label">Kernel</span><span class="value">{{ details.kernel }}</span></div>
          <div class="info-item"><span class="label">Arch</span><span class="value">{{ details.architecture }}</span></div>
          <div class="info-item"><span class="label">Uptime</span><span class="value">{{ humanUptime(details.uptime) }}</span></div>
          <div class="info-item"><span class="label">Agent</span><span class="value">v{{ details.agentVersion }}</span></div>
        </div>
      </section>

      <div class="charts-grid">
        <section class="detail-section glass-card">
          <h3>CPU Usage ({{ details.cpu.cores }} cores)</h3>
          <div class="big-stat">{{ Math.round(details.cpu.usagePercent) }}%</div>
          <VChart :option="cpuChartOptions" style="height: 220px;" autoresize />
        </section>
        <section class="detail-section glass-card">
          <h3>Memory Usage</h3>
          <div class="big-stat">{{ humanBytes(details.memory.usedBytes) }} / {{ humanBytes(details.memory.totalBytes) }}</div>
          <VChart :option="memChartOptions" style="height: 220px;" autoresize />
        </section>
      </div>

      <section class="detail-section glass-card">
        <h3>Disk Usage</h3>
        <VChart :option="diskChartOptions" style="height: 200px;" autoresize />
        <div v-for="disk in details.disks" :key="disk.mountPoint" class="disk-item">
          <div class="disk-label">
            <span>{{ disk.mountPoint }} ({{ disk.filesystem }})</span>
            <span>{{ humanBytes(disk.usedBytes) }} / {{ humanBytes(disk.totalBytes) }} ({{ Math.round(disk.usagePercent) }}%)</span>
          </div>
          <div class="bar-track"><div class="bar-fill" :style="{ width: disk.usagePercent + '%' }"></div></div>
        </div>
      </section>

      <section class="detail-section glass-card">
        <h3>Network Interfaces</h3>
        <div class="info-grid">
          <div v-for="ni in details.networkInterfaces" :key="ni.name" class="info-item">
            <span class="label">{{ ni.name }}</span>
            <span class="value">RX: {{ humanBytes(ni.rxBytes) }} / TX: {{ humanBytes(ni.txBytes) }}</span>
          </div>
        </div>
      </section>

      <section class="detail-section glass-card">
        <h3>Temperatures</h3>
        <div class="info-grid">
          <div v-for="t in details.temperatures" :key="t.sensor" class="info-item">
            <span class="label">{{ t.sensor }}</span>
            <span class="value" :class="t.celsius > 80 ? 'status-critical' : t.celsius > 60 ? 'status-warning' : 'status-ok'">{{ Math.round(t.celsius) }}°C</span>
          </div>
        </div>
      </section>

      <section class="detail-section glass-card">
        <h3>Collectors</h3>
        <div v-for="c in details.collectorStatuses" :key="c.name" class="collector-item">
          <span>{{ c.name }}</span>
          <span :class="c.hasError ? 'status-critical' : 'status-ok'">{{ c.enabled ? (c.hasError ? 'Error' : 'OK') : 'Disabled' }}</span>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.details-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.back-btn {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  padding: 0.5rem 1rem;
  border-radius: 8px;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all var(--transition-speed);
}

.back-btn:hover {
  background: var(--bg-card-hover);
  border-color: var(--accent);
}

.details-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.detail-section {
  padding: 1.25rem;
}

.detail-section h3 {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.75rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 0.75rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.info-item .label {
  font-size: 0.7rem;
  color: var(--text-muted);
  font-family: var(--font-mono);
}

.info-item .value {
  font-size: 0.9rem;
  color: var(--text-primary);
  font-family: var(--font-mono);
}

.charts-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

@media (max-width: 768px) {
  .charts-grid {
    grid-template-columns: 1fr;
  }
}

.big-stat {
  font-family: var(--font-mono);
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--accent);
  margin-bottom: 0.5rem;
}

.bar-track {
  width: 100%;
  height: 6px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 3px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: var(--accent);
  border-radius: 3px;
  transition: width 0.5s ease;
}

.disk-item {
  margin-bottom: 0.5rem;
}

.disk-label {
  display: flex;
  justify-content: space-between;
  font-size: 0.75rem;
  font-family: var(--font-mono);
  margin-bottom: 4px;
}

.collector-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.8rem;
  font-family: var(--font-mono);
  padding: 6px 0;
  border-bottom: 1px solid var(--border-color);
}

.collector-item:last-child {
  border-bottom: none;
}

.loading, .error {
  padding: 2rem;
  text-align: center;
  color: var(--text-muted);
}
</style>
