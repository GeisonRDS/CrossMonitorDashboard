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
      :class="{ active: current === theme }"
      @click="emit('select', theme)"
    >
      <Icon :icon="current === theme ? 'mdi-check-circle' : 'mdi-palette-swatch'" width="20" height="20" />
      <span class="theme-name">{{ theme }}</span>
    </button>
  </div>
</template>

<style scoped>
.theme-selector {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.theme-card {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--card-radius);
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  cursor: pointer;
  transition: all var(--transition-speed);
}

.theme-card:hover {
  background: var(--bg-card-hover);
  border-color: var(--border-glow);
}

.theme-card.active {
  border-color: var(--accent);
  box-shadow: 0 0 12px var(--glow-accent);
}

.theme-name {
  text-transform: capitalize;
}
</style>
