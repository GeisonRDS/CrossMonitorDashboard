import { ref, computed } from 'vue'
import { en } from '../locales/en'
import { pt } from '../locales/pt'

export type Locale = 'en' | 'pt'
type Messages = typeof en

const LOCALE_KEY = 'crossmonitor-dashboard-language'

const messages: Record<Locale, Messages> = { en, pt }

function detectLocale(): Locale {
  const saved = localStorage.getItem(LOCALE_KEY) as Locale | null
  if (saved === 'en' || saved === 'pt') return saved
  const navLang = navigator.language?.toLowerCase() || ''
  if (navLang.startsWith('pt')) return 'pt'
  return 'en'
}

const currentLocale = ref<Locale>(detectLocale())

function setLocale(locale: Locale) {
  currentLocale.value = locale
  localStorage.setItem(LOCALE_KEY, locale)
  document.documentElement.setAttribute('lang', locale === 'pt' ? 'pt-BR' : 'en')
}

const t = computed(() => messages[currentLocale.value])

export function useI18n() {
  function translate(key: string, params?: Record<string, string | number>): string {
    const keys = key.split('.')
    let value: any = t.value
    for (const k of keys) {
      value = value?.[k]
    }
    if (value == null) return key
    if (params) {
      return Object.entries(params).reduce((acc, [k, v]) => acc.replace(`{${k}}`, String(v)), value as string)
    }
    return String(value)
  }

  function availableLocales(): { value: Locale; label: string }[] {
    return [
      { value: 'en', label: 'English' },
      { value: 'pt', label: 'Português' },
    ]
  }

  return {
    currentLocale,
    setLocale,
    translate,
    t,
    availableLocales,
  }
}
