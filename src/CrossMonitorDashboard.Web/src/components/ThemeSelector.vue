<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { useI18n } from '../composables/useI18n'

defineProps<{
  themes: string[]
  current: string
}>()

const emit = defineEmits<{
  select: [theme: string]
}>()

const { translate } = useI18n()
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
        <span class="theme-name">{{ translate(`themes.${theme}` as any) || theme }}</span>
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
    linear-gradient(90deg, rgba(0,0,0,0.2) 2px, transparent 2px),
    linear-gradient(0deg, rgba(0,0,0,0.2) 2px, transparent 2px),
    linear-gradient(135deg, #1a1a2e, #e94560 50%, #ff6b35);
  background-size: 8px 8px, 8px 8px, auto;
}

.theme-card[data-preview="terminal-mono"] .theme-preview {
  background: linear-gradient(135deg, #0a0a0a, #2a2a2a 55%, #cccccc);
}

.theme-card[data-preview="terminal-blue"] .theme-preview {
  background: linear-gradient(135deg, #0a0e14, #142438 58%, #4d9fff);
}

.theme-card[data-preview="terminal-red"] .theme-preview {
  background: linear-gradient(135deg, #140a0a, #381414 58%, #ff4d4d);
}

.theme-card[data-preview="terminal-green-matte"] .theme-preview {
  background: repeating-linear-gradient(0deg, #081408, #081408 6px, #0c1f0c 7px), linear-gradient(135deg, #020f02, #3ab83a);
}

.theme-card[data-preview="material-slate"] .theme-preview {
  background: linear-gradient(135deg, #101418, #1e2833 55%, #5b7a9a);
}

.theme-card[data-preview="material-graphite"] .theme-preview {
  background: linear-gradient(135deg, #121212, #2a2a2a 55%, #9e9e9e);
}

.theme-card[data-preview="material-ocean"] .theme-preview {
  background: linear-gradient(135deg, #0a141e, #0f2a38 58%, #26c6da);
}

.theme-card[data-preview="material-forest"] .theme-preview {
  background: linear-gradient(135deg, #0a120a, #1a2e1a 55%, #66bb6a);
}

.theme-card[data-preview="hacker-prompt"] .theme-preview {
  background:
    linear-gradient(0deg, rgba(51,255,51,0.05) 1px, transparent 1px),
    linear-gradient(135deg, #000000, #0a1a0a 55%, #00ff41);
  background-size: 100% 4px, auto;
}

.theme-card[data-preview="code-editor"] .theme-preview {
  background:
    repeating-linear-gradient(0deg, transparent, transparent 18px, rgba(255,255,255,0.03) 19px),
    linear-gradient(135deg, #1e1e2e, #2d2d44 55%, #7c3aed);
  background-size: auto 20px, auto;
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
