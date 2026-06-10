<script setup lang="ts">
import { onMounted, onUnmounted, reactive, ref } from 'vue'
import { useTheme, type VisualSettings, type MetricKey, type MetricChartType } from '../composables/useTheme'
import { useI18n } from '../composables/useI18n'
import ThemeSelector from '../components/ThemeSelector.vue'
import { getThemes } from '../api/dashboard'
import {
  deleteBackgroundImage,
  getBackgroundImage,
  replaceBackgroundImage,
  validateBackgroundImageBasics,
  type BackgroundImageMetadata
} from '../utils/backgroundImageStore'

const { currentTheme, availableThemes, visualSettings, applyTheme, applySettings } = useTheme()
const { translate, currentLocale, setLocale, availableLocales } = useI18n()
const saved = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const backgroundMetadata = ref<BackgroundImageMetadata | null>(null)
const backgroundPreviewUrl = ref('')
const backgroundError = ref('')
const backgroundWarning = ref('')
const backgroundInfo = ref('')
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
      'hacker-prompt', 'code-editor', 'black-white'
    ]
  }
  Object.assign(localSettings, JSON.parse(JSON.stringify(visualSettings.value)))
  await loadSavedBackgroundPreview()
})

onUnmounted(() => {
  revokeBackgroundPreview()
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

function revokeBackgroundPreview() {
  if (backgroundPreviewUrl.value) {
    URL.revokeObjectURL(backgroundPreviewUrl.value)
    backgroundPreviewUrl.value = ''
  }
}

async function loadSavedBackgroundPreview() {
  backgroundError.value = ''
  try {
    const stored = await getBackgroundImage()
    backgroundMetadata.value = stored?.metadata ?? null
    revokeBackgroundPreview()
    if (stored) {
      backgroundPreviewUrl.value = URL.createObjectURL(stored.blob)
    }
  } catch {
    backgroundError.value = translate('settings.backgroundLoadError')
  }
}

function triggerImagePicker() {
  fileInput.value?.click()
}

function backgroundMessage(key?: string) {
  if (!key) return ''
  const map: Record<string, string> = {
    tooLarge: 'settings.backgroundTooLarge',
    invalidFormat: 'settings.backgroundInvalidFormat',
    loadFailed: 'settings.backgroundReadError',
    storageUnsupported: 'settings.backgroundStorageUnsupported',
    lowResolution: 'settings.backgroundLowResolution'
  }
  return translate((map[key] ?? key) as any)
}

async function onBackgroundFileSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  backgroundError.value = ''
  backgroundWarning.value = ''
  backgroundInfo.value = ''
  if (!file) return

  const basics = validateBackgroundImageBasics(file)
  if (!basics.valid) {
    backgroundError.value = backgroundMessage(basics.errorKey)
    return
  }

  try {
    const result = await replaceBackgroundImage(file)
    backgroundMetadata.value = result.metadata
    backgroundWarning.value = backgroundMessage(result.warningKey)
    backgroundInfo.value = `${translate('settings.backgroundStored')} ${translate('settings.backgroundIndependent')}`
    localSettings.background.type = 'image'
    localSettings.background.imagePath = ''
    applySettings(JSON.parse(JSON.stringify(localSettings)))
    await loadSavedBackgroundPreview()
    saved.value = true
    setTimeout(() => { saved.value = false }, 1300)
  } catch (error: any) {
    backgroundError.value = backgroundMessage(error?.message) || translate('settings.backgroundSaveError')
  }
}

async function removeBackgroundImage() {
  backgroundError.value = ''
  backgroundWarning.value = ''
  backgroundInfo.value = ''
  try {
    await deleteBackgroundImage()
    backgroundMetadata.value = null
    revokeBackgroundPreview()
    applySettings(JSON.parse(JSON.stringify(localSettings)))
    saved.value = true
    setTimeout(() => { saved.value = false }, 1300)
  } catch {
    backgroundError.value = translate('settings.backgroundSaveError')
  }
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
      <div v-if="localSettings.background.type === 'image'" class="setting-row stacked image-setting-row">
        <label>{{ translate('settings.imagePath') }}</label>
        <div class="background-picker">
          <input
            ref="fileInput"
            class="file-input"
            type="file"
            accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
            :aria-label="translate('settings.selectImage')"
            @change="onBackgroundFileSelected"
          />
          <div class="background-actions">
            <button type="button" class="secondary-btn" @click="triggerImagePicker">
              {{ backgroundMetadata ? translate('settings.changeImage') : translate('settings.selectImage') }}
            </button>
            <button v-if="backgroundMetadata" type="button" class="secondary-btn danger" @click="removeBackgroundImage">
              {{ translate('settings.removeImage') }}
            </button>
          </div>
          <div class="background-current">
            <span class="text-mono">{{ translate('settings.currentImage') }}</span>
            <strong>{{ backgroundMetadata?.name || translate('settings.noImageSelected') }}</strong>
          </div>
          <img
            v-if="backgroundPreviewUrl"
            class="background-preview"
            :src="backgroundPreviewUrl"
            :alt="translate('settings.backgroundPreview')"
          />
          <p class="setting-hint">{{ translate('settings.backgroundUploadHint') }}</p>
          <p v-if="backgroundWarning" class="setting-message warning">{{ backgroundWarning }}</p>
          <p v-if="backgroundInfo" class="setting-message success">{{ backgroundInfo }}</p>
          <p v-if="backgroundError" class="setting-message error">{{ backgroundError }}</p>
        </div>
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

.image-setting-row {
  align-items: flex-start;
}

.background-picker {
  width: min(460px, 100%);
  display: grid;
  gap: 0.65rem;
}

.file-input {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
}

.background-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
}

.secondary-btn {
  padding: 0.64rem 0.9rem;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-card);
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  cursor: pointer;
  transition: border-color var(--transition-speed), box-shadow var(--transition-speed), transform var(--transition-speed);
}

.secondary-btn:hover,
.secondary-btn:focus-visible {
  border-color: var(--accent);
  box-shadow: 0 0 16px var(--glow-accent);
  transform: translateY(-1px);
  outline: none;
}

.secondary-btn.danger:hover,
.secondary-btn.danger:focus-visible {
  border-color: var(--critical);
  box-shadow: 0 0 16px var(--glow-critical);
}

.background-current {
  display: grid;
  gap: 0.2rem;
  padding: 0.65rem;
  border: 1px solid var(--border-color);
  border-radius: 12px;
  background: var(--metric-tile-bg);
}

.background-current span {
  color: var(--text-muted);
  font-size: 0.65rem;
}

.background-current strong {
  color: var(--text-primary);
  overflow-wrap: anywhere;
}

.background-preview {
  width: min(280px, 100%);
  aspect-ratio: 16 / 9;
  object-fit: cover;
  border-radius: 14px;
  border: 1px solid var(--border-color);
  box-shadow: 0 0 16px var(--glow-accent);
}

.setting-message {
  margin: 0;
  padding: 0.55rem 0.65rem;
  border-radius: 10px;
  font-family: var(--font-mono);
  font-size: 0.68rem;
  line-height: 1.45;
  border: 1px solid var(--border-color);
}

.setting-message.warning {
  color: var(--warning);
  border-color: color-mix(in srgb, var(--warning) 45%, var(--border-color));
  background: color-mix(in srgb, var(--warning) 9%, var(--bg-card));
}

.setting-message.success {
  color: var(--success);
  border-color: color-mix(in srgb, var(--success) 45%, var(--border-color));
  background: color-mix(in srgb, var(--success) 8%, var(--bg-card));
}

.setting-message.error {
  color: var(--critical);
  border-color: color-mix(in srgb, var(--critical) 45%, var(--border-color));
  background: color-mix(in srgb, var(--critical) 9%, var(--bg-card));
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
