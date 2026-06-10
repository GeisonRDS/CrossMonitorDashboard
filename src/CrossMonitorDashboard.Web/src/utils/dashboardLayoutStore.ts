import type { NodeSummary } from '../types/dashboard'

export const dashboardLayoutStorageKey = 'crossmonitor-dashboard-card-order'
const dashboardLayoutVersion = 1

export interface DashboardLayout {
  version: number
  cardOrder: string[]
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

export function getDashboardLayout(): string[] {
  if (typeof localStorage === 'undefined') return []

  try {
    const raw = localStorage.getItem(dashboardLayoutStorageKey)
    if (!raw) return []

    const parsed = JSON.parse(raw)
    return sanitizeOrder(Array.isArray(parsed) ? parsed : parsed?.cardOrder)
  } catch {
    return []
  }
}

export function saveDashboardLayout(layout: string[]) {
  if (typeof localStorage === 'undefined') return
  const payload: DashboardLayout = {
    version: dashboardLayoutVersion,
    cardOrder: sanitizeOrder(layout)
  }
  localStorage.setItem(dashboardLayoutStorageKey, JSON.stringify(payload))
}

export function resetDashboardLayout() {
  if (typeof localStorage === 'undefined') return
  localStorage.removeItem(dashboardLayoutStorageKey)
}
