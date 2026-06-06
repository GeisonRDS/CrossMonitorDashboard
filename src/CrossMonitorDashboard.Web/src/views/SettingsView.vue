<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useTheme } from '../composables/useTheme'
import ThemeSelector from '../components/ThemeSelector.vue'
import { getThemes } from '../api/dashboard'

const { currentTheme, availableThemes, applyTheme } = useTheme()

const refreshSeconds = ref(5)
const cardSize = ref('normal')
const animationsEnabled = ref(true)
const animationIntensity = ref('medium')
const backgroundType = ref('gradient')
const backgroundImagePath = ref('')
const opacity = ref(80)
const blur = ref(12)
const overlay = ref(50)

onMounted(async () => {
  try {
    const themes = await getThemes()
    availableThemes.value = themes
  } catch {
    availableThemes.value = ['glass-blue', 'neon-green', 'cyber-red', 'terminal-green', 'pixel-platformer']
  }
})

function selectTheme(theme: string) {
  applyTheme(theme)
}

function saveSettings() {
  localStorage.setItem('dashboard-settings', JSON.stringify({
    refreshSeconds: refreshSeconds.value,
    cardSize: cardSize.value,
    animations: {
      enabled: animationsEnabled.value,
      intensity: animationIntensity.value
    },
    background: {
      type: backgroundType.value,
      imagePath: backgroundImagePath.value,
      opacity: opacity.value / 100,
      blur: blur.value,
      overlay: overlay.value / 100
    }
  }))
}
</script>

<template>
  <div class="settings-page fade-in">
    <h1 class="page-title">Settings</h1>

    <section class="settings-section glass-card">
      <h3>Theme</h3>
      <ThemeSelector :themes="availableThemes" :current="currentTheme" @select="selectTheme" />
    </section>

    <section class="settings-section glass-card">
      <h3>Display</h3>
      <div class="setting-row">
        <label>Refresh Interval</label>
        <select v-model="refreshSeconds" class="setting-input">
          <option :value="2">2 seconds</option>
          <option :value="5">5 seconds</option>
          <option :value="10">10 seconds</option>
          <option :value="30">30 seconds</option>
          <option :value="60">60 seconds</option>
        </select>
      </div>
      <div class="setting-row">
        <label>Card Size</label>
        <div class="radio-group">
          <label><input type="radio" v-model="cardSize" value="compact" /> Compact</label>
          <label><input type="radio" v-model="cardSize" value="normal" /> Normal</label>
          <label><input type="radio" v-model="cardSize" value="wide" /> Wide</label>
          <label><input type="radio" v-model="cardSize" value="detailed" /> Detailed</label>
        </div>
      </div>
    </section>

    <section class="settings-section glass-card">
      <h3>Animations</h3>
      <div class="setting-row">
        <label>Enable Animations</label>
        <label class="toggle">
          <input type="checkbox" v-model="animationsEnabled" />
          <span class="toggle-slider"></span>
        </label>
      </div>
      <div v-if="animationsEnabled" class="setting-row">
        <label>Intensity</label>
        <input type="range" v-model.number="animationIntensity" :min="0" :max="100" class="setting-slider" />
      </div>
    </section>

    <section class="settings-section glass-card">
      <h3>Background</h3>
      <div class="setting-row">
        <label>Type</label>
        <select v-model="backgroundType" class="setting-input">
          <option value="gradient">Gradient</option>
          <option value="solid">Solid</option>
          <option value="image">Image</option>
        </select>
      </div>
      <div v-if="backgroundType === 'image'" class="setting-row">
        <label>Image Path</label>
        <input v-model="backgroundImagePath" placeholder="/images/bg.jpg" class="setting-input" />
      </div>
      <div class="setting-row">
        <label>Opacity ({{ opacity }}%)</label>
        <input type="range" v-model.number="opacity" :min="0" :max="100" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>Blur ({{ blur }}px)</label>
        <input type="range" v-model.number="blur" :min="0" :max="50" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>Overlay ({{ overlay }}%)</label>
        <input type="range" v-model.number="overlay" :min="0" :max="100" class="setting-slider" />
      </div>
    </section>

    <button class="save-btn" @click="saveSettings">Save Settings</button>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 700px;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-title {
  font-size: 1.3rem;
  font-weight: 600;
}

.settings-section {
  padding: 1.25rem;
}

.settings-section h3 {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 1rem;
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0;
  border-bottom: 1px solid var(--border-color);
}

.setting-row:last-child {
  border-bottom: none;
}

.setting-row label {
  font-size: 0.85rem;
  color: var(--text-primary);
}

.setting-input {
  background: rgba(255,255,255,0.05);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  padding: 0.4rem 0.75rem;
  border-radius: 6px;
  font-family: var(--font-mono);
  font-size: 0.8rem;
  min-width: 120px;
}

.setting-slider {
  width: 200px;
  accent-color: var(--accent);
}

.radio-group {
  display: flex;
  gap: 1rem;
  font-size: 0.8rem;
}

.radio-group label {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
}

.toggle {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 24px;
}

.toggle input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  inset: 0;
  background: rgba(255,255,255,0.1);
  border-radius: 24px;
  transition: all 0.3s;
}

.toggle-slider::before {
  content: '';
  position: absolute;
  height: 18px;
  width: 18px;
  left: 3px;
  bottom: 3px;
  background: var(--text-muted);
  border-radius: 50%;
  transition: all 0.3s;
}

.toggle input:checked + .toggle-slider {
  background: var(--accent);
}

.toggle input:checked + .toggle-slider::before {
  transform: translateX(20px);
  background: #fff;
}

.save-btn {
  padding: 0.75rem 2rem;
  background: var(--accent);
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  align-self: flex-start;
  transition: all var(--transition-speed);
}

.save-btn:hover {
  background: var(--accent-light);
  box-shadow: 0 0 16px var(--glow-accent);
}
</style>
