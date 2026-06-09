import test from 'node:test'
import assert from 'node:assert/strict'

import {
  formatNetworkRateMBps,
  networkChartSeries,
  networkVisualMaxMBps,
  normalizeNetworkChartValue,
} from '../src/utils/networkChart.ts'

test('low network history generates visible proportional movement', () => {
  const visual = networkChartSeries([0.01, 0.02, 0.05, 0.10])

  assert.equal(visual.length, 4)
  assert.ok(visual.every(value => value > 0))
  assert.ok(visual[0] < visual[1])
  assert.ok(visual[1] < visual[2])
  assert.ok(visual[2] < visual[3])
})

test('zero network rate remains zero visually', () => {
  assert.equal(normalizeNetworkChartValue(0, [0, 0, 0]), 0)
})

test('very small positive network rate gets minimum visual movement', () => {
  const visual = normalizeNetworkChartValue(0.001, [0.001])

  assert.ok(visual >= 4)
  assert.equal(formatNetworkRateMBps(0.001), '<0.01 MB/s')
})

test('isolated outlier saturates without flattening nearby low values', () => {
  const visual = networkChartSeries([0.05, 0.06, 0.07, 20.00, 0.06])

  assert.equal(visual[3], 100)
  assert.ok(visual[0] >= 15)
  assert.ok(visual[1] > visual[0])
  assert.ok(visual[2] > visual[1])
  assert.ok(visual[4] >= 15)
})

test('visual max is dynamic and not a fixed high scale', () => {
  assert.ok(networkVisualMaxMBps([0.01, 0.02, 0.05, 0.10]) < 1)
  assert.ok(networkVisualMaxMBps([0.05, 0.06, 0.07, 20.00, 0.06]) < 1)
})

test('radial gauge percentage uses dynamic visual max', () => {
  const visual = normalizeNetworkChartValue(0.08, [0.02, 0.05, 0.08])

  assert.ok(visual > 50)
  assert.ok(visual <= 100)
})

test('network rate formatting is stable', () => {
  assert.equal(formatNetworkRateMBps(0), '0.00 MB/s')
  assert.equal(formatNetworkRateMBps(0.004), '<0.01 MB/s')
  assert.equal(formatNetworkRateMBps(0.08), '0.08 MB/s')
  assert.equal(formatNetworkRateMBps(1.25), '1.25 MB/s')
  assert.equal(formatNetworkRateMBps(125.7), '125.7 MB/s')
})
