export const MAX_VISIBLE_POINTS = 10

export function alignRight(points: number[], max = 100): Array<number | null> {
  const safeMax = Math.max(0, Number(max) || 0)
  const last = points.slice(-MAX_VISIBLE_POINTS).map(v => Math.max(0, Math.min(safeMax, Number(v) || 0)))
  return [...Array(Math.max(0, MAX_VISIBLE_POINTS - last.length)).fill(null), ...last]
}
