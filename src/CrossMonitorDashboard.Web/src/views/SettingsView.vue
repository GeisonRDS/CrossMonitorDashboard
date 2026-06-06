<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useTheme, type VisualSettings, type MetricKey, type MetricChartType } from '../composables/useTheme'
import { useI18n } from '../composables/useI18n'
import { availableBackgrounds } from '../config/backgrounds'
import ThemeSelector from '../components/ThemeSelector.vue'
import { getThemes } from '../api/dashboard'

const { currentTheme, availableThemes, visualSettings, applyTheme, applySettings } = useTheme()
const { translate, currentLocale, setLocale, availableLocales } = useI18n()
const saved = ref(false)
const chartTypes: Array<{ value: MetricChartType; label: string; description: string }> = [
  { value: 'line-glow', label: 'line-glow', description: 'Linha com brilho' },
  { value: 'radial-gauge', label: 'radial-gauge', description: 'Donut radial' },
  { value: 'bar-pulse', label: 'bar-pulse', description: 'Barras verticais' }
]
const metricOptions: Array<{ key: MetricKey; label: string }> = [
  { key: 'cpu', label: 'CPU' },
  { key: 'memory', label: 'RAM' },
  { key: 'disk', label: 'Disco' },
  { key: 'temperature', label: 'Temperatura' },
  { key: 'network', label: 'Rede' }
]

const localSettings = reactive<VisualSettings>({
  theme: 'glass-blue',
  refreshSeconds: 5,
  cardSize: 'normal',
  animations: { enabled: true, intensity: 'medium' },
  background: { type: 'gradient', imagePath: '', opacity: 0.8, blur: 12, overlay: 0.5 },
  metricCharts: {
    cpu: 'radial-gauge',
    memory: 'line-glow',
    disk: 'bar-pulse',
    temperature: 'line-glow',
    network: 'line-glow'
  }
})

onMounted(async () => {
  try {
    availableThemes.value = await getThemes()
  } catch {
    availableThemes.value = [
      'glass-blue', 'neon-green', 'cyber-red', 'terminal-green', 'pixel-platformer',
      'terminal-mono', 'terminal-blue', 'terminal-red', 'terminal-green-matte',
      'material-slate', 'material-graphite', 'material-ocean', 'material-forest',
      'hacker-prompt', 'code-editor'
    ]
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

function saveImmediately() {
  saveSettings()
}
</script>

<template>
  <div class="settings-page fade-in">
    <div class="settings-header">
      <p class="eyebrow text-mono">{{ translate('settings.visualControl') }}</p>
      <h1>{{ translate('settings.themeAndDisplay') }}</h1>
      <p>{{ translate('settings.saveInfo') }}</p>
    </div>

    <section class="settings-section glass-card">
      <div class="section-title">
        <h3>{{ translate('settings.language') }}</h3>
      </div>
      <div class="setting-row">
        <label>{{ translate('settings.language') }}</label>
        <select :value="currentLocale" class="setting-input" @change="(e) => setLocale((e.target as HTMLSelectElement).value as 'en' | 'pt')">
          <option v-for="lang in availableLocales()" :key="lang.value" :value="lang.value">
            {{ lang.label }}
          </option>
        </select>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title">
        <h3>{{ translate('settings.theme') }}</h3>
        <span class="current-theme text-mono">{{ translate(`themes.${currentTheme}` as any) || currentTheme }}</span>
      </div>
      <ThemeSelector :themes="availableThemes" :current="currentTheme" @select="selectTheme" />
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>{{ translate('settings.chartsByMetric') }}</h3></div>
      <div class="chart-type-grid">
        <label v-for="metric in metricOptions" :key="metric.key" class="chart-type-row">
          <span>{{ translate(`metrics.${metric.key}Full` as any) || metric.label }}</span>
          <select v-model="localSettings.metricCharts[metric.key]" class="setting-input" @change="saveImmediately">
            <option v-for="type in chartTypes" :key="type.value" :value="type.value">
              {{ type.label }}
            </option>
          </select>
        </label>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>{{ translate('settings.display') }}</h3></div>
      <div class="setting-row">
        <label>{{ translate('settings.refreshInterval') }}</label>
        <select v-model.number="localSettings.refreshSeconds" class="setting-input">
          <option :value="2">{{ translate('settings.seconds', { value: 2 }) }}</option>
          <option :value="5">{{ translate('settings.seconds', { value: 5 }) }}</option>
          <option :value="10">{{ translate('settings.seconds', { value: 10 }) }}</option>
          <option :value="30">{{ translate('settings.seconds', { value: 30 }) }}</option>
          <option :value="60">{{ translate('settings.seconds', { value: 60 }) }}</option>
        </select>
      </div>
      <div class="setting-row stacked">
        <label>{{ translate('settings.cardSize') }}</label>
        <div class="radio-group">
          <label><input type="radio" v-model="localSettings.cardSize" value="compact" /> {{ translate('settings.compact') }}</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="normal" /> {{ translate('settings.normal') }}</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="wide" /> {{ translate('settings.wide') }}</label>
          <label><input type="radio" v-model="localSettings.cardSize" value="detailed" /> {{ translate('settings.detailed') }}</label>
        </div>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>{{ translate('settings.motion') }}</h3></div>
      <div class="setting-row">
        <label>{{ translate('settings.enableAnimations') }}</label>
        <label class="toggle">
          <input type="checkbox" v-model="localSettings.animations.enabled" />
          <span class="toggle-slider"></span>
        </label>
      </div>
      <div v-if="localSettings.animations.enabled" class="setting-row">
        <label>{{ translate('settings.intensity') }}</label>
        <select v-model="localSettings.animations.intensity" class="setting-input">
          <option value="low">{{ translate('settings.low') }}</option>
          <option value="medium">{{ translate('settings.medium') }}</option>
          <option value="high">{{ translate('settings.high') }}</option>
        </select>
      </div>
    </section>

    <section class="settings-section glass-card">
      <div class="section-title"><h3>{{ translate('settings.background') }}</h3></div>
      <div class="setting-row">
        <label>{{ translate('settings.type') }}</label>
        <select v-model="localSettings.background.type" class="setting-input">
          <option value="gradient">{{ translate('settings.gradient') }}</option>
          <option value="solid">{{ translate('settings.solid') }}</option>
          <option value="image">{{ translate('settings.image') }}</option>
        </select>
      </div>
      <div v-if="localSettings.background.type === 'image'" class="setting-row">
        <label>{{ translate('settings.imagePath') }}</label>
        <select v-model="localSettings.background.imagePath" class="setting-input">
          <option
            v-for="bg in availableBackgrounds"
            :key="bg.id"
            :value="bg.path"
          >
            {{ currentLocale === 'pt' ? bg.namePt : bg.name }}
          </option>
        </select>
        <p class="setting-hint">{{ translate('settings.backgroundHint', { folder: 'public/backgrounds/' }) }}</p>
      </div>
      <div class="setting-row">
        <label>{{ translate('settings.opacity', { value: Math.round(localSettings.background.opacity * 100) }) }}</label>
        <input type="range" v-model.number="localSettings.background.opacity" :min="0" :max="1" :step="0.05" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>{{ translate('settings.blur', { value: localSettings.background.blur }) }}</label>
        <input type="range" v-model.number="localSettings.background.blur" :min="0" :max="50" class="setting-slider" />
      </div>
      <div class="setting-row">
        <label>{{ translate('settings.overlay', { value: Math.round(localSettings.background.overlay * 100) }) }}</label>
        <input type="range" v-model.number="localSettings.background.overlay" :min="0" :max="1" :step="0.05" class="setting-slider" />
      </div>
    </section>

    <div class="settings-actions">
      <button class="save-btn" @click="saveSettings">{{ translate('settings.saveSettings') }}</button>
      <span v-if="saved" class="saved-message text-mono">{{ translate('settings.savedLocally') }}</span>
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

.chart-type-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 0.75rem;
}

.chart-type-row {
  display: grid;
  gap: 0.45rem;
  padding: 0.75rem;
  border-radius: 12px;
  background: var(--metric-tile-bg);
  border: 1px solid var(--border-color);
}

.chart-type-row span {
  color: var(--text-secondary);
  font-family: var(--font-mono);
  font-size: 0.76rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
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
  min-width: 150px;
}

.setting-hint {
  display: block;
  margin-top: 0.35rem;
  color: var(--text-muted);
  font-family: var(--font-mono);
  font-size: 0.68rem;
  line-height: 1.4;
}

.setting-hint code {
  background: rgba(255,255,255,0.06);
  padding: 1px 5px;
  border-radius: 4px;
  font-size: 0.65rem;
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
