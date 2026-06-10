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
import {
  calculateDragOffset,
  calculateDropIndex,
  didPassDragThreshold,
  insertDraggedCard,
  removeDraggedCard,
} from '../src/utils/dashboardDragLayout.ts'

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

test('legacy array layout is still readable', () => {
  localStorage.setItem(dashboardLayoutStorageKey, '["charlie","alpha"]')

  assert.deepEqual(getDashboardLayout(), ['charlie', 'alpha'])
})

test('save stores versioned card order with only string ids once', () => {
  saveDashboardLayout(['bravo', 'alpha', 'bravo', 10])

  assert.deepEqual(JSON.parse(localStorage.getItem(dashboardLayoutStorageKey)), {
    version: 1,
    cardOrder: ['bravo', 'alpha']
  })
})

test('new order is saved only when saveDashboardLayout is called', () => {
  const intermediateOrder = ['bravo', 'alpha', 'charlie']
  assert.equal(localStorage.getItem(dashboardLayoutStorageKey), null)

  saveDashboardLayout(intermediateOrder)

  assert.deepEqual(getDashboardLayout(), intermediateOrder)
})

test('polling with a refreshed node list preserves saved order', () => {
  const refreshedNodes = nodes.map(node => ({ ...node, cpuUsagePercent: Math.random() }))
  saveDashboardLayout(['charlie', 'alpha'])

  const ordered = applyCardOrder(refreshedNodes, getDashboardLayout())

  assert.deepEqual(ordered.map(node => node.id), ['charlie', 'alpha', 'bravo'])
})

test('reset removes only dashboard layout key', () => {
  localStorage.setItem(dashboardLayoutStorageKey, '["alpha"]')
  localStorage.setItem('crossmonitor-dashboard-visual-settings', '{"theme":"glass-blue"}')

  resetDashboardLayout()

  assert.equal(localStorage.getItem(dashboardLayoutStorageKey), null)
  assert.equal(localStorage.getItem('crossmonitor-dashboard-visual-settings'), '{"theme":"glass-blue"}')
})

test('drag threshold separates click from drag', () => {
  assert.equal(didPassDragThreshold({ clientX: 10, clientY: 10 }, { clientX: 13, clientY: 14 }, 7), false)
  assert.equal(didPassDragThreshold({ clientX: 10, clientY: 10 }, { clientX: 18, clientY: 10 }, 7), true)
})

test('drag offset uses client coordinates and rect position', () => {
  assert.deepEqual(calculateDragOffset({ clientX: 360, clientY: 140 }, { left: 300, top: 100, width: 240, height: 120 }), {
    x: 60,
    y: 40,
  })
})

test('dragged card is removed from temporary grid order', () => {
  assert.deepEqual(removeDraggedCard(['alpha', 'bravo', 'charlie'], 'bravo'), ['alpha', 'charlie'])
})

test('dragged card is inserted at requested index', () => {
  assert.deepEqual(insertDraggedCard(['alpha', 'charlie'], 'bravo', 1), ['alpha', 'bravo', 'charlie'])
})

test('drop before first card inserts at the beginning', () => {
  const rects = [
    { left: 100, top: 100, width: 200, height: 100 },
    { left: 320, top: 100, width: 200, height: 100 },
  ]

  assert.equal(calculateDropIndex({ clientX: 80, clientY: 90 }, rects), 0)
})

test('drop after last card inserts at the end', () => {
  const rects = [
    { left: 100, top: 100, width: 200, height: 100 },
    { left: 320, top: 100, width: 200, height: 100 },
  ]

  assert.equal(calculateDropIndex({ clientX: 560, clientY: 180 }, rects), 2)
})

test('drop in middle of responsive grid inserts at visual index', () => {
  const rects = [
    { left: 100, top: 100, width: 200, height: 100 },
    { left: 320, top: 100, width: 200, height: 100 },
    { left: 100, top: 220, width: 200, height: 100 },
  ]

  assert.equal(calculateDropIndex({ clientX: 310, clientY: 150 }, rects), 1)
})

test('pointer move helpers do not save localStorage', () => {
  removeDraggedCard(['alpha', 'bravo'], 'alpha')
  calculateDropIndex({ clientX: 100, clientY: 100 }, [{ left: 0, top: 0, width: 200, height: 120 }])

  assert.equal(localStorage.getItem(dashboardLayoutStorageKey), null)
})
