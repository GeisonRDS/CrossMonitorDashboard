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
  const rows: { startIndex: number; top: number; bottom: number; rects: RectLike[] }[] = []

  sortedRects.forEach((rect, index) => {
    const bottom = rect.top + rect.height
    const currentRow = rows.at(-1)
    if (!currentRow || Math.abs(rect.top - currentRow.top) > 10) {
      rows.push({ startIndex: index, top: rect.top, bottom, rects: [rect] })
      return
    }

    currentRow.bottom = Math.max(currentRow.bottom, bottom)
    currentRow.rects.push(rect)
  })

  for (const row of rows) {
    if (pointer.clientY < row.top) return row.startIndex

    if (pointer.clientY <= row.bottom) {
      for (let rowIndex = 0; rowIndex < row.rects.length; rowIndex += 1) {
        const rect = row.rects[rowIndex]
        const centerX = rect.left + rect.width / 2
        if (pointer.clientX < centerX) return row.startIndex + rowIndex
      }

      return row.startIndex + row.rects.length
    }
  }

  return sortedRects.length
}
