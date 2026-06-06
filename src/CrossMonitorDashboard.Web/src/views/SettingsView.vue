<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useTheme, type VisualSettings } from '../composables/useTheme'
import ThemeSelector from '../components/ThemeSelector.vue'
import { getThemes } from '../api/dashboard'

const { currentTheme, availableThemes, visualSettings, applyTheme, applySettings } = useTheme()
const saved = ref(false)

const localSettings = reactive<VisualSettings>({
  theme: 'glass-blue',
  refreshSeconds: 5,
  cardSize: 'normal',
  animations: { enabled: true, intensity: 'medium' },
  background: { type: 'gradient', imagePath: '', opacity: 0.8, blur: 12, overlay: 0.5 }
})

onMounted(async () => {
  try {
    availableThemes.value = await getThemes()
  } catch {
    availableThemes.value = ['glass-blue', 'neon-green', 'cyber-red', 'terminal-green', 'pixel-platformer']
  }
  Object.assign(localSettings, JSON.parse(JSON.stringify(visualSettings.value)))
})

function selectTheme(theme: string) {
  localSettings.theme = theme
  applyTheme(theme)
  saved.value = true
  setTimeout(() => { saved.value = false }, 1300)
}

function saveSettings() {
  applySettings(JSON.parse(JSON.stringify(localSettings)))
  saved.value = true
  setTimeout(() => { saved.value = false }, 1300)
}
</script>

<template>
  <div class="settings-page fade-in">
    <div class="settings-header">
      <p class="eyebrow text-mono">Visual control</p>
      <h1>Theme & display</h1>
      <p>Changes are saved locally in this browser only. Tokens and agent configuration are never stored here.</p>
    </div>

    <section class="settings-section glass-card">
      <div class="section-title">
        <h3>Theme</h3>
        <span class="current-theme text-mono">{{ currentTheme }}</span>
      </div>
      <ThemeSelector :themes="availableThemes" :current="currentTheme" @select="selectTheme" />
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>Display</h3></div>
      <div class="setting-row">
        <label>Refresh Interval</label>
        <select v-model.number="localSettings.refreshSeconds" class="setting-input">
          <option :value="2">2 seconds</option>
          <option :value="5">5 seconds</option>
          <option :value="10">10 seconds</option>
          <option :value="30">30 seconds</option>
          <option :value="60">60 seconds</option>
        </select>
      </div>
      <div class="setting-row stacked">
        <label>Card Size</label>
        <div class="radio-group">
          <label><input type="radio" v-model="localSettings.cardSize" value="compact" /> Compact</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="normal" /> Normal</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="wide" /> Wide</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="detailed" /> Detailed</label>
        </div>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>Motion</h3></div>
      <div class="setting-row">
        <label>Enable Animations</label>
        <label class="toggle">
          <input type="checkbox" v-model="localSettings.animations.enabled" />
          <span class="toggle-slider"></span>
        </label>
      </div>
      <div v-if="localSettings.animations.enabled" class="setting-row">
        <label>Intensity</label>
        <select v-model="localSettings.animations.intensity" class="setting-input">
          <option value="low">Low</option>
          <option value="medium">Medium</option>
          <option value="high">High</option>
        </select>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>Background</h3></div>
      <div class="setting-row">
        <label>Type</label>
        <select v-model="localSettings.background.type" class="setting-input">
          <option value="gradient">Gradient</option>
          <option value="solid">Solid</option>
          <option value="image">Image</option>
        </select>
      </div>
      <div v-if="localSettings.background.type === 'image'" class="setting-row">
        <label>Image Path</label>
        <input v-model="localSettings.background.imagePath" placeholder="/images/bg.jpg" class="setting-input" />
      </div>
      <div class="setting-row">
        <label>Opacity ({{ Math.round(localSettings.background.opacity * 100) }}%)</label>
        <input type="range" v-model.number="localSettings.background.opacity" :min="0" :max="1" :step="0.05" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>Blur ({{ localSettings.background.blur }}px)</label>
        <input type="range" v-model.number="localSettings.background.blur" :min="0" :max="50" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>Overlay ({{ Math.round(localSettings.background.overlay * 100) }}%)</label>
        <input type="range" v-model.number="localSettings.background.overlay" :min="0" :max="1" :step="0.05" class="setting-slider" />
      </div>
    </section>

    <div class="settings-actions">
      <button class="save-btn" @click="saveSettings">Save visual settings</button>
      <span v-if="saved" class="saved-message text-mono">Saved locally</span>
    </div>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 920px;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.settings-header {
  padding: 0.35rem 0 0.2rem;
}

.eyebrow {
  margin-bottom: 0.35rem;
  color: var(--accent-light);
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.16em;
}

.settings-header h1 {
  font-size: clamp(1.55rem, 2.5vw, 2.3rem);
}

.settings-header p {
  max-width: 680px;
  margin-top: 0.45rem;
  color: var(--text-secondary);
  line-height: 1.55;
}

.settings-section {
  padding: 1.15rem;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.section-title h3 {
  font-size: 0.8rem;
  font-weight: 800;
  color: var(--text-secondary);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.1em;
}

.current-theme {
  color: var(--accent-light);
  font-size: 0.76rem;
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.85rem 0;
  border-bottom: 1px solid var(--border-color);
}

.setting-row.stacked {
  align-items: flex-start;
}

.setting-row:last-child {
  border-bottom: none;
}

.setting-row label {
  font-size: 0.88rem;
  color: var(--text-primary);
}

.setting-input {
  background: rgba(255,255,255,0.06);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  padding: 0.52rem 0.75rem;
  border-radius: 10px;
  font-family: var(--font-mono);
  font-size: 0.8rem;
  min-width: 150px;
}

.setting-slider {
  width: min(240px, 45vw);
  accent-color: var(--accent);
}

.radio-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  justify-content: flex-end;
}

.radio-group label {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 0.48rem 0.65rem;
  border: 1px solid var(--border-color);
  border-radius: 999px;
  background: rgba(255,255,255,0.04);
  font-family: var(--font-mono);
  font-size: 0.76rem;
}

.toggle {
  position: relative;
  display: inline-block;
  width: 50px;
  height: 28px;
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
  border: 1px solid var(--border-color);
  border-radius: 999px;
  transition: all 0.3s;
}

.toggle-slider::before {
  content: '';
  position: absolute;
  height: 20px;
  width: 20px;
  left: 3px;
  bottom: 3px;
  background: var(--text-muted);
  border-radius: 50%;
  transition: all 0.3s;
}

.toggle input:checked + .toggle-slider {
  background: var(--accent);
  box-shadow: 0 0 16px var(--glow-accent);
}

.toggle input:checked + .toggle-slider::before {
  transform: translateX(22px);
  background: #fff;
}

.settings-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.save-btn {
  padding: 0.8rem 1.35rem;
  background: linear-gradient(135deg, var(--accent), var(--accent-dark));
  color: #fff;
  border: 1px solid var(--accent-light);
  border-radius: 999px;
  font-size: 0.9rem;
  font-weight: 800;
  cursor: pointer;
  transition: all var(--transition-speed);
  box-shadow: 0 0 16px var(--glow-accent);
}

.save-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 0 26px var(--glow-accent);
}

.saved-message {
  color: var(--success);
  font-size: 0.78rem;
}

@media (max-width: 640px) {
  .setting-row {
    flex-direction: column;
    align-items: stretch;
  }

  .radio-group {
    justify-content: flex-start;
  }
}
</style>
