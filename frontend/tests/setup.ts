import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
import { beforeEach, vi } from 'vitest'

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockImplementation(() =>
      Promise.resolve(new Response(
        JSON.stringify({
          email: 'teacher@example.test',
          accountType: 'Teacher',
          expiresAtUtc: '2026-08-25T12:00:00Z',
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      )),
    ),
  )
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})
