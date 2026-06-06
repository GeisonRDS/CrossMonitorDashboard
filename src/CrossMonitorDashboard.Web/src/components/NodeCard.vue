<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { NodeSummary } from '../types/dashboard'
import { useTheme } from '../composables/useTheme'
import MiniChart from './MiniChart.vue'

const props = defineProps<{ node: NodeSummary }>()
const router = useRouter()
const { currentTheme } = useTheme()

const cpuHistory = ref<number[]>([])
const memoryHistory = ref<number[]>([])
const diskHistory = ref<number[]>([])
const temperatureHistory = ref<number[]>([])

function clampPercent(value: number) {
  return Math.max(0, Math.min(100, Math.round(value || 0)))
}

function pushPoint(target: typeof cpuHistory, value: number) {
  const next = [...target.value, clampPercent(value)]
  target.value = next.slice(Math.max(0, next.length - 24))
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
    pushPoint(cpuHistory, cpu)
    pushPoint(memoryHistory, memory)
    pushPoint(diskHistory, disk)
    pushPoint(temperatureHistory, temperature ?? 0)
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
    case 'critical': return currentTheme.value === 'pixel-platformer' ? 'BOSS FIGHT' : 'CRITICAL'
    case 'warning': return currentTheme.value === 'pixel-platformer' ? 'LOW HP' : 'WARNING'
    default: return currentTheme.value === 'pixel-platformer' ? 'READY' : 'ONLINE'
  }
})

const metricLabels = computed(() => currentTheme.value === 'pixel-platformer'
  ? { cpu: 'HP', memory: 'MP', disk: 'BAG', temp: 'HEAT' }
  : { cpu: 'CPU', memory: 'MEM', disk: 'DSK', temp: 'TEMP' })

const cardStyle = computed(() => {
  const severity = props.node.online ? props.node.status : 'offline'
  const token = severity === 'critical' ? 'critical' : severity === 'warning' ? 'warning' : severity === 'offline' ? 'offline' : 'success'
  const glow = severity === 'critical' ? 'glow-critical' : severity === 'warning' ? 'glow-warning' : severity === 'offline' ? 'glow-accent' : 'glow-success'
  return {
    '--node-tone': `var(--${token})`,
    '--node-glow': `var(--${glow})`
  }
})

const lastUpdate = computed(() => {
  if (!props.node.lastUpdateUnix) return '--'
  const diff = Math.max(0, Date.now() / 1000 - props.node.lastUpdateUnix)
  if (diff < 60) return `${Math.round(diff)}s ago`
  if (diff < 3600) return `${Math.round(diff / 60)}m ago`
  if (diff < 86400) return `${Math.round(diff / 3600)}h ago`
  return `${Math.round(diff / 86400)}d ago`
})

const tempDisplay = computed(() => props.node.primaryTemperatureCelsius !== null && props.node.primaryTemperatureCelsius !== undefined
  ? `${Math.round(props.node.primaryTemperatureCelsius)}°C`
  : '--')

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
    <div class="card-orb"></div>
    <div class="card-header">
      <div class="node-info">
        <span class="node-type">{{ node.type }}</span>
        <h2 class="node-name">{{ node.name }}</h2>
      </div>
      <span class="status-badge" :class="statusClass">
        <span class="status-light"></span>
        {{ statusLabel }}
      </span>
    </div>

    <div class="card-platform text-mono">
      <span>{{ node.os || 'unknown' }}</span>
      <span class="platform-separator">/</span>
      <span>{{ node.platform || 'unknown platform' }}</span>
    </div>

    <div class="metric-hero">
      <div>
        <span class="hero-label">{{ metricLabels.cpu }}</span>
        <strong>{{ Math.round(node.cpuUsagePercent) }}%</strong>
      </div>
      <MiniChart :data="cpuHistory" color="var(--accent)" :height="58" />
    </div>

    <div class="card-stats">
      <div class="stat-row stat-memory">
        <div class="stat-label">
          <span>{{ metricLabels.memory }}</span>
          <strong>{{ Math.round(node.memoryUsagePercent) }}%</strong>
        </div>
        <div class="bar-track"><div class="bar-fill" :style="{ width: `${clampPercent(node.memoryUsagePercent)}%` }"></div></div>
        <MiniChart :data="memoryHistory" color="var(--warning)" :height="26" />
      </div>

      <div class="stat-row stat-disk">
        <div class="stat-label">
          <span>{{ metricLabels.disk }}</span>
          <strong>{{ Math.round(node.primaryDiskUsagePercent) }}%</strong>
        </div>
        <div class="bar-track"><div class="bar-fill" :style="{ width: `${clampPercent(node.primaryDiskUsagePercent)}%` }"></div></div>
        <MiniChart :data="diskHistory" color="var(--success)" :height="26" type="bar" />
      </div>

      <div class="stat-row stat-temp">
        <div class="stat-label">
          <span>{{ metricLabels.temp }}</span>
          <strong>{{ tempDisplay }}</strong>
        </div>
        <div class="bar-track"><div class="bar-fill" :style="{ width: `${clampPercent(node.primaryTemperatureCelsius ?? 0)}%` }"></div></div>
        <MiniChart :data="temperatureHistory" color="var(--critical)" :height="26" />
      </div>
    </div>

    <div v-if="node.lastError" class="node-error text-mono">{{ node.lastError }}</div>

    <div class="card-footer">
      <span class="text-mono">Agent v{{ node.agentVersion || '--' }}</span>
      <span class="text-mono">{{ lastUpdate }}</span>
    </div>
  </article>
</template>

<style scoped>
.node-card {
  --node-tone: var(--success);
  --node-glow: var(--glow-success);
  position: relative;
  padding: 1.05rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  min-width: 0;
  overflow: hidden;
  border-color: color-mix(in srgb, var(--node-tone) 42%, var(--border-color));
  box-shadow: 0 20px 54px rgba(0,0,0,0.28), 0 0 20px color-mix(in srgb, var(--node-glow) 68%, transparent);
}

.node-card::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    linear-gradient(135deg, color-mix(in srgb, var(--node-tone) 16%, transparent), transparent 36%),
    radial-gradient(circle at 80% 8%, color-mix(in srgb, var(--node-tone) 24%, transparent), transparent 34%);
  opacity: 0.85;
}

.node-card:hover {
  transform: translateY(-4px) scale(1.01);
  box-shadow: 0 28px 72px rgba(0,0,0,0.36), 0 0 34px var(--node-glow);
}

.node-card:active {
  transform: translateY(-1px) scale(0.995);
}

.node-card:focus-visible {
  outline: 2px solid var(--accent-light);
  outline-offset: 3px;
}

.node-card.offline {
  filter: saturate(0.55);
}

.card-orb {
  position: absolute;
  right: -42px;
  top: -42px;
  width: 128px;
  height: 128px;
  border-radius: 50%;
  background: radial-gradient(circle, var(--node-tone), transparent 65%);
  filter: blur(9px);
  opacity: 0.32;
  pointer-events: none;
}

.card-header,
.card-platform,
.metric-hero,
.card-stats,
.node-error,
.card-footer {
  position: relative;
  z-index: 1;
}

.card-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.node-info {
  min-width: 0;
}

.node-name {
  font-weight: 760;
  font-size: 1.1rem;
  line-height: 1.15;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.node-type {
  display: block;
  margin-bottom: 0.22rem;
  font-size: 0.68rem;
  color: var(--text-muted);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.12em;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 5px 8px;
  border-radius: 999px;
  font-size: 0.64rem;
  font-family: var(--font-mono);
  font-weight: 800;
  white-space: nowrap;
  flex-shrink: 0;
  color: var(--node-tone);
  background: color-mix(in srgb, var(--node-tone) 13%, rgba(0,0,0,0.34));
  border: 1px solid color-mix(in srgb, var(--node-tone) 48%, transparent);
}

.status-light {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: currentColor;
  box-shadow: 0 0 12px currentColor;
}

.card-platform {
  display: flex;
  gap: 0.35rem;
  min-width: 0;
  overflow: hidden;
  color: var(--text-secondary);
  font-size: 0.72rem;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.platform-separator {
  color: var(--accent);
}

.metric-hero {
  display: grid;
  grid-template-columns: minmax(72px, 0.38fr) 1fr;
  gap: 0.6rem;
  align-items: center;
  padding: 0.7rem;
  border-radius: calc(var(--card-radius) - 4px);
  background: rgba(0, 0, 0, 0.18);
  border: 1px solid rgba(255,255,255,0.055);
}

.hero-label {
  display: block;
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.12em;
}

.metric-hero strong {
  display: block;
  margin-top: 0.15rem;
  font-family: var(--font-mono);
  font-size: 1.7rem;
  color: var(--text-primary);
}

.card-stats {
  display: grid;
  gap: 0.65rem;
}

.stat-row {
  display: grid;
  gap: 0.32rem;
}

.stat-label {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  font-size: 0.72rem;
  color: var(--text-secondary);
  font-family: var(--font-mono);
}

.stat-label strong {
  color: var(--text-primary);
  font-size: 0.78rem;
}

.bar-track {
  width: 100%;
  height: 6px;
  background: rgba(255, 255, 255, 0.06);
  border-radius: 999px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  border-radius: 999px;
  transition: width 0.55s ease;
  box-shadow: 0 0 12px currentColor;
}

.stat-memory .bar-fill { background: var(--warning); color: var(--warning); }
.stat-disk .bar-fill { background: var(--success); color: var(--success); }
.stat-temp .bar-fill { background: var(--critical); color: var(--critical); }

.node-error {
  max-height: 3.2em;
  overflow: hidden;
  color: var(--warning);
  font-size: 0.68rem;
  line-height: 1.55;
  padding: 0.5rem 0.6rem;
  border: 1px solid color-mix(in srgb, var(--warning) 32%, transparent);
  background: color-mix(in srgb, var(--warning) 8%, transparent);
  border-radius: 10px;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  padding-top: 0.25rem;
  border-top: 1px solid var(--border-color);
  color: var(--text-muted);
  font-size: 0.68rem;
}

[data-theme="pixel-platformer"] .node-card {
  border-width: 3px;
  box-shadow: 6px 6px 0 rgba(0,0,0,0.45), 0 0 0 2px rgba(255,255,255,0.08) inset;
}

[data-theme="pixel-platformer"] .node-card:hover {
  transform: translate(-2px, -2px);
  box-shadow: 8px 8px 0 rgba(0,0,0,0.55), 0 0 20px var(--node-glow);
}

@media (max-width: 420px) {
  .metric-hero {
    grid-template-columns: 1fr;
  }
}
</style>
