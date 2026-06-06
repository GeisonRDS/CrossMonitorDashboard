<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '../stores/dashboardStore'
import NodeCard from '../components/NodeCard.vue'

const store = useDashboardStore()

const summaryStats = computed(() => {
  const s = store.summary.value
  if (!s) return null
  return [
    { label: 'Total', value: s.totalNodes, tone: 'var(--text-primary)' },
    { label: 'Online', value: s.onlineNodes, tone: 'var(--success)' },
    { label: 'Offline', value: s.offlineNodes, tone: 'var(--offline)' },
    { label: 'Warning', value: s.warningNodes, tone: 'var(--warning)' },
    { label: 'Critical', value: s.criticalNodes, tone: 'var(--critical)' },
    { label: 'Avg CPU', value: `${Math.round(s.averageCpuPercent)}%`, tone: 'var(--accent)' },
    { label: 'Avg MEM', value: `${Math.round(s.averageMemoryPercent)}%`, tone: 'var(--warning)' },
    { label: 'Max Disk', value: `${Math.round(s.highestDiskUsagePercent)}%`, tone: 'var(--success)' },
    { label: 'Max Temp', value: `${Math.round(s.highestTemperatureCelsius)}°`, tone: 'var(--critical)' }
  ]
})
</script>

<template>
  <div class="dashboard">
    <section class="dashboard-hero glass-card">
      <div>
        <p class="eyebrow text-mono">CrossMonitor NOC</p>
        <h1>Live node telemetry</h1>
        <p class="hero-copy">CPU, memory, disk, network and temperature streamed through the dashboard backend.</p>
      </div>
      <div v-if="summaryStats" class="summary-strip">
        <div v-for="stat in summaryStats" :key="stat.label" class="summary-item">
          <span class="summary-value" :style="{ color: stat.tone }">{{ stat.value }}</span>
          <span class="summary-label">{{ stat.label }}</span>
        </div>
      </div>
    </section>

    <div v-if="store.loading.value && store.nodes.value.length === 0" class="grid-responsive node-grid">
      <div v-for="i in 6" :key="i" class="skeleton-card glass-card">
        <div class="skeleton-line" style="width: 60%; height: 18px;"></div>
        <div class="skeleton-line" style="width: 42%; height: 12px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 64px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 8px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 8px;"></div>
      </div>
    </div>

    <div v-else-if="store.error.value && store.nodes.value.length === 0" class="error-state glass-card">
      <p>Failed to load nodes: {{ store.error.value }}</p>
      <button class="retry-btn" @click="store.fetchNodes()">Retry</button>
    </div>

    <div v-else class="grid-responsive node-grid">
      <NodeCard v-for="node in store.nodes.value" :key="node.id" :node="node" />
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.dashboard-hero {
  position: relative;
  overflow: hidden;
  padding: 1.35rem;
  display: grid;
  grid-template-columns: minmax(240px, 0.7fr) 1fr;
  gap: 1.25rem;
  align-items: center;
}

.dashboard-hero::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(circle at 12% 20%, var(--glow-accent), transparent 38%),
    linear-gradient(100deg, rgba(255,255,255,0.06), transparent 55%);
}

.dashboard-hero > * {
  position: relative;
  z-index: 1;
}

.eyebrow {
  margin-bottom: 0.4rem;
  color: var(--accent-light);
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.18em;
}

.dashboard-hero h1 {
  font-size: clamp(1.6rem, 3vw, 2.55rem);
  line-height: 1;
  letter-spacing: -0.04em;
}

.hero-copy {
  max-width: 520px;
  margin-top: 0.65rem;
  color: var(--text-secondary);
  line-height: 1.55;
  font-size: 0.92rem;
}

.summary-strip {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(86px, 1fr));
  gap: 0.65rem;
}

.summary-item {
  min-height: 72px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 0.65rem;
  border-radius: 14px;
  background: rgba(0,0,0,0.18);
  border: 1px solid rgba(255,255,255,0.06);
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.05);
}

.summary-value {
  font-family: var(--font-mono);
  font-size: 1.22rem;
  font-weight: 800;
}

.summary-label {
  margin-top: 0.28rem;
  font-size: 0.64rem;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.node-grid {
  grid-template-columns: repeat(auto-fill, minmax(var(--card-min-width, 320px), 1fr));
  gap: 1rem;
}

.skeleton-card {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.skeleton-line {
  background: rgba(255, 255, 255, 0.06);
  border-radius: 8px;
  animation: pulse 2s ease-in-out infinite;
}

.error-state {
  padding: 2rem;
  text-align: center;
  color: var(--critical);
}

.retry-btn {
  margin-top: 1rem;
  padding: 0.55rem 1.5rem;
  background: var(--accent);
  color: #fff;
  border: none;
  border-radius: 999px;
  font-size: 0.9rem;
  cursor: pointer;
}

.retry-btn:hover {
  background: var(--accent-light);
  box-shadow: 0 0 18px var(--glow-accent);
}

@media (max-width: 980px) {
  .dashboard-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .node-grid {
    grid-template-columns: 1fr;
  }
}
</style>
