import assert from 'node:assert/strict'
import { test } from 'node:test'

import { readPublicEnvironment } from '../src/config/publicEnvironment.ts'

test('uses the versioned relative API path by default', () => {
  assert.deepEqual(readPublicEnvironment({}), { apiBaseUrl: '/api/v1' })
})

test('normalizes a configured API URL', () => {
  assert.deepEqual(
    readPublicEnvironment({ VITE_API_BASE_URL: 'https://api.example.test/api/v1/' }),
    { apiBaseUrl: 'https://api.example.test/api/v1' },
  )
})

test('rejects values that could expose credentials', () => {
  assert.throws(
    () =>
      readPublicEnvironment({
        VITE_API_BASE_URL: 'https://user:password@example.test/api/v1',
      }),
    /must not contain credentials/,
  )
})

test('rejects protocol-relative URLs', () => {
  assert.throws(
    () => readPublicEnvironment({ VITE_API_BASE_URL: '//example.test/api/v1' }),
    /root-relative path or an absolute HTTP\(S\) URL/,
  )
})
