import { ref, onUnmounted } from 'vue'

export function useWebSocket(url: string) {
  const connected = ref(false)
  const lastMessage = ref<any>(null)
  let ws: WebSocket | null = null

  function connect() {
    try {
      ws = new WebSocket(url)
      ws.onopen = () => { connected.value = true }
      ws.onclose = () => { connected.value = false }
      ws.onmessage = (event) => {
        try {
          lastMessage.value = JSON.parse(event.data)
        } catch {
          lastMessage.value = event.data
        }
      }
      ws.onerror = () => { connected.value = false }
    } catch {
      connected.value = false
    }
  }

  function disconnect() {
    if (ws) {
      ws.close()
      ws = null
    }
    connected.value = false
  }

  onUnmounted(() => disconnect())

  return { connected, lastMessage, connect, disconnect }
}
