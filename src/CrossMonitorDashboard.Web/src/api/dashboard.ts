import type { NodeSummary, NodeDetails, HistoryDataPoint, DashboardSummary, VisualConfig } from '../types/dashboard'

const API_BASE = '/api/dashboard'

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`)
  }
  return response.json()
}

export function getNodes(): Promise<NodeSummary[]> {
  return fetchJson<NodeSummary[]>(`${API_BASE}/nodes`)
}

export function getNodeDetails(id: string): Promise<NodeDetails> {
  return fetchJson<NodeDetails>(`${API_BASE}/nodes/${id}`)
}

export function getNodeHistory(id: string): Promise<HistoryDataPoint[]> {
  return fetchJson<HistoryDataPoint[]>(`${API_BASE}/nodes/${id}/history`)
}

export function getSummary(): Promise<DashboardSummary> {
  return fetchJson<DashboardSummary>(`${API_BASE}/summary`)
}

export function getThemes(): Promise<string[]> {
  return fetchJson<string[]>(`${API_BASE}/themes`)
}

export function getPublicConfig(): Promise<VisualConfig> {
  return fetchJson<VisualConfig>(`${API_BASE}/config/public`)
}

export function validateConfig(config: any): Promise<{ valid: boolean; errors: string[] }> {
  return fetch(`${API_BASE}/config/validate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(config)
  }).then(r => r.json())
}
