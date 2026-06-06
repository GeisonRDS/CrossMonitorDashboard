import { ref } from 'vue'

const currentTheme = ref('glass-blue')
const availableThemes = ref<string[]>([])

export function useTheme() {
  function applyTheme(theme: string) {
    document.documentElement.setAttribute('data-theme', theme)
    currentTheme.value = theme
    localStorage.setItem('dashboard-theme', theme)
  }

  function loadTheme() {
    const saved = localStorage.getItem('dashboard-theme')
    if (saved && availableThemes.value.includes(saved)) {
      applyTheme(saved)
    } else {
      applyTheme(currentTheme.value)
    }
  }

  return {
    currentTheme,
    availableThemes,
    applyTheme,
    loadTheme
  }
}
