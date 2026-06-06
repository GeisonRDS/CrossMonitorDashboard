<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import type { NodeSummary } from '../types/dashboard'
import MiniChart from './MiniChart.vue'

const props = defineProps<{ node: NodeSummary }>()
const router = useRouter()

const statusClass = computed(() => {
  if (!props.node.online) return 'status-offline'
  switch (props.node.status?.toLowerCase()) {
    case 'critical': return 'status-critical'
    case 'warning': return 'status-warning'
    default: return 'status-ok'
  }
})

const statusLabel = computed(() => {
  if (!props.node.online) return 'OFFLINE'
  switch (props.node.status?.toLowerCase()) {
    case 'critical': return 'CRITICAL'
    case 'warning': return 'WARNING'
    default: return 'ONLINE'
  }
})

const borderStyle = computed(() => {
  if (!props.node.online) return {}
  const glow = statusClass.value.replace('status-', 'glow-')
  return {
    borderColor: `var(--${props.node.status === 'critical' ? 'critical' : props.node.status === 'warning' ? 'warning' : 'success'})`,
    boxShadow: `0 0 12px var(--${glow})`
  }
})

const sparklineData = computed(() => {
  return Array.from({ length: 20 }, (_, i) => {
    const base = i < 15 ? props.node.cpuUsagePercent : props.node.cpuUsagePercent * (0.9 + Math.random() * 0.2)
    return Math.round(Math.max(0, Math.min(100, base)))
  })
})

const memorySparkline = computed(() => {
  return Array.from({ length: 20 }, (_, i) => {
    const base = i < 15 ? props.node.memoryUsagePercent : props.node.memoryUsagePercent * (0.9 + Math.random() * 0.2)
    return Math.round(Math.max(0, Math.min(100, base)))
  })
})

const lastUpdate = computed(() => {
  const diff = Date.now() / 1000 - props.node.lastUpdateUnix
  if (diff < 60) return `${Math.round(diff)}s ago`
  if (diff < 3600) return `${Math.round(diff / 60)}m ago`
  if (diff < 86400) return `${Math.round(diff / 3600)}h ago`
  return `${Math.round(diff / 86400)}d ago`
})

const tempDisplay = computed(() => {
  return props.node.primaryTemperatureCelsius !== null && props.node.primaryTemperatureCelsius !== undefined
    ? `${Math.round(props.node.primaryTemperatureCelsius)}°C`
    : '--'
})

const tempBarWidth = computed(() => {
  const temperature = props.node.primaryTemperatureCelsius ?? 0
  return `${Math.min(Math.max(temperature, 0), 100)}%`
})

function goToDetails() {
  router.push(`/nodes/${props.node.id}`)
}

</script>

<template>
  <div class="node-card glass-card fade-in" :style="borderStyle" @click="goToDetails">
    <div class="card-header">
      <div class="node-info">
        <span class="node-name">{{ node.name }}</span>
        <span class="node-type">{{ node.type }}</span>
      </div>
      <span class="status-badge" :class="statusClass">{{ statusLabel }}</span>
    </div>

    <div class="card-platform">
      <span class="text-muted text-mono">{{ node.os }} / {{ node.platform }}</span>
    </div>

    <div class="card-stats">
      <div class="stat-bar">
        <div class="stat-label">
          <span class="stat-name">CPU</span>
          <span class="stat-value">{{ Math.round(node.cpuUsagePercent) }}%</span>
        </div>
        <div class="bar-track">
          <div class="bar-fill" :style="{ width: node.cpuUsagePercent + '%', background: 'var(--accent)' }"></div>
        </div>
        <MiniChart :data="sparklineData" :color="'var(--accent)'" :height="28" />
      </div>
      <div class="stat-bar">
        <div class="stat-label">
          <span class="stat-name">MEM</span>
          <span class="stat-value">{{ Math.round(node.memoryUsagePercent) }}%</span>
        </div>
        <div class="bar-track">
          <div class="bar-fill" :style="{ width: node.memoryUsagePercent + '%', background: 'var(--warning)' }"></div>
        </div>
        <MiniChart :data="memorySparkline" :color="'var(--warning)'" :height="28" />
      </div>
      <div class="stat-bar">
        <div class="stat-label">
          <span class="stat-name">DSK</span>
          <span class="stat-value">{{ Math.round(node.primaryDiskUsagePercent) }}%</span>
        </div>
        <div class="bar-track">
          <div class="bar-fill" :style="{ width: node.primaryDiskUsagePercent + '%', background: 'var(--success)' }"></div>
        </div>
      </div>
      <div class="stat-bar">
        <div class="stat-label">
          <span class="stat-name">TEMP</span>
          <span class="stat-value">{{ tempDisplay }}</span>
        </div>
        <div class="temp-indicator">
          <div class="temp-bar" :style="{ width: tempBarWidth }"></div>
        </div>
      </div>
    </div>

    <div class="card-footer">
      <span class="text-muted text-mono" style="font-size: 11px;">v{{ node.agentVersion }}</span>
      <span class="text-muted text-mono" style="font-size: 11px;">{{ lastUpdate }}</span>
    </div>
  </div>
</template>

<style scoped>
.node-card {
  padding: 1rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  min-width: 0;
}

.card-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.5rem;
}

.node-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.node-name {
  font-weight: 600;
  font-size: 1rem;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.node-type {
  font-size: 0.75rem;
  color: var(--text-muted);
  font-family: var(--font-mono);
}

.status-badge {
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.65rem;
  font-family: var(--font-mono);
  font-weight: 700;
  white-space: nowrap;
  flex-shrink: 0;
  background: rgba(0, 0, 0, 0.3);
}

.status-badge.status-ok {
  color: var(--success);
  border: 1px solid var(--success);
  box-shadow: 0 0 6px var(--glow-success);
}

.status-badge.status-warning {
  color: var(--warning);
  border: 1px solid var(--warning);
  box-shadow: 0 0 6px var(--glow-warning);
}

.status-badge.status-critical {
  color: var(--critical);
  border: 1px solid var(--critical);
  box-shadow: 0 0 6px var(--glow-critical);
}

.status-badge.status-offline {
  color: var(--offline);
  border: 1px solid var(--offline);
}

.card-platform {
  font-size: 0.7rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card-stats {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.stat-bar {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.stat-label {
  display: flex;
  justify-content: space-between;
  font-size: 0.7rem;
}

.stat-name {
  font-family: var(--font-mono);
  font-weight: 600;
  color: var(--text-secondary);
}

.stat-value {
  font-family: var(--font-mono);
  color: var(--text-primary);
}

.bar-track {
  width: 100%;
  height: 4px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 2px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.5s ease;
}

.temp-indicator {
  width: 100%;
  height: 4px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 2px;
  overflow: hidden;
}

.temp-bar {
  height: 100%;
  background: var(--critical);
  border-radius: 2px;
  transition: width 0.5s ease;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 0.25rem;
  border-top: 1px solid var(--border-color);
}
</style>
