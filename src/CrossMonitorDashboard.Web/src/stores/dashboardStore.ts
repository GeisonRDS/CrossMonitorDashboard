import { reactive, toRefs } from 'vue'
import type { NodeSummary, NodeDetails, DashboardSummary, HistoryDataPoint } from '../types/dashboard'
import { getNodes, getSummary, getNodeHistory, getNodeDetails } from '../api/dashboard'

interface DashboardState {
  nodes: NodeSummary[]
  summary: DashboardSummary | null
  history: Map<string, HistoryDataPoint[]>
  details: Map<string, NodeDetails>
  loading: boolean
  error: string | null
}

const state = reactive<DashboardState>({
  nodes: [],
  summary: null,
  history: new Map(),
  details: new Map(),
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
      await fetchNodeTelemetry(state.nodes.map(node => node.id))
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

  async function fetchNodeTelemetry(ids: string[]) {
    await Promise.all(ids.map(async id => {
      try {
        const [details, history] = await Promise.all([getNodeDetails(id), getNodeHistory(id)])
        state.details.set(id, details)
        state.history.set(id, history)
      } catch (e: any) {
        state.error = e.message
      }
    }))
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
    fetchNodeTelemetry,
    startPolling,
    stopPolling
  }
}
