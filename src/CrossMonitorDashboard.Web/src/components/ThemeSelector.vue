<script setup lang="ts">
import { Icon } from '@iconify/vue'

defineProps<{
  themes: string[]
  current: string
}>()

const emit = defineEmits<{
  select: [theme: string]
}>()
</script>

<template>
  <div class="theme-selector">
      <button
        v-for="theme in themes"
        :key="theme"
        class="theme-card"
        :data-preview="theme"
        :class="{ active: current === theme }"
        @click="emit('select', theme)"
      >
      <span class="theme-preview"></span>
      <span class="theme-content">
        <Icon :icon="current === theme ? 'mdi-check-circle' : 'mdi-palette-swatch'" width="20" height="20" />
        <span class="theme-name">{{ theme }}</span>
      </span>
    </button>
  </div>
</template>

<style scoped>
.theme-selector {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
  gap: 0.75rem;
}

.theme-card {
  position: relative;
  min-height: 86px;
  display: flex;
  align-items: center;
  overflow: hidden;
  padding: 0.85rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--card-radius);
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  cursor: pointer;
  transition: all var(--transition-speed);
}

.theme-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background: radial-gradient(circle at 80% 20%, var(--glow-accent), transparent 45%);
  opacity: 0.85;
}

.theme-preview {
  position: absolute;
  inset: 0;
  opacity: 0.72;
}

.theme-content {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  gap: 0.55rem;
}

.theme-card[data-preview="glass-blue"] .theme-preview {
  background: linear-gradient(135deg, #06111f, #0e2e67 55%, #25d7ff);
}

.theme-card[data-preview="neon-green"] .theme-preview {
  background: linear-gradient(135deg, #020b07, #06351f 58%, #00ff8a);
}

.theme-card[data-preview="cyber-red"] .theme-preview {
  background: linear-gradient(135deg, #120409, #551326 58%, #ff3864);
}

.theme-card[data-preview="terminal-green"] .theme-preview {
  background: repeating-linear-gradient(0deg, #061006, #061006 7px, #0a1f0a 8px), linear-gradient(135deg, #001100, #00ff41);
}

.theme-card[data-preview="pixel-platformer"] .theme-preview {
  background:
    linear-gradient(90deg, rgba(0,0,0,0.18) 2px, transparent 2px),
    linear-gradient(0deg, rgba(0,0,0,0.18) 2px, transparent 2px),
    linear-gradient(135deg, #1a1a2e, #ff6b35 70%);
  background-size: 12px 12px, 12px 12px, auto;
}

.theme-card:hover {
  background: var(--bg-card-hover);
  border-color: var(--border-glow);
  transform: translateY(-2px);
}

.theme-card.active {
  border-color: var(--accent);
  box-shadow: 0 0 12px var(--glow-accent);
}

.theme-name {
  text-transform: capitalize;
}
</style>
