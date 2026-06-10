<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useDashboardStore } from '../stores/dashboardStore'
import { useI18n } from '../composables/useI18n'
import NodeCard from '../components/NodeCard.vue'
import { applyCardOrder, getDashboardLayout, saveDashboardLayout, validateCardOrder } from '../utils/dashboardLayoutStore'
import { calculateDragOffset, calculateDropIndex, didPassDragThreshold, insertDraggedCard, removeDraggedCard } from '../utils/dashboardDragLayout'

const dragThresholdPx = 7
const store = useDashboardStore()
const { translate } = useI18n()

interface PendingDrag {
  nodeId: string
  pointerId: number
  startX: number
  startY: number
  offsetX: number
  offsetY: number
  width: number
  height: number
  order: string[]
}

interface DragSession {
  nodeId: string
  pointerId: number
  offsetX: number
  offsetY: number
  left: number
  top: number
  width: number
  height: number
}

const savedOrder = ref<string[]>(getDashboardLayout())
const draftOrder = ref<string[]>([])
const pendingDrag = ref<PendingDrag | null>(null)
const dragSession = ref<DragSession | null>(null)
const suppressNextClick = ref(false)
const gridElement = ref<HTMLElement | null>(null)
let clickSuppressTimer: ReturnType<typeof setTimeout> | null = null

const displayedNodes = computed(() => {
  const ordered = applyCardOrder(store.nodes.value, dragSession.value ? draftOrder.value : savedOrder.value)
  return dragSession.value ? ordered.filter(node => node.id !== dragSession.value?.nodeId) : ordered
})

const floatingNode = computed(() => {
  const nodeId = dragSession.value?.nodeId
  return nodeId ? store.nodes.value.find(node => node.id === nodeId) ?? null : null
})

watch(
  () => store.nodes.value.map(node => node.id),
  () => {
    const session = dragSession.value
    if (!session) return
    draftOrder.value = removeDraggedCard(completeOrder(validateCardOrder(draftOrder.value, store.nodes.value)), session.nodeId)
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

  const shell = (event.currentTarget as HTMLElement)
  const rect = shell.getBoundingClientRect()
  const offset = calculateDragOffset({ clientX: event.clientX, clientY: event.clientY }, rect)

  pendingDrag.value = {
    nodeId,
    pointerId: event.pointerId,
    startX: event.clientX,
    startY: event.clientY,
    offsetX: offset.x,
    offsetY: offset.y,
    width: rect.width,
    height: rect.height,
    order: applyCardOrder(store.nodes.value, savedOrder.value).map(node => node.id)
  }

  window.addEventListener('pointermove', onPointerMove, { passive: false })
  window.addEventListener('pointerup', onPointerUp)
  window.addEventListener('pointercancel', onPointerCancel)
}

function onPointerMove(event: PointerEvent) {
  const pending = pendingDrag.value
  const session = dragSession.value
  if ((!pending && !session) || event.pointerId !== (session?.pointerId ?? pending?.pointerId)) return

  if (!session && pending) {
    const passedThreshold = didPassDragThreshold(
      { clientX: pending.startX, clientY: pending.startY },
      { clientX: event.clientX, clientY: event.clientY },
      dragThresholdPx
    )
    if (!passedThreshold) return
    startDrag(event, pending)
  } else if (session) {
    updateFloatingPosition(event)
  }

  event.preventDefault()
}

function startDrag(event: PointerEvent, pending: PendingDrag) {
  const first = measureCards()
  draftOrder.value = removeDraggedCard(pending.order, pending.nodeId)
  dragSession.value = {
    nodeId: pending.nodeId,
    pointerId: pending.pointerId,
    offsetX: pending.offsetX,
    offsetY: pending.offsetY,
    left: event.clientX - pending.offsetX,
    top: event.clientY - pending.offsetY,
    width: pending.width,
    height: pending.height
  }
  nextTick(() => animateCards(first))
}

function updateFloatingPosition(event: PointerEvent) {
  const session = dragSession.value
  if (!session) return
  dragSession.value = {
    ...session,
    left: event.clientX - session.offsetX,
    top: event.clientY - session.offsetY
  }
}

async function onPointerUp(event: PointerEvent) {
  const pending = pendingDrag.value
  const session = dragSession.value
  if ((!pending && !session) || event.pointerId !== (session?.pointerId ?? pending?.pointerId)) return

  if (session) {
    updateFloatingPosition(event)
    const first = measureCards()
    const dropIndex = calculateDropIndex(
      { clientX: event.clientX, clientY: event.clientY },
      cardElements().map(element => element.getBoundingClientRect())
    )
    const nextOrder = insertDraggedCard(currentNodeIds(), session.nodeId, dropIndex)
    saveDashboardLayout(nextOrder)
    savedOrder.value = nextOrder
    suppressGeneratedClick()
    endPointerDrag()
    await nextTick()
    animateCards(first)
    return
  }

  endPointerDrag()
}

function onPointerCancel(event: PointerEvent) {
  const pending = pendingDrag.value
  const session = dragSession.value
  if ((!pending && !session) || event.pointerId !== (session?.pointerId ?? pending?.pointerId)) return
  endPointerDrag()
}

function endPointerDrag() {
  pendingDrag.value = null
  dragSession.value = null
  draftOrder.value = []
  removePointerListeners()
}

function removePointerListeners() {
  window.removeEventListener('pointermove', onPointerMove)
  window.removeEventListener('pointerup', onPointerUp)
  window.removeEventListener('pointercancel', onPointerCancel)
}

function suppressGeneratedClick() {
  suppressNextClick.value = true
  if (clickSuppressTimer) clearTimeout(clickSuppressTimer)
  clickSuppressTimer = setTimeout(() => {
    suppressNextClick.value = false
  }, 250)
}

function onShellClick(event: MouseEvent) {
  if (!suppressNextClick.value) return
  event.preventDefault()
  event.stopImmediatePropagation()
  suppressNextClick.value = false
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

function animateCards(first: Map<string, DOMRect>) {
  cardElements().forEach(element => {
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
    ], { duration: 180, easing: 'cubic-bezier(0.22, 1, 0.36, 1)' })
  })
}

const floatingStyle = computed(() => {
  const session = dragSession.value
  if (!session) return undefined
  return {
    left: `${session.left}px`,
    top: `${session.top}px`,
    width: `${session.width}px`,
    height: `${session.height}px`
  }
})
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

    <div v-else ref="gridElement" class="grid-responsive node-grid" :class="{ 'is-dragging': dragSession }">
      <div
        v-for="node in displayedNodes"
        :key="node.id"
        class="node-card-shell"
        :data-node-id="node.id"
        @pointerdown="onPointerDown($event, node.id)"
        @click.capture="onShellClick"
      >
        <NodeCard
          :node="node"
          :history="store.history.value.get(node.id) || []"
          :details="store.details.value.get(node.id) || null"
        />
      </div>
    </div>

    <div v-if="floatingNode && dragSession" class="floating-card-shell" :style="floatingStyle" aria-hidden="true">
      <NodeCard
        :node="floatingNode"
        :history="store.history.value.get(floatingNode.id) || []"
        :details="store.details.value.get(floatingNode.id) || null"
        navigation-disabled
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

.node-card-shell {
  position: relative;
  min-width: 0;
  touch-action: pan-y;
  user-select: none;
}

.node-grid.is-dragging .node-card-shell {
  transition: transform 180ms cubic-bezier(0.22, 1, 0.36, 1);
}

.floating-card-shell {
  position: fixed;
  z-index: 1000;
  pointer-events: none;
  transform: scale(1.02);
  transform-origin: center;
  filter: drop-shadow(0 18px 30px color-mix(in srgb, var(--glow-accent) 52%, transparent));
  cursor: grabbing;
}

.floating-card-shell :deep(.node-card) {
  height: 100%;
  margin: 0;
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
