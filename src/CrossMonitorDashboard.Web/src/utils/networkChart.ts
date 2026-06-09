export const NETWORK_VISIBLE_POINTS = 10
const MIN_VISUAL_MAX_MBPS = 0.10
const MIN_VISIBLE_PERCENT = 4
const OUTLIER_MEDIAN_MULTIPLIER = 4

export function sanitizeNetworkRateMBps(value: number | null | undefined): number {
  if (value == null || !Number.isFinite(value) || value < 0) return 0
  return value
}

export function formatNetworkRateMBps(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return '--'
  const rate = Math.max(0, value)
  if (rate === 0) return '0.00 MB/s'
  if (rate < 0.01) return '<0.01 MB/s'
  if (rate < 100) return `${rate.toFixed(2)} MB/s`
  return `${rate.toFixed(1)} MB/s`
}

export function networkVisualMaxMBps(historyMBps: number[]): number {
  const values = historyMBps
    .slice(-NETWORK_VISIBLE_POINTS)
    .map(sanitizeNetworkRateMBps)
    .filter(value => value > 0)
    .sort((a, b) => a - b)

  if (values.length === 0) return MIN_VISUAL_MAX_MBPS

  const max = values.at(-1) ?? 0
  const median = values[Math.floor(values.length / 2)] ?? max
  const outlierProtectedMax = Math.max(MIN_VISUAL_MAX_MBPS, Math.min(max * 1.25, median * OUTLIER_MEDIAN_MULTIPLIER))

  return Math.max(MIN_VISUAL_MAX_MBPS, outlierProtectedMax)
}

export function normalizeNetworkChartValue(valueMBps: number, historyMBps: number[]): number {
  const value = sanitizeNetworkRateMBps(valueMBps)
  if (value === 0) return 0

  const visualMax = networkVisualMaxMBps(historyMBps)
  const normalized = Math.min(100, (value / visualMax) * 100)

  return normalized > 0 && normalized < MIN_VISIBLE_PERCENT ? MIN_VISIBLE_PERCENT : normalized
}

export function networkChartSeries(historyMBps: number[]): number[] {
  const window = historyMBps.slice(-NETWORK_VISIBLE_POINTS).map(sanitizeNetworkRateMBps)
  return window.map(value => normalizeNetworkChartValue(value, window))
}
