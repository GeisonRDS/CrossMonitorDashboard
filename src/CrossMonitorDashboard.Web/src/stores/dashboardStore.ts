import { reactive, toRefs } from 'vue'
import type { NodeSummary, DashboardSummary, HistoryDataPoint } from '../types/dashboard'
import { getNodes, getSummary, getNodeHistory } from '../api/dashboard'

interface DashboardState {
  nodes: NodeSummary[]
  summary: DashboardSummary | null
  history: Map<string, HistoryDataPoint[]>
  loading: boolean
  error: string | null
}

const state = reactive<DashboardState>({
  nodes: [],
  summary: null,
  history: new Map(),
  loading: false,
  error: null
})

let pollingTimer: ReturnType<typeof setInterval> | null = null

export function useDashboardStore() {
  async function fetchNodes() {
    try {
      state.loading = true
      state.error = null
      state.nodes = await getNodes()
    } catch (e: any) {
      state.error = e.message
    } finally {
      state.loading = false
    }
  }

  async function fetchSummary() {
    try {
      state.summary = await getSummary()
    } catch (e: any) {
      state.error = e.message
    }
  }

  async function fetchNodeHistory(id: string) {
    try {
      const data = await getNodeHistory(id)
      state.history.set(id, data)
    } catch (e: any) {
      state.error = e.message
    }
  }

  function startPolling(intervalMs: number) {
    stopPolling()
    fetchNodes()
    fetchSummary()
    pollingTimer = setInterval(() => {
      fetchNodes()
      fetchSummary()
    }, intervalMs)
  }

  function stopPolling() {
    if (pollingTimer) {
      clearInterval(pollingTimer)
      pollingTimer = null
    }
  }

  return {
    ...toRefs(state),
    fetchNodes,
    fetchSummary,
    fetchNodeHistory,
    startPolling,
    stopPolling
  }
}
