<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useDashboardStore } from '../stores/dashboardStore'
import { useI18n } from '../composables/useI18n'
import NodeCard from '../components/NodeCard.vue'
import { applyCardOrder, getDashboardLayout, resetDashboardLayout, saveDashboardLayout, validateCardOrder } from '../utils/dashboardLayoutStore'

const store = useDashboardStore()
const { translate } = useI18n()

const isEditingLayout = ref(false)
const savedOrder = ref<string[]>(getDashboardLayout())
const draftOrder = ref<string[]>([])
const dragNodeId = ref<string | null>(null)
const gridElement = ref<HTMLElement | null>(null)

const displayedNodes = computed(() => applyCardOrder(store.nodes.value, isEditingLayout.value ? draftOrder.value : savedOrder.value))

watch(
  () => store.nodes.value.map(node => node.id),
  () => {
    if (!isEditingLayout.value) return
    draftOrder.value = validateCardOrder(draftOrder.value, store.nodes.value)
    const knownIds = new Set(draftOrder.value)
    draftOrder.value = [...draftOrder.value, ...store.nodes.value.filter(node => !knownIds.has(node.id)).map(node => node.id)]
  },
  { immediate: true }
)

function currentNodeIds(nodes = displayedNodes.value) {
  return nodes.map(node => node.id)
}

function startEditLayout() {
  draftOrder.value = currentNodeIds()
  isEditingLayout.value = true
}

function saveLayout() {
  const nextOrder = currentNodeIds()
  saveDashboardLayout(nextOrder)
  savedOrder.value = nextOrder
  draftOrder.value = nextOrder
  isEditingLayout.value = false
}

function cancelLayout() {
  draftOrder.value = savedOrder.value
  isEditingLayout.value = false
}

function restoreDefaultLayout() {
  resetDashboardLayout()
  savedOrder.value = []
  draftOrder.value = currentNodeIds(store.nodes.value)
  isEditingLayout.value = false
}

function moveNode(nodeId: string, direction: -1 | 1) {
  const order = currentNodeIds()
  const from = order.indexOf(nodeId)
  const to = from + direction
  if (from < 0 || to < 0 || to >= order.length) return
  reorderWithAnimation(order, from, to)
}

function onDragStart(event: DragEvent, nodeId: string) {
  if (!isEditingLayout.value) return
  dragNodeId.value = nodeId
  event.dataTransfer?.setData('text/plain', nodeId)
  if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move'
}

function onDragOver(event: DragEvent, targetNodeId: string) {
  if (!dragNodeId.value || dragNodeId.value === targetNodeId) return
  event.preventDefault()
  const order = currentNodeIds()
  const from = order.indexOf(dragNodeId.value)
  const to = order.indexOf(targetNodeId)
  if (from < 0 || to < 0 || from === to) return
  reorderWithAnimation(order, from, to)
}

function onDragEnd() {
  dragNodeId.value = null
}

async function reorderWithAnimation(order: string[], from: number, to: number) {
  const first = measureCards()
  const [moved] = order.splice(from, 1)
  order.splice(to, 0, moved)
  draftOrder.value = order
  await nextTick()
  animateCards(first)
}

function measureCards() {
  const positions = new Map<string, DOMRect>()
  gridElement.value?.querySelectorAll<HTMLElement>('[data-node-id]').forEach(element => {
    positions.set(element.dataset.nodeId ?? '', element.getBoundingClientRect())
  })
  return positions
}

function animateCards(first: Map<string, DOMRect>) {
  gridElement.value?.querySelectorAll<HTMLElement>('[data-node-id]').forEach(element => {
    const nodeId = element.dataset.nodeId ?? ''
    const before = first.get(nodeId)
    if (!before) return
    const after = element.getBoundingClientRect()
    const dx = before.left - after.left
    const dy = before.top - after.top
    if (!dx && !dy) return
    element.animate([
      { transform: `translate(${dx}px, ${dy}px)` },
      { transform: 'translate(0, 0)' }
    ], { duration: 180, easing: 'ease-out' })
  })
}
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

    <template v-else>
      <div class="layout-toolbar glass-card" :class="{ editing: isEditingLayout }">
        <span class="layout-status text-mono">{{ isEditingLayout ? translate('dashboard.layoutEditing') : translate('dashboard.layoutHint') }}</span>
        <div class="layout-actions">
          <button v-if="!isEditingLayout" class="layout-btn" type="button" @click="startEditLayout">{{ translate('dashboard.editLayout') }}</button>
          <template v-else>
            <button class="layout-btn primary" type="button" @click="saveLayout">{{ translate('dashboard.saveLayout') }}</button>
            <button class="layout-btn" type="button" @click="cancelLayout">{{ translate('dashboard.cancelLayout') }}</button>
            <button class="layout-btn danger" type="button" @click="restoreDefaultLayout">{{ translate('dashboard.restoreDefaultLayout') }}</button>
          </template>
        </div>
      </div>

      <div ref="gridElement" class="grid-responsive node-grid" :class="{ 'is-editing': isEditingLayout }">
        <div
          v-for="(node, index) in displayedNodes"
          :key="node.id"
          class="node-card-shell"
          :class="{ dragging: dragNodeId === node.id }"
          :data-node-id="node.id"
          :draggable="isEditingLayout"
          @dragstart="onDragStart($event, node.id)"
          @dragover="onDragOver($event, node.id)"
          @dragend="onDragEnd"
          @drop.prevent="onDragEnd"
        >
          <div v-if="isEditingLayout" class="card-move-controls" role="group" :aria-label="translate('dashboard.moveCard', { name: node.name })">
            <button class="move-btn" type="button" :disabled="index === 0" @click="moveNode(node.id, -1)">{{ translate('dashboard.moveBefore') }}</button>
            <button class="move-btn" type="button" :disabled="index === displayedNodes.length - 1" @click="moveNode(node.id, 1)">{{ translate('dashboard.moveAfter') }}</button>
          </div>
          <NodeCard
            :node="node"
            :history="store.history.value.get(node.id) || []"
            :details="store.details.value.get(node.id) || null"
            :editing="isEditingLayout"
          />
        </div>
      </div>
    </template>
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

.layout-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
}

.layout-toolbar.editing {
  border-color: var(--accent);
  box-shadow: var(--card-shadow), 0 0 16px var(--glow-accent);
}

.layout-status {
  color: var(--text-secondary);
  font-size: 0.78rem;
}

.layout-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.5rem;
}

.layout-btn,
.move-btn {
  border: 1px solid var(--border-color);
  background: var(--bg-card-hover);
  color: var(--text-primary);
  border-radius: 999px;
  cursor: pointer;
  transition: border-color var(--transition-speed), box-shadow var(--transition-speed), transform var(--transition-speed);
}

.layout-btn {
  padding: 0.48rem 0.9rem;
}

.layout-btn:hover,
.move-btn:hover:not(:disabled) {
  border-color: var(--accent);
  box-shadow: 0 0 12px var(--glow-accent);
  transform: translateY(-1px);
}

.layout-btn.primary {
  border-color: var(--accent);
  background: var(--accent);
  color: var(--bg-primary);
}

.layout-btn.danger {
  border-color: var(--critical);
}

.node-card-shell {
  position: relative;
  min-width: 0;
}

.node-grid.is-editing .node-card-shell {
  cursor: grab;
}

.node-card-shell.dragging {
  opacity: 0.62;
}

.card-move-controls {
  position: absolute;
  top: 0.55rem;
  right: 0.55rem;
  z-index: 4;
  display: flex;
  gap: 0.35rem;
}

.move-btn {
  padding: 0.3rem 0.55rem;
  font-size: 0.72rem;
}

.move-btn:disabled {
  cursor: not-allowed;
  opacity: 0.45;
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
  .layout-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .layout-actions {
    justify-content: flex-start;
  }

  .node-grid {
    grid-template-columns: 1fr;
  }
}
</style>
