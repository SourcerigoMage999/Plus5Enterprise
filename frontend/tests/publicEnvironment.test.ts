import { describe, expect, it } from 'vitest'

import { readPublicEnvironment } from '../src/config/publicEnvironment.ts'

describe('public environment', () => {
  it('uses the versioned relative API path by default', () => {
    expect(readPublicEnvironment({})).toEqual({ apiBaseUrl: '/api/v1' })
  })

  it('normalizes a configured API URL', () => {
    expect(
      readPublicEnvironment({ VITE_API_BASE_URL: 'https://api.example.test/api/v1/' }),
    ).toEqual({ apiBaseUrl: 'https://api.example.test/api/v1' })
  })

  it('rejects values that could expose credentials', () => {
    expect(() =>
      readPublicEnvironment({
        VITE_API_BASE_URL: 'https://user:password@example.test/api/v1',
      }),
    ).toThrow(/must not contain credentials/)
  })

  it('rejects protocol-relative URLs', () => {
    expect(() => readPublicEnvironment({ VITE_API_BASE_URL: '//example.test/api/v1' })).toThrow(
      /root-relative path or an absolute HTTP\(S\) URL/,
    )
  })
})
