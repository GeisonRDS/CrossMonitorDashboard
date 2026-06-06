<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { NodeSummary } from '../types/dashboard'
import { useTheme, type MetricKey } from '../composables/useTheme'
import MiniChart from './MiniChart.vue'

const props = defineProps<{ node: NodeSummary }>()
const router = useRouter()
const { currentTheme, visualSettings } = useTheme()

const histories = {
  cpu: ref<number[]>([]),
  memory: ref<number[]>([]),
  disk: ref<number[]>([]),
  temperature: ref<number[]>([])
}

function clamp(value: number, max = 100) {
  return Math.max(0, Math.min(max, Number.isFinite(value) ? value : 0))
}

function pushPoint(target: typeof histories.cpu, value: number, max = 100) {
  const next = [...target.value, Math.round(clamp(value, max) * 10) / 10]
  target.value = next.slice(Math.max(0, next.length - 28))
}

watch(
  () => [
    props.node.cpuUsagePercent,
    props.node.memoryUsagePercent,
    props.node.primaryDiskUsagePercent,
    props.node.primaryTemperatureCelsius,
    props.node.lastUpdateUnix
  ] as const,
  ([cpu, memory, disk, temperature]) => {
    pushPoint(histories.cpu, cpu)
    pushPoint(histories.memory, memory)
    pushPoint(histories.disk, disk)
    pushPoint(histories.temperature, temperature ?? 0, 120)
  },
  { immediate: true }
)

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
    case 'critical': return currentTheme.value === 'pixel-platformer' ? 'BOSS' : 'CRITICAL'
    case 'warning': return currentTheme.value === 'pixel-platformer' ? 'LOW HP' : 'WARNING'
    default: return currentTheme.value === 'pixel-platformer' ? 'READY' : 'ONLINE'
  }
})

const metricLabels = computed(() => currentTheme.value === 'pixel-platformer'
  ? { cpu: 'HP', memory: 'MP', disk: 'BAG', temperature: 'HEAT' }
  : { cpu: 'CPU', memory: 'RAM', disk: 'DISK', temperature: 'TEMP' })

const cardStyle = computed(() => {
  const severity = props.node.online ? props.node.status : 'offline'
  const token = severity === 'critical' ? 'critical' : severity === 'warning' ? 'warning' : severity === 'offline' ? 'offline' : 'success'
  const glow = severity === 'critical' ? 'glow-critical' : severity === 'warning' ? 'glow-warning' : severity === 'offline' ? 'glow-accent' : 'glow-success'
  return {
    '--node-tone': `var(--${token})`,
    '--node-glow': `var(--${glow})`
  }
})

const metrics = computed(() => [
  {
    key: 'cpu' as MetricKey,
    label: metricLabels.value.cpu,
    value: `${Math.round(props.node.cpuUsagePercent)}%`,
    data: histories.cpu.value,
    color: 'var(--accent)',
    unit: '%',
    max: 100
  },
  {
    key: 'memory' as MetricKey,
    label: metricLabels.value.memory,
    value: `${Math.round(props.node.memoryUsagePercent)}%`,
    data: histories.memory.value,
    color: 'var(--warning)',
    unit: '%',
    max: 100
  },
  {
    key: 'disk' as MetricKey,
    label: metricLabels.value.disk,
    value: `${Math.round(props.node.primaryDiskUsagePercent)}%`,
    data: histories.disk.value,
    color: 'var(--success)',
    unit: '%',
    max: 100
  },
  {
    key: 'temperature' as MetricKey,
    label: metricLabels.value.temperature,
    value: props.node.primaryTemperatureCelsius == null ? '--' : `${Math.round(props.node.primaryTemperatureCelsius)}°C`,
    data: histories.temperature.value,
    color: 'var(--critical)',
    unit: '°',
    max: 120
  }
])

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
        <p class="node-platform text-mono">{{ node.os || 'unknown' }} / {{ node.platform || 'unknown' }}</p>
      </div>
      <span class="status-badge" :class="statusClass">
        <span class="status-light"></span>
        {{ statusLabel }}
      </span>
    </div>

    <div class="metrics-grid">
      <section v-for="metric in metrics" :key="metric.key" class="metric-tile">
        <div class="metric-copy">
          <span class="metric-label text-mono">{{ metric.label }}</span>
          <strong>{{ metric.value }}</strong>
        </div>
        <MiniChart
          :data="metric.data"
          :color="metric.color"
          :height="visualSettings.metricCharts[metric.key] === 'radial-gauge' ? 66 : 54"
          :chart-type="visualSettings.metricCharts[metric.key]"
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
  min-height: 318px;
  padding: 1rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
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
  grid-template-columns: minmax(0, 1fr) auto;
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
  font-size: 1.06rem;
  line-height: 1.1;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.node-platform {
  margin-top: 0.35rem;
  color: var(--text-secondary);
  font-size: 0.68rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
  gap: 0.7rem;
}

.metric-tile {
  min-width: 0;
  padding: 0.68rem;
  border-radius: 14px;
  background: var(--metric-tile-bg);
  border: 1px solid rgba(255,255,255,0.055);
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.05);
}

.metric-copy {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.38rem;
}

.metric-label {
  color: var(--text-muted);
  font-size: 0.64rem;
  letter-spacing: 0.1em;
}

.metric-copy strong {
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.92rem;
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
  .metrics-grid {
    grid-template-columns: 1fr;
  }
}
</style>
