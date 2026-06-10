<script setup lang="ts">
import { onMounted, watch } from 'vue'
import SideMenu from './components/SideMenu.vue'
import { useTheme } from './composables/useTheme'
import { useDashboardStore } from './stores/dashboardStore'

const { loadTheme, visualSettings } = useTheme()
const store = useDashboardStore()

onMounted(async () => {
  await loadTheme()
  store.startPolling(visualSettings.value.refreshSeconds * 1000)
})

watch(() => visualSettings.value.refreshSeconds, (seconds) => {
  store.startPolling(seconds * 1000)
})
</script>

<template>
  <div class="app-container">
    <div class="app-background"></div>
    <div class="app-ambient app-ambient-a"></div>
    <div class="app-ambient app-ambient-b"></div>
    <div class="app-grid"></div>
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
  isolation: isolate;
}

.app-background {
  position: fixed;
  inset: 0;
  background: var(--bg-primary);
  z-index: -1;
  transition: background var(--transition-speed);
  overflow: hidden;
}

.app-background::before,
.app-background::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.app-background::before {
  background-image: var(--custom-bg-image, none);
  background-size: cover;
  background-position: center;
  background-repeat: no-repeat;
  opacity: var(--custom-bg-opacity, 0.8);
  filter: blur(var(--custom-bg-blur, 0px));
  transform: scale(1.04);
}

.app-background::after {
  background: rgba(0, 0, 0, var(--custom-bg-overlay, 0));
}

.app-ambient {
  position: fixed;
  width: 42vw;
  height: 42vw;
  border-radius: 999px;
  filter: blur(70px);
  opacity: 0.34;
  pointer-events: none;
  z-index: -1;
}

.app-ambient-a {
  top: -12vw;
  right: -8vw;
  background: var(--accent);
}

.app-ambient-b {
  bottom: -18vw;
  left: 18vw;
  background: var(--critical);
  opacity: 0.16;
}

.app-grid {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: -1;
  opacity: 0.18;
  background-image:
    linear-gradient(rgba(255,255,255,0.035) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255,255,255,0.035) 1px, transparent 1px);
  background-size: 42px 42px;
  mask-image: radial-gradient(circle at 55% 25%, black, transparent 70%);
}

.main-content {
  flex: 1;
  margin-left: 58px;
  padding: 1.75rem;
  overflow-y: auto;
  min-height: 100vh;
}

@media (max-width: 760px) {
  .main-content {
    margin-left: 50px;
    padding: 1rem;
  }
}
</style>
