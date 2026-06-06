export interface BackgroundOption {
  id: string
  name: string
  namePt: string
  path: string
}

export const availableBackgrounds: BackgroundOption[] = [
  { id: 'none', name: 'None (use gradient)', namePt: 'Nenhum (usar gradiente)', path: '' },
  { id: 'theme-gradient', name: 'Theme gradient type', namePt: 'Tipo gradiente do tema', path: '' },
  { id: 'dark-grid', name: 'Dark Grid', namePt: 'Grade Escura', path: '/backgrounds/dark-grid.jpg' },
  { id: 'dark-blue', name: 'Dark Blue', namePt: 'Azul Escuro', path: '/backgrounds/default-dark.jpg' },
  { id: 'ocean', name: 'Ocean', namePt: 'Oceano', path: '/backgrounds/default-blue.jpg' },
]

export function getBackgroundPath(id: string): string {
  const found = availableBackgrounds.find(b => b.id === id)
  return found?.path ?? ''
}

export function getBackgroundIdByPath(path: string): string {
  const found = availableBackgrounds.find(b => b.path === path)
  return found?.id ?? 'none'
}

export function getBackgroundLabel(id: string, locale: 'en' | 'pt'): string {
  const found = availableBackgrounds.find(b => b.id === id)
  if (!found) return id
  return locale === 'pt' ? found.namePt : found.name
}
