const DB_NAME = 'crossmonitor-dashboard-background'
const DB_VERSION = 1
const STORE_NAME = 'background'
const ACTIVE_KEY = 'active'

export const MAX_BACKGROUND_IMAGE_BYTES = 2 * 1024 * 1024
export const RECOMMENDED_BACKGROUND_WIDTH = 1920
export const RECOMMENDED_BACKGROUND_HEIGHT = 1080

const ACCEPTED_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])
const ACCEPTED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']

export interface BackgroundImageMetadata {
  name: string
  type: string
  size: number
  width: number
  height: number
  updatedAt: number
}

interface BackgroundImageRecord {
  id: typeof ACTIVE_KEY
  blob: Blob
  metadata: BackgroundImageMetadata
}

export interface BackgroundImageValidationResult {
  valid: boolean
  errorKey?: 'tooLarge' | 'invalidFormat' | 'loadFailed' | 'storageUnsupported'
  warningKey?: 'lowResolution'
}

export function isBackgroundImageStorageSupported(): boolean {
  return typeof indexedDB !== 'undefined' && typeof Blob !== 'undefined'
}

export function validateBackgroundImageBasics(file: Pick<File, 'name' | 'type' | 'size'>): BackgroundImageValidationResult {
  const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'))
  if (!ACCEPTED_TYPES.has(file.type) || !ACCEPTED_EXTENSIONS.includes(extension)) {
    return { valid: false, errorKey: 'invalidFormat' }
  }
  if (file.size > MAX_BACKGROUND_IMAGE_BYTES) {
    return { valid: false, errorKey: 'tooLarge' }
  }
  return { valid: true }
}

export function validateBackgroundImageResolution(width: number, height: number): BackgroundImageValidationResult {
  if (width < RECOMMENDED_BACKGROUND_WIDTH || height < RECOMMENDED_BACKGROUND_HEIGHT) {
    return { valid: true, warningKey: 'lowResolution' }
  }
  return { valid: true }
}

export async function readImageDimensions(file: Blob): Promise<{ width: number; height: number }> {
  const url = URL.createObjectURL(file)
  try {
    const image = new Image()
    await new Promise<void>((resolve, reject) => {
      image.onload = () => resolve()
      image.onerror = () => reject(new Error('image-load-failed'))
      image.src = url
    })
    return { width: image.naturalWidth, height: image.naturalHeight }
  } finally {
    URL.revokeObjectURL(url)
  }
}

function openDatabase(): Promise<IDBDatabase> {
  if (!isBackgroundImageStorageSupported()) {
    return Promise.reject(new Error('indexeddb-unavailable'))
  }

  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION)
    request.onerror = () => reject(request.error ?? new Error('indexeddb-open-failed'))
    request.onsuccess = () => resolve(request.result)
    request.onupgradeneeded = () => {
      const db = request.result
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: 'id' })
      }
    }
  })
}

async function withStore<T>(mode: IDBTransactionMode, callback: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  const db = await openDatabase()
  try {
    return await new Promise<T>((resolve, reject) => {
      const transaction = db.transaction(STORE_NAME, mode)
      const store = transaction.objectStore(STORE_NAME)
      const request = callback(store)
      request.onerror = () => reject(request.error ?? new Error('indexeddb-request-failed'))
      request.onsuccess = () => resolve(request.result)
      transaction.onerror = () => reject(transaction.error ?? new Error('indexeddb-transaction-failed'))
    })
  } finally {
    db.close()
  }
}

export async function saveBackgroundImage(file: File): Promise<{ metadata: BackgroundImageMetadata; warningKey?: 'lowResolution' }> {
  const basics = validateBackgroundImageBasics(file)
  if (!basics.valid) throw new Error(basics.errorKey)

  let dimensions: { width: number; height: number }
  try {
    dimensions = await readImageDimensions(file)
  } catch {
    throw new Error('loadFailed')
  }

  const resolution = validateBackgroundImageResolution(dimensions.width, dimensions.height)
  const metadata: BackgroundImageMetadata = {
    name: file.name,
    type: file.type,
    size: file.size,
    width: dimensions.width,
    height: dimensions.height,
    updatedAt: Date.now()
  }

  const record: BackgroundImageRecord = {
    id: ACTIVE_KEY,
    blob: file.slice(0, file.size, file.type),
    metadata
  }

  await withStore('readwrite', store => store.put(record))
  return { metadata, warningKey: resolution.warningKey }
}

export async function replaceBackgroundImage(file: File): Promise<{ metadata: BackgroundImageMetadata; warningKey?: 'lowResolution' }> {
  await deleteBackgroundImage()
  return saveBackgroundImage(file)
}

export async function getBackgroundImage(): Promise<{ blob: Blob; metadata: BackgroundImageMetadata } | null> {
  const record = await withStore<BackgroundImageRecord | undefined>('readonly', store => store.get(ACTIVE_KEY))
  if (!record) return null
  return { blob: record.blob, metadata: record.metadata }
}

export async function getBackgroundImageMetadata(): Promise<BackgroundImageMetadata | null> {
  const record = await getBackgroundImage()
  return record?.metadata ?? null
}

export async function deleteBackgroundImage(): Promise<void> {
  await withStore('readwrite', store => store.delete(ACTIVE_KEY))
}
