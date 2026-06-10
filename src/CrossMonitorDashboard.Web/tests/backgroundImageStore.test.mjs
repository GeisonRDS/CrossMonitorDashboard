import test from 'node:test'
import assert from 'node:assert/strict'

import {
  MAX_BACKGROUND_IMAGE_BYTES,
  validateBackgroundImageBasics,
  validateBackgroundImageResolution,
} from '../src/utils/backgroundImageStore.ts'

function file(name, type, size = 1024) {
  return { name, type, size }
}

test('JPG, PNG and WebP backgrounds are accepted', () => {
  assert.equal(validateBackgroundImageBasics(file('photo.jpg', 'image/jpeg')).valid, true)
  assert.equal(validateBackgroundImageBasics(file('photo.jpeg', 'image/jpeg')).valid, true)
  assert.equal(validateBackgroundImageBasics(file('photo.png', 'image/png')).valid, true)
  assert.equal(validateBackgroundImageBasics(file('photo.webp', 'image/webp')).valid, true)
})

test('SVG and non-image backgrounds are rejected', () => {
  assert.equal(validateBackgroundImageBasics(file('icon.svg', 'image/svg+xml')).errorKey, 'invalidFormat')
  assert.equal(validateBackgroundImageBasics(file('notes.txt', 'text/plain')).errorKey, 'invalidFormat')
})

test('backgrounds larger than 2 MB are rejected', () => {
  assert.equal(validateBackgroundImageBasics(file('large.webp', 'image/webp', MAX_BACKGROUND_IMAGE_BYTES + 1)).errorKey, 'tooLarge')
})

test('low resolution background returns warning without fatal error', () => {
  const result = validateBackgroundImageResolution(1280, 720)

  assert.equal(result.valid, true)
  assert.equal(result.warningKey, 'lowResolution')
})

test('recommended resolution background returns no warning', () => {
  const result = validateBackgroundImageResolution(1920, 1080)

  assert.equal(result.valid, true)
  assert.equal(result.warningKey, undefined)
})
