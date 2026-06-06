<script setup lang="ts">
import { onMounted } from 'vue'
import SideMenu from './components/SideMenu.vue'
import { useTheme } from './composables/useTheme'
import { useDashboardStore } from './stores/dashboardStore'

const { loadTheme } = useTheme()
const store = useDashboardStore()

onMounted(() => {
  loadTheme()
  store.startPolling(5000)
})
</script>

<template>
  <div class="app-container">
    <div class="app-background"></div>
    <SideMenu />
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.app-container {
  display: flex;
  min-height: 100vh;
  position: relative;
}

.app-background {
  position: fixed;
  inset: 0;
  background: var(--bg-primary);
  z-index: -1;
  transition: background var(--transition-speed);
}

.main-content {
  flex: 1;
  margin-left: 56px;
  padding: 1.5rem;
  overflow-y: auto;
  min-height: 100vh;
}
</style>
