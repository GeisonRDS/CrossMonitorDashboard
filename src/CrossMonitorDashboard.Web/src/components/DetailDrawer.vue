<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import VChart from 'vue-echarts'
import 'echarts'
import type { NodeDetails, HistoryDataPoint } from '../types/dashboard'
import { getNodeDetails, getNodeHistory } from '../api/dashboard'

const props = defineProps<{ nodeId: string }>()
const emit = defineEmits<{ close: [] }>()

const details = ref<NodeDetails | null>(null)
const history = ref<HistoryDataPoint[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    const [d, h] = await Promise.all([
      getNodeDetails(props.nodeId),
      getNodeHistory(props.nodeId)
    ])
    details.value = d
    history.value = h
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

watch(() => props.nodeId, load, { immediate: true })

const cpuChartOptions = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 40, right: 10, top: 10, bottom: 25 },
  xAxis: {
    type: 'time',
    axisLabel: { color: 'var(--text-muted)', fontSize: 10 }
  },
  yAxis: {
    type: 'value',
    min: 0, max: 100,
    axisLabel: { color: 'var(--text-muted)', fontSize: 10, formatter: '{value}%' }
  },
  series: [{
    type: 'line',
    data: history.value.map(p => [p.timestampUnix * 1000, p.cpuPercent]),
    smooth: true,
    showSymbol: false,
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
  grid: { left: 40, right: 10, top: 10, bottom: 25 },
  xAxis: {
    type: 'time',
    axisLabel: { color: 'var(--text-muted)', fontSize: 10 }
  },
  yAxis: {
    type: 'value',
    min: 0, max: 100,
    axisLabel: { color: 'var(--text-muted)', fontSize: 10, formatter: '{value}%' }
  },
  series: [{
    type: 'line',
    data: history.value.map(p => [p.timestampUnix * 1000, p.memoryPercent]),
    smooth: true,
    showSymbol: false,
    lineStyle: { color: 'var(--warning)', width: 2 },
    areaStyle: {
      color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [
        { offset: 0, color: 'var(--glow-warning)' },
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
  <div class="drawer-overlay" @click.self="emit('close')">
    <div class="drawer glass-card">
      <div class="drawer-header">
        <h2 v-if="details">{{ details.name }}</h2>
        <button class="close-btn" @click="emit('close')">✕</button>
      </div>

      <div v-if="loading" class="loading-state">Loading...</div>
      <div v-else-if="error" class="error-state">{{ error }}</div>
      <div v-else-if="details" class="drawer-content">
        <section class="detail-section">
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

        <section class="detail-section">
          <h3>CPU</h3>
          <div class="info-grid">
            <div class="info-item"><span class="label">Cores</span><span class="value">{{ details.cpu.cores }}</span></div>
            <div class="info-item"><span class="label">Usage</span><span class="value">{{ Math.round(details.cpu.usagePercent) }}%</span></div>
          </div>
          <VChart :option="cpuChartOptions" style="height: 180px; margin-top: 8px;" autoresize />
        </section>

        <section class="detail-section">
          <h3>Memory</h3>
          <div class="info-grid">
            <div class="info-item"><span class="label">Total</span><span class="value">{{ humanBytes(details.memory.totalBytes) }}</span></div>
            <div class="info-item"><span class="label">Used</span><span class="value">{{ humanBytes(details.memory.usedBytes) }}</span></div>
            <div class="info-item"><span class="label">Usage</span><span class="value">{{ Math.round(details.memory.usagePercent) }}%</span></div>
          </div>
          <VChart :option="memChartOptions" style="height: 180px; margin-top: 8px;" autoresize />
        </section>

        <section class="detail-section">
          <h3>Disks</h3>
          <div v-for="disk in details.disks" :key="disk.mountPoint" class="disk-item">
            <div class="disk-label">
              <span>{{ disk.mountPoint }}</span>
              <span>{{ humanBytes(disk.usedBytes) }} / {{ humanBytes(disk.totalBytes) }} ({{ Math.round(disk.usagePercent) }}%)</span>
            </div>
            <div class="bar-track">
              <div class="bar-fill" :style="{ width: disk.usagePercent + '%' }"></div>
            </div>
          </div>
        </section>

        <section class="detail-section">
          <h3>Network</h3>
          <div class="info-grid">
            <div v-for="ni in details.networkInterfaces" :key="ni.name" class="info-item">
              <span class="label">{{ ni.name }}</span>
              <span class="value">RX: {{ humanBytes(ni.rxBytes) }} / TX: {{ humanBytes(ni.txBytes) }}</span>
            </div>
          </div>
        </section>

        <section class="detail-section">
          <h3>Temperatures</h3>
          <div class="info-grid">
            <div v-for="t in details.temperatures" :key="t.sensor" class="info-item">
              <span class="label">{{ t.sensor }}</span>
              <span class="value">{{ Math.round(t.celsius) }}°C</span>
            </div>
          </div>
        </section>

        <section class="detail-section">
          <h3>Collectors</h3>
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
.drawer-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 200;
  display: flex;
  justify-content: flex-end;
}

.drawer {
  width: 480px;
  max-width: 90vw;
  height: 100vh;
  overflow-y: auto;
  border-radius: 0;
  border-left: 1px solid var(--border-color);
  animation: slideIn 0.3s ease-out;
}

.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
  position: sticky;
  top: 0;
  background: var(--bg-sidebar);
  backdrop-filter: blur(12px);
  z-index: 1;
}

.drawer-header h2 {
  font-size: 1.1rem;
  font-weight: 600;
}

.close-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 1.2rem;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
}

.close-btn:hover {
  color: var(--text-primary);
  background: var(--bg-card);
}

.drawer-content {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.detail-section h3 {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
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
  font-size: 0.85rem;
  color: var(--text-primary);
  font-family: var(--font-mono);
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
  padding: 4px 0;
  border-bottom: 1px solid var(--border-color);
}

.loading-state, .error-state {
  padding: 2rem;
  text-align: center;
  color: var(--text-muted);
}
</style>
