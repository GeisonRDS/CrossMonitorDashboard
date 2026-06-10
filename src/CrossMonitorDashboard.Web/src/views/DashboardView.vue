<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useDashboardStore } from '../stores/dashboardStore'
import { useI18n } from '../composables/useI18n'
import NodeCard from '../components/NodeCard.vue'
import { applyCardOrder, getDashboardLayout, saveDashboardLayout, validateCardOrder } from '../utils/dashboardLayoutStore'

const dragThresholdPx = 7
const store = useDashboardStore()
const { translate } = useI18n()

interface PendingDrag {
  nodeId: string
  pointerId: number
  startX: number
  startY: number
}

const savedOrder = ref<string[]>(getDashboardLayout())
const draftOrder = ref<string[]>([])
const pendingDrag = ref<PendingDrag | null>(null)
const dragNodeId = ref<string | null>(null)
const dragOffset = ref({ x: 0, y: 0 })
const suppressClickNodeId = ref<string | null>(null)
const gridElement = ref<HTMLElement | null>(null)
let clickSuppressTimer: ReturnType<typeof setTimeout> | null = null

const displayedNodes = computed(() => applyCardOrder(store.nodes.value, dragNodeId.value ? draftOrder.value : savedOrder.value))

watch(
  () => store.nodes.value.map(node => node.id),
  () => {
    if (!dragNodeId.value) return
    draftOrder.value = completeOrder(validateCardOrder(draftOrder.value, store.nodes.value))
  }
)

onBeforeUnmount(() => {
  removePointerListeners()
  if (clickSuppressTimer) clearTimeout(clickSuppressTimer)
})

function completeOrder(order: string[]) {
  const knownIds = new Set(order)
  return [...order, ...store.nodes.value.filter(node => !knownIds.has(node.id)).map(node => node.id)]
}

function currentNodeIds() {
  return displayedNodes.value.map(node => node.id)
}

function onPointerDown(event: PointerEvent, nodeId: string) {
  if (event.button !== 0 || (event.pointerType === 'mouse' && event.buttons !== 1)) return
  if ((event.target as HTMLElement).closest('button, a, input, select, textarea')) return

  pendingDrag.value = {
    nodeId,
    pointerId: event.pointerId,
    startX: event.clientX,
    startY: event.clientY
  }
  draftOrder.value = currentNodeIds()
  window.addEventListener('pointermove', onPointerMove, { passive: false })
  window.addEventListener('pointerup', onPointerUp)
  window.addEventListener('pointercancel', onPointerCancel)
}

function onPointerMove(event: PointerEvent) {
  const pending = pendingDrag.value
  if (!pending || event.pointerId !== pending.pointerId) return

  const x = event.clientX - pending.startX
  const y = event.clientY - pending.startY
  const hasMoved = Math.hypot(x, y) >= dragThresholdPx

  if (!dragNodeId.value) {
    if (!hasMoved) return
    dragNodeId.value = pending.nodeId
  }

  event.preventDefault()
  dragOffset.value = { x, y }
  reorderForPointer(event.clientX, event.clientY)
}

function onPointerUp(event: PointerEvent) {
  const pending = pendingDrag.value
  if (!pending || event.pointerId !== pending.pointerId) return

  if (dragNodeId.value) {
    const nextOrder = currentNodeIds()
    saveDashboardLayout(nextOrder)
    savedOrder.value = nextOrder
    suppressNextClick(pending.nodeId)
  }

  endPointerDrag()
}

function onPointerCancel(event: PointerEvent) {
  const pending = pendingDrag.value
  if (!pending || event.pointerId !== pending.pointerId) return
  endPointerDrag()
}

function endPointerDrag() {
  pendingDrag.value = null
  dragNodeId.value = null
  dragOffset.value = { x: 0, y: 0 }
  removePointerListeners()
}

function removePointerListeners() {
  window.removeEventListener('pointermove', onPointerMove)
  window.removeEventListener('pointerup', onPointerUp)
  window.removeEventListener('pointercancel', onPointerCancel)
}

function suppressNextClick(nodeId: string) {
  suppressClickNodeId.value = nodeId
  if (clickSuppressTimer) clearTimeout(clickSuppressTimer)
  clickSuppressTimer = setTimeout(() => {
    suppressClickNodeId.value = null
  }, 250)
}

function onShellClick(event: MouseEvent, nodeId: string) {
  if (suppressClickNodeId.value !== nodeId) return
  event.preventDefault()
  event.stopImmediatePropagation()
  suppressClickNodeId.value = null
}

async function reorderForPointer(clientX: number, clientY: number) {
  const draggedId = dragNodeId.value
  if (!draggedId) return

  const order = currentNodeIds()
  const from = order.indexOf(draggedId)
  if (from < 0) return

  const to = insertionIndexForPointer(clientX, clientY, draggedId)
  if (to === from) return

  const first = measureCards()
  order.splice(from, 1)
  order.splice(to, 0, draggedId)
  draftOrder.value = order
  await nextTick()
  animateCards(first, draggedId)
}

function insertionIndexForPointer(clientX: number, clientY: number, draggedId: string) {
  const cards = cardElements()
    .filter(element => element.dataset.nodeId !== draggedId)
    .map(element => ({ id: element.dataset.nodeId ?? '', rect: element.getBoundingClientRect() }))
    .sort((a, b) => Math.abs(a.rect.top - b.rect.top) > 8 ? a.rect.top - b.rect.top : a.rect.left - b.rect.left)

  for (let index = 0; index < cards.length; index += 1) {
    const rect = cards[index].rect
    if (clientY < rect.top) return index
    if (clientY <= rect.bottom) return clientX < rect.left + rect.width / 2 ? index : index + 1
  }

  return cards.length
}

function cardElements() {
  return Array.from(gridElement.value?.querySelectorAll<HTMLElement>('[data-node-id]') ?? [])
}

function measureCards() {
  const positions = new Map<string, DOMRect>()
  cardElements().forEach(element => {
    positions.set(element.dataset.nodeId ?? '', element.getBoundingClientRect())
  })
  return positions
}

function animateCards(first: Map<string, DOMRect>, draggedId: string) {
  cardElements().forEach(element => {
    const nodeId = element.dataset.nodeId ?? ''
    if (nodeId === draggedId) return
    const before = first.get(nodeId)
    if (!before) return
    const after = element.getBoundingClientRect()
    const dx = before.left - after.left
    const dy = before.top - after.top
    if (!dx && !dy) return
    element.animate([
      { transform: `translate(${dx}px, ${dy}px)` },
      { transform: 'translate(0, 0)' }
    ], { duration: 180, easing: 'cubic-bezier(0.22, 1, 0.36, 1)' })
  })
}

function dragStyle(nodeId: string) {
  if (dragNodeId.value !== nodeId) return undefined
  return {
    '--drag-x': `${dragOffset.value.x}px`,
    '--drag-y': `${dragOffset.value.y}px`
  }
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

    <div v-else ref="gridElement" class="grid-responsive node-grid" :class="{ 'is-dragging': dragNodeId }">
      <div
        v-for="node in displayedNodes"
        :key="node.id"
        class="node-card-shell"
        :class="{ dragging: dragNodeId === node.id }"
        :data-node-id="node.id"
        :style="dragStyle(node.id)"
        @pointerdown="onPointerDown($event, node.id)"
        @click.capture="onShellClick($event, node.id)"
      >
        <NodeCard
          :node="node"
          :history="store.history.value.get(node.id) || []"
          :details="store.details.value.get(node.id) || null"
          :navigation-disabled="dragNodeId === node.id"
        />
      </div>
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

.node-card-shell {
  position: relative;
  min-width: 0;
  touch-action: pan-y;
  user-select: none;
}

.node-card-shell.dragging {
  z-index: 50;
  pointer-events: none;
  transform: translate3d(var(--drag-x, 0), var(--drag-y, 0), 0) scale(1.025);
  filter: drop-shadow(0 18px 28px color-mix(in srgb, var(--glow-accent) 48%, transparent));
  cursor: grabbing;
}

.node-card-shell.dragging::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 1px dashed var(--accent);
  border-radius: var(--card-radius);
  opacity: 0.45;
  pointer-events: none;
}

.node-grid.is-dragging .node-card-shell:not(.dragging) {
  transition: transform 180ms cubic-bezier(0.22, 1, 0.36, 1);
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
