import { ref } from 'vue'
import { getPublicConfig } from '../api/dashboard'

export const VISUAL_SETTINGS_KEY = 'crossmonitor-dashboard-visual-settings'

export type MetricChartType = 'line-glow' | 'radial-gauge' | 'bar-pulse'
export type MetricKey = 'cpu' | 'memory' | 'disk' | 'temperature' | 'network'

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
  }
}

const currentTheme = ref(defaultSettings.theme)
const availableThemes = ref<string[]>([])
const visualSettings = ref<VisualSettings>({ ...defaultSettings })

function normalizeSettings(value: Partial<VisualSettings> | null | undefined): VisualSettings {
  return {
    ...defaultSettings,
    ...value,
    animations: {
      ...defaultSettings.animations,
      ...value?.animations
    },
    background: {
      ...defaultSettings.background,
      ...value?.background
    },
    metricCharts: {
      ...defaultSettings.metricCharts,
      ...value?.metricCharts
    }
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
