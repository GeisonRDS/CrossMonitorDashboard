import { ref } from 'vue'
import { getPublicConfig } from '../api/dashboard'
import { getBackgroundImage } from '../utils/backgroundImageStore'

export const VISUAL_SETTINGS_KEY = 'crossmonitor-dashboard-visual-settings'

export type MetricChartType = 'line-glow' | 'radial-gauge' | 'bar-pulse'
export type MetricKey = 'cpu' | 'memory' | 'disk' | 'temperature' | 'network'
export type BackgroundEffect = 'none' | 'penguin' | 'snake' | 'solar-system'
export type BackgroundEffectIntensity = 'low' | 'medium' | 'high'

export interface VisualSettings {
  theme: string
  refreshSeconds: number
  cardSize: 'compact' | 'normal' | 'wide' | 'detailed'
  animations: {
    enabled: boolean
    intensity: 'low' | 'medium' | 'high'
  }
  background: {
    type: 'gradient' | 'solid' | 'image'
    imagePath: string
    opacity: number
    blur: number
    overlay: number
  }
  metricCharts: Record<MetricKey, MetricChartType>
  backgroundEffect: BackgroundEffect
  backgroundEffectIntensity: BackgroundEffectIntensity
}

const defaultSettings: VisualSettings = {
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
  },
  backgroundEffect: 'none',
  backgroundEffectIntensity: 'medium'
}

const currentTheme = ref(defaultSettings.theme)
const availableThemes = ref<string[]>([])
const visualSettings = ref<VisualSettings>({ ...defaultSettings })
let activeBackgroundObjectUrl = ''

const defaultThemesList = [
  'glass-blue', 'neon-green', 'cyber-red', 'terminal-green', 'pixel-platformer',
  'terminal-mono', 'terminal-blue', 'terminal-red', 'terminal-green-matte',
  'material-slate', 'material-graphite', 'material-ocean', 'material-forest',
  'hacker-prompt', 'code-editor', 'black-white'
]

function normalizeSettings(value: Partial<VisualSettings> | null | undefined): VisualSettings {
  const validEffects: BackgroundEffect[] = ['none', 'penguin', 'snake', 'solar-system']
  const validIntensities: BackgroundEffectIntensity[] = ['low', 'medium', 'high']
  return {
    ...defaultSettings,
    ...value,
    animations: {
      ...defaultSettings.animations,
      ...value?.animations
    },
    background: {
      ...defaultSettings.background,
      ...value?.background,
      imagePath: ''
    },
    metricCharts: {
      ...defaultSettings.metricCharts,
      ...value?.metricCharts
    },
    backgroundEffect: validEffects.includes(value?.backgroundEffect as BackgroundEffect)
      ? (value!.backgroundEffect as BackgroundEffect)
      : defaultSettings.backgroundEffect,
    backgroundEffectIntensity: validIntensities.includes(value?.backgroundEffectIntensity as BackgroundEffectIntensity)
      ? (value!.backgroundEffectIntensity as BackgroundEffectIntensity)
      : defaultSettings.backgroundEffectIntensity
  }
}

function setBackgroundObjectUrl(url: string) {
  if (activeBackgroundObjectUrl && activeBackgroundObjectUrl !== url) {
    URL.revokeObjectURL(activeBackgroundObjectUrl)
  }
  activeBackgroundObjectUrl = url
  document.documentElement.style.setProperty('--custom-bg-image', url ? `url(${url})` : 'none')
}

async function loadStoredBackgroundImage(settings: VisualSettings) {
  if (settings.background.type !== 'image') {
    setBackgroundObjectUrl('')
    return
  }

  try {
    const stored = await getBackgroundImage()
    if (!stored) {
      setBackgroundObjectUrl('')
      return
    }
    setBackgroundObjectUrl(URL.createObjectURL(stored.blob))
  } catch {
    setBackgroundObjectUrl('')
  }
}

function readSavedSettings(): Partial<VisualSettings> | null {
  try {
    const saved = localStorage.getItem(VISUAL_SETTINGS_KEY)
    return saved ? JSON.parse(saved) : null
  } catch {
    return null
  }
}

function writeSettings(settings: VisualSettings) {
  localStorage.setItem(VISUAL_SETTINGS_KEY, JSON.stringify(settings))
}

function updateDocument(settings: VisualSettings) {
  document.documentElement.setAttribute('data-theme', settings.theme)
  document.documentElement.setAttribute('data-card-size', settings.cardSize)
  document.documentElement.setAttribute('data-animations', settings.animations.enabled ? 'on' : 'off')
  document.documentElement.setAttribute('data-animation-intensity', settings.animations.intensity)
  document.documentElement.style.setProperty('--custom-bg-opacity', String(settings.background.opacity))
  document.documentElement.style.setProperty('--custom-bg-blur', `${settings.background.blur}px`)
  document.documentElement.style.setProperty('--custom-bg-overlay', String(settings.background.overlay))
  void loadStoredBackgroundImage(settings)
}

export function useTheme() {
  function applySettings(settings: VisualSettings, persist = true) {
    visualSettings.value = normalizeSettings(settings)
    currentTheme.value = visualSettings.value.theme
    updateDocument(visualSettings.value)
    if (persist) writeSettings(visualSettings.value)
  }

  function applyTheme(theme: string) {
    applySettings({ ...visualSettings.value, theme })
  }

  async function loadTheme() {
    let publicSettings: Partial<VisualSettings> = {}
    try {
      const config = await getPublicConfig()
      publicSettings = normalizeSettings(config as Partial<VisualSettings>)
    } catch {
      publicSettings = defaultSettings
    }

    const savedSettings = readSavedSettings()
    applySettings(normalizeSettings({ ...publicSettings, ...savedSettings }), false)

    if (availableThemes.value.length === 0) {
      availableThemes.value = defaultThemesList
    }
  }

  return {
    currentTheme,
    availableThemes,
    visualSettings,
    applyTheme,
    applySettings,
    loadTheme
  }
}
