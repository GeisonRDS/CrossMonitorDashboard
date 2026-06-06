<script setup lang="ts">
import { onMounted, onUnmounted, computed } from 'vue'
import { useDashboardStore } from '../stores/dashboardStore'
import NodeCard from '../components/NodeCard.vue'

const store = useDashboardStore()

const summaryStats = computed(() => {
  const s = store.summary.value
  if (!s) return null
  return [
    { label: 'Total', value: s.totalNodes, color: 'var(--text-primary)' },
    { label: 'Online', value: s.onlineNodes, color: 'var(--success)' },
    { label: 'Offline', value: s.offlineNodes, color: 'var(--offline)' },
    { label: 'Warning', value: s.warningNodes, color: 'var(--warning)' },
    { label: 'Critical', value: s.criticalNodes, color: 'var(--critical)' },
    { label: 'Avg CPU', value: `${Math.round(s.averageCpuPercent)}%`, color: 'var(--accent)' },
    { label: 'Avg MEM', value: `${Math.round(s.averageMemoryPercent)}%`, color: 'var(--warning)' }
  ]
})

onMounted(() => {
  store.startPolling(5000)
})

onUnmounted(() => {
  store.stopPolling()
})
</script>

<template>
  <div class="dashboard">
    <div v-if="summaryStats" class="summary-bar">
      <div v-for="stat in summaryStats" :key="stat.label" class="summary-item">
        <span class="summary-value" :style="{ color: stat.color }">{{ stat.value }}</span>
        <span class="summary-label">{{ stat.label }}</span>
      </div>
    </div>

    <div v-if="store.loading.value && store.nodes.value.length === 0" class="grid-responsive">
      <div v-for="i in 6" :key="i" class="skeleton-card glass-card">
        <div class="skeleton-line" style="width: 60%; height: 16px;"></div>
        <div class="skeleton-line" style="width: 40%; height: 12px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 6px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 6px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 6px;"></div>
      </div>
    </div>

    <div v-else-if="store.error.value && store.nodes.value.length === 0" class="error-state glass-card">
      <p>Failed to load nodes: {{ store.error.value }}</p>
      <button class="retry-btn" @click="store.fetchNodes()">Retry</button>
    </div>

    <div v-else class="grid-responsive">
      <NodeCard v-for="node in store.nodes.value" :key="node.id" :node="node" />
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.summary-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--card-radius);
  backdrop-filter: blur(var(--card-blur));
}

.summary-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0 0.75rem;
  border-right: 1px solid var(--border-color);
}

.summary-item:last-child {
  border-right: none;
}

.summary-value {
  font-family: var(--font-mono);
  font-size: 1.2rem;
  font-weight: 700;
}

.summary-label {
  font-size: 0.65rem;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.skeleton-card {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.skeleton-line {
  background: rgba(255, 255, 255, 0.05);
  border-radius: 4px;
  animation: pulse 2s ease-in-out infinite;
}

.error-state {
  padding: 2rem;
  text-align: center;
  color: var(--critical);
}

.retry-btn {
  margin-top: 1rem;
  padding: 0.5rem 1.5rem;
  background: var(--accent);
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 0.9rem;
  cursor: pointer;
}

.retry-btn:hover {
  background: var(--accent-light);
}

.grid-responsive {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}
</style>
