export const MAX_VISIBLE_POINTS = 10

export function alignRight(points: number[]): Array<number | null> {
  const last = points.slice(-MAX_VISIBLE_POINTS).map(v => Math.max(0, Math.min(100, Number(v) || 0)))
  return [...Array(Math.max(0, MAX_VISIBLE_POINTS - last.length)).fill(null), ...last]
}
