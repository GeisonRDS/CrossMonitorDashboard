<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { Icon } from '@iconify/vue'

const router = useRouter()
const route = useRoute()

const menuItems = [
  { icon: 'mdi:view-dashboard-outline', label: 'Dashboard', route: '/' },
  { icon: 'mdi:palette-outline', label: 'Themes', route: '/settings' },
  { icon: 'mdi:information-outline', label: 'About', route: '/about' }
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
  <nav class="side-menu" aria-label="Primary navigation">
    <div class="menu-rail">
      <button
        v-for="item in menuItems"
        :key="item.label"
        class="menu-item"
        :class="{ active: isActive(item) }"
        :title="item.label"
        :aria-label="item.label"
        @click="navigate(item)"
      >
        <span class="active-beam"></span>
        <Icon class="menu-icon" :icon="item.icon" width="25" height="25" aria-hidden="true" />
        <span class="tooltip">{{ item.label }}</span>
      </button>
    </div>
    <div class="menu-footer" title="Dashboard online">
      <span class="status-dot"></span>
    </div>
  </nav>
</template>

<style scoped>
.side-menu {
  position: fixed;
  left: 14px;
  top: 14px;
  bottom: 14px;
  width: 58px;
  z-index: 300;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  background:
    linear-gradient(180deg, rgba(255,255,255,0.08), rgba(255,255,255,0.02)),
    var(--bg-sidebar);
  border: 1px solid var(--border-color);
  border-radius: 22px;
  box-shadow: 0 24px 70px rgba(0, 0, 0, 0.45), inset 0 1px 0 rgba(255,255,255,0.08);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
}

.menu-rail {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.menu-item {
  position: relative;
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
  background: rgba(255, 255, 255, 0.045);
  border: 1px solid transparent;
  border-radius: 15px;
  transition: transform var(--transition-speed), color var(--transition-speed), background var(--transition-speed), box-shadow var(--transition-speed), border-color var(--transition-speed);
  cursor: pointer;
  overflow: visible;
}

.menu-icon {
  position: relative;
  z-index: 2;
  display: block;
  color: currentColor;
  opacity: 1;
  filter: drop-shadow(0 0 5px rgba(255,255,255,0.18));
}

.menu-icon :deep(svg),
.menu-icon :deep(path) {
  color: currentColor;
  fill: currentColor;
  stroke: currentColor;
  opacity: 1;
}

.menu-item:hover {
  color: var(--text-primary);
  background: var(--bg-card-hover);
  border-color: var(--border-glow);
  transform: translateX(3px) scale(1.04);
}

.menu-item.active {
  color: #fff;
  background: linear-gradient(135deg, var(--accent), var(--accent-dark));
  border-color: var(--accent-light);
  box-shadow: 0 0 18px var(--glow-accent), inset 0 1px 0 rgba(255,255,255,0.28);
}

.active-beam {
  position: absolute;
  left: -9px;
  width: 3px;
  height: 24px;
  border-radius: 4px;
  background: var(--accent-light);
  box-shadow: 0 0 14px var(--glow-accent);
  opacity: 0;
  transform: scaleY(0.5);
  transition: all var(--transition-speed);
}

.menu-item.active .active-beam {
  opacity: 1;
  transform: scaleY(1);
}

.tooltip {
  position: absolute;
  left: 52px;
  z-index: 10;
  background: rgba(2, 8, 18, 0.95);
  color: var(--text-primary);
  padding: 7px 11px;
  border-radius: 10px;
  border: 1px solid var(--border-color);
  font-size: 12px;
  white-space: nowrap;
  pointer-events: none;
  opacity: 0;
  transform: translateX(-6px);
  transition: all 0.2s ease;
  font-family: var(--font-mono);
  box-shadow: 0 12px 30px rgba(0,0,0,0.35);
}

.menu-item:hover .tooltip {
  opacity: 1;
  transform: translateX(0);
}

.menu-footer {
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  background: rgba(255,255,255,0.045);
  border: 1px solid var(--border-color);
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--success);
  box-shadow: 0 0 16px var(--glow-success), 0 0 2px #fff inset;
}

@media (max-width: 760px) {
  .side-menu {
    left: 8px;
    top: 8px;
    bottom: 8px;
    width: 50px;
    border-radius: 18px;
  }

  .menu-item {
    width: 38px;
    height: 38px;
  }
}
</style>
