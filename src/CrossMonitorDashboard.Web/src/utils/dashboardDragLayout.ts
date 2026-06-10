export interface PointerPosition {
  clientX: number
  clientY: number
}

export interface RectLike {
  left: number
  top: number
  width: number
  height: number
}

export interface DragOffset {
  x: number
  y: number
}

export function didPassDragThreshold(start: PointerPosition, current: PointerPosition, thresholdPx: number) {
  return Math.hypot(current.clientX - start.clientX, current.clientY - start.clientY) >= thresholdPx
}

export function calculateDragOffset(pointer: PointerPosition, rect: RectLike): DragOffset {
  return {
    x: pointer.clientX - rect.left,
    y: pointer.clientY - rect.top
  }
}

export function removeDraggedCard(order: string[], draggedId: string) {
  return order.filter(id => id !== draggedId)
}

export function insertDraggedCard(order: string[], draggedId: string, index: number) {
  const withoutDragged = removeDraggedCard(order, draggedId)
  const safeIndex = Math.max(0, Math.min(index, withoutDragged.length))
  return [...withoutDragged.slice(0, safeIndex), draggedId, ...withoutDragged.slice(safeIndex)]
}

export function calculateDropIndex(pointer: PointerPosition, rects: RectLike[]) {
  const sortedRects = [...rects].sort((a, b) => Math.abs(a.top - b.top) > 8 ? a.top - b.top : a.left - b.left)

  for (let index = 0; index < sortedRects.length; index += 1) {
    const rect = sortedRects[index]
    const centerY = rect.top + rect.height / 2
    const centerX = rect.left + rect.width / 2

    if (pointer.clientY < centerY) return index
    if (pointer.clientY <= rect.top + rect.height && pointer.clientX < centerX) return index
  }

  return sortedRects.length
}
