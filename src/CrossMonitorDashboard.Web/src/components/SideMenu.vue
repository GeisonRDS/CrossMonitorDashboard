<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { Icon } from '@iconify/vue'

const router = useRouter()
const route = useRoute()

const menuItems = [
  { icon: 'mdi-view-dashboard', label: 'Dashboard', route: '/' },
  { icon: 'mdi-server', label: 'Nodes', route: '/nodes' },
  { icon: 'mdi-palette', label: 'Themes', route: '/settings' },
  { icon: 'mdi-cog', label: 'Settings', route: '/settings' },
  { icon: 'mdi-code-json', label: 'JSON Editor', route: '/editor' },
  { icon: 'mdi-information', label: 'About', route: '/about' }
]

function isActive(item: typeof menuItems[0]): boolean {
  if (item.route === '/') return route.path === '/'
  return route.path.startsWith(item.route)
}

function navigate(item: typeof menuItems[0]) {
  router.push(item.route)
}
</script>

<template>
  <nav class="side-menu">
    <div class="menu-items">
      <button
        v-for="item in menuItems"
        :key="item.route"
        class="menu-item"
        :class="{ active: isActive(item) }"
        :title="item.label"
        @click="navigate(item)"
      >
        <Icon :icon="item.icon" width="24" height="24" />
        <span class="tooltip">{{ item.label }}</span>
      </button>
    </div>
    <div class="menu-footer">
      <div class="status-indicator" title="System Status">
        <span class="status-dot"></span>
        <span class="version-text">v1.0.0</span>
      </div>
    </div>
  </nav>
</template>

<style scoped>
.side-menu {
  position: fixed;
  left: 0;
  top: 0;
  bottom: 0;
  width: 56px;
  background: var(--bg-sidebar);
  border-right: 1px solid var(--border-color);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  z-index: 100;
  transition: background var(--transition-speed);
}

.menu-items {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 0;
  gap: 4px;
}

.menu-item {
  position: relative;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  color: var(--text-muted);
  border-radius: 10px;
  transition: all var(--transition-speed);
  cursor: pointer;
}

.menu-item:hover {
  background: var(--bg-card);
  color: var(--text-primary);
}

.menu-item.active {
  background: var(--bg-card-hover);
  color: var(--accent);
  box-shadow: 0 0 12px var(--glow-accent);
}

.tooltip {
  position: absolute;
  left: 52px;
  background: rgba(0, 0, 0, 0.9);
  color: var(--text-primary);
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 12px;
  white-space: nowrap;
  pointer-events: none;
  opacity: 0;
  transform: translateX(-4px);
  transition: all 0.2s;
  font-family: var(--font-mono);
}

.menu-item:hover .tooltip {
  opacity: 1;
  transform: translateX(0);
}

.menu-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 12px 0;
}

.status-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--success);
  box-shadow: 0 0 6px var(--glow-success);
}

.version-text {
  font-size: 8px;
  color: var(--text-muted);
  font-family: var(--font-mono);
}
</style>
