<script setup lang="ts">
import { useDashboardStore } from '../stores/dashboardStore'
import { useI18n } from '../composables/useI18n'
import NodeCard from '../components/NodeCard.vue'

const store = useDashboardStore()
const { translate } = useI18n()
</script>

<template>
  <div class="dashboard">
    <div v-if="store.loading.value && store.nodes.value.length === 0" class="grid-responsive node-grid">
      <div v-for="i in 6" :key="i" class="skeleton-card glass-card">
        <div class="skeleton-line" style="width: 62%; height: 17px;"></div>
        <div class="skeleton-line" style="width: 38%; height: 11px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 58px;"></div>
        <div class="skeleton-line" style="width: 100%; height: 58px;"></div>
      </div>
    </div>

    <div v-else-if="store.error.value && store.nodes.value.length === 0" class="error-state glass-card">
      <p>{{ translate('dashboard.failedToLoad', { error: store.error.value }) }}</p>
      <button class="retry-btn" @click="store.fetchNodes()">{{ translate('dashboard.retry') }}</button>
    </div>

    <div v-else class="grid-responsive node-grid">
      <NodeCard
        v-for="node in store.nodes.value"
        :key="node.id"
        :node="node"
        :history="store.history.value.get(node.id) || []"
        :details="store.details.value.get(node.id) || null"
      />
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  min-height: calc(100vh - 3.5rem);
}

.node-grid {
  grid-template-columns: repeat(auto-fill, minmax(var(--card-min-width, 315px), 1fr));
  gap: 1rem;
  align-items: stretch;
}

.skeleton-card {
  min-height: 310px;
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

@media (max-width: 480px) {
  .node-grid {
    grid-template-columns: 1fr;
  }
}
</style>
