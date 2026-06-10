import test from 'node:test'
import assert from 'node:assert/strict'

import {
  applyCardOrder,
  dashboardLayoutStorageKey,
  getDashboardLayout,
  resetDashboardLayout,
  saveDashboardLayout,
  validateCardOrder,
} from '../src/utils/dashboardLayoutStore.ts'

const nodes = [
  { id: 'alpha', name: 'Alpha' },
  { id: 'bravo', name: 'Bravo' },
  { id: 'charlie', name: 'Charlie' },
]

function installLocalStorage() {
  const storage = new Map()
  globalThis.localStorage = {
    getItem: key => storage.has(key) ? storage.get(key) : null,
    setItem: (key, value) => storage.set(key, String(value)),
    removeItem: key => storage.delete(key),
    clear: () => storage.clear(),
  }
}

test.beforeEach(() => {
  installLocalStorage()
})

test('empty localStorage returns default layout', () => {
  assert.deepEqual(getDashboardLayout(), [])
})

test('saved order is applied to matching nodes', () => {
  const ordered = applyCardOrder(nodes, ['charlie', 'alpha', 'bravo'])

  assert.deepEqual(ordered.map(node => node.id), ['charlie', 'alpha', 'bravo'])
})

test('new nodes append after saved order', () => {
  const ordered = applyCardOrder(nodes, ['bravo'])

  assert.deepEqual(ordered.map(node => node.id), ['bravo', 'alpha', 'charlie'])
})

test('removed nodes and duplicates are ignored', () => {
  assert.deepEqual(validateCardOrder(['missing', 'bravo', 'bravo', 'alpha'], nodes), ['bravo', 'alpha'])
})

test('invalid localStorage is safe', () => {
  localStorage.setItem(dashboardLayoutStorageKey, '{bad-json')

  assert.deepEqual(getDashboardLayout(), [])
})

test('save stores only string ids once', () => {
  saveDashboardLayout(['bravo', 'alpha', 'bravo', 10])

  assert.deepEqual(JSON.parse(localStorage.getItem(dashboardLayoutStorageKey)), ['bravo', 'alpha'])
})

test('reset removes only dashboard layout key', () => {
  localStorage.setItem(dashboardLayoutStorageKey, '["alpha"]')
  localStorage.setItem('crossmonitor-dashboard-visual-settings', '{"theme":"glass-blue"}')

  resetDashboardLayout()

  assert.equal(localStorage.getItem(dashboardLayoutStorageKey), null)
  assert.equal(localStorage.getItem('crossmonitor-dashboard-visual-settings'), '{"theme":"glass-blue"}')
})
