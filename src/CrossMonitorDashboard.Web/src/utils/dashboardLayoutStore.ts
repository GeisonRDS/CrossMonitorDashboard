import type { NodeSummary } from '../types/dashboard'

export const dashboardLayoutStorageKey = 'crossmonitor-dashboard-card-order'
const dashboardLayoutVersion = 2
export const defaultMetricOrder = ['cpu', 'temperature', 'memory', 'disk', 'download', 'upload'] as const
export type MetricLayoutId = typeof defaultMetricOrder[number]

export interface DashboardLayout {
  version: number
  cardOrder: string[]
  metricOrders?: Record<string, string[]>
}

function sanitizeOrder(order: unknown): string[] {
  if (!Array.isArray(order)) return []

  const seen = new Set<string>()
  return order.filter((id): id is string => {
    if (typeof id !== 'string' || seen.has(id)) return false
    seen.add(id)
    return true
  })
}

export function validateCardOrder(order: unknown, nodes: Pick<NodeSummary, 'id'>[]): string[] {
  const validIds = new Set(nodes.map(node => node.id))
  return sanitizeOrder(order).filter(id => validIds.has(id))
}

export function applyCardOrder<T extends Pick<NodeSummary, 'id'>>(nodes: T[], savedOrder: unknown): T[] {
  const order = validateCardOrder(savedOrder, nodes)
  if (order.length === 0) return nodes

  const nodeById = new Map(nodes.map(node => [node.id, node]))
  const orderedNodes = order.map(id => nodeById.get(id)).filter((node): node is T => Boolean(node))
  const orderedIds = new Set(order)
  const newNodes = nodes.filter(node => !orderedIds.has(node.id))
  return [...orderedNodes, ...newNodes]
}

function getStoredLayout(): DashboardLayout {
  if (typeof localStorage === 'undefined') return { version: dashboardLayoutVersion, cardOrder: [], metricOrders: {} }

  try {
    const raw = localStorage.getItem(dashboardLayoutStorageKey)
    if (!raw) return { version: dashboardLayoutVersion, cardOrder: [], metricOrders: {} }

    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) return { version: dashboardLayoutVersion, cardOrder: sanitizeOrder(parsed), metricOrders: {} }

    return {
      version: Number(parsed?.version) || dashboardLayoutVersion,
      cardOrder: sanitizeOrder(parsed?.cardOrder),
      metricOrders: typeof parsed?.metricOrders === 'object' && parsed.metricOrders !== null ? parsed.metricOrders : {}
    }
  } catch {
    return { version: dashboardLayoutVersion, cardOrder: [], metricOrders: {} }
  }
}

export function getDashboardLayout(): string[] {
  return getStoredLayout().cardOrder
}

export function saveDashboardLayout(layout: string[]) {
  if (typeof localStorage === 'undefined') return
  const current = getStoredLayout()
  const payload: DashboardLayout = {
    version: dashboardLayoutVersion,
    cardOrder: sanitizeOrder(layout),
    metricOrders: current.metricOrders ?? {}
  }
  localStorage.setItem(dashboardLayoutStorageKey, JSON.stringify(payload))
}

export function validateMetricOrder(order: unknown, metricIds: readonly string[] = defaultMetricOrder): string[] {
  const validIds = new Set(metricIds)
  return sanitizeOrder(order).filter(id => validIds.has(id))
}

export function applyMetricOrder<T extends { id: string }>(metrics: T[], savedOrder: unknown): T[] {
  const order = validateMetricOrder(savedOrder, metrics.map(metric => metric.id))
  if (order.length === 0) return metrics

  const metricById = new Map(metrics.map(metric => [metric.id, metric]))
  const orderedMetrics = order.map(id => metricById.get(id)).filter((metric): metric is T => Boolean(metric))
  const orderedIds = new Set(order)
  const newMetrics = metrics.filter(metric => !orderedIds.has(metric.id))
  return [...orderedMetrics, ...newMetrics]
}

export function getMetricOrders() {
  return getStoredLayout().metricOrders ?? {}
}

export function getMetricOrder(nodeId: string, metricIds: readonly string[] = defaultMetricOrder) {
  return validateMetricOrder(getMetricOrders()[nodeId], metricIds)
}

export function saveMetricOrder(nodeId: string, order: string[], metricIds: readonly string[] = defaultMetricOrder) {
  if (typeof localStorage === 'undefined') return
  const current = getStoredLayout()
  localStorage.setItem(dashboardLayoutStorageKey, JSON.stringify({
    version: dashboardLayoutVersion,
    cardOrder: current.cardOrder,
    metricOrders: {
      ...(current.metricOrders ?? {}),
      [nodeId]: validateMetricOrder(order, metricIds)
    }
  }))
}

export function resetDashboardLayout() {
  if (typeof localStorage === 'undefined') return
  localStorage.removeItem(dashboardLayoutStorageKey)
}
