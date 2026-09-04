import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

function renderAuthRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('authentication boundary', () => {
  it('redirects an anonymous visitor away from protected Teacher content', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response(null, { status: 401 }))
    renderAuthRoute('/students')

    expect(await screen.findByRole('heading', { name: 'Prijava za učitelje' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'PLUS 5' }).querySelector('img')).toHaveAttribute('alt', 'PLUS 5')
    expect(screen.queryByRole('heading', { name: 'Učenici' })).not.toBeInTheDocument()
  })

  it('fails closed with an explicit retry state when session verification is unavailable', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error('network unavailable'))
    renderAuthRoute('/students')

    expect(
      await screen.findByRole('heading', { name: 'Prijavu trenutačno nije moguće provjeriti' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pokušaj ponovno' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Učenici' })).not.toBeInTheDocument()
  })

  it('renders explicit expired-session and access-denied states', async () => {
    const expired = renderAuthRoute('/auth/session-expired')
    expect(await screen.findByRole('heading', { name: 'Sesija je istekla' })).toBeInTheDocument()
    expired.unmount()

    renderAuthRoute('/auth/access-denied')
    expect(await screen.findByRole('heading', { name: 'Pristup nije dopušten' })).toBeInTheDocument()
  })

  it('submits login with cookie credentials and a CSRF token without browser storage', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ email: 'teacher@example.test', accountType: 'Teacher', expiresAtUtc: '2026-08-25T12:00:00Z' }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    const localStorageSpy = vi.spyOn(Storage.prototype, 'setItem')
    renderAuthRoute('/auth/login')

    fireEvent.change(await screen.findByLabelText('E-mail'), { target: { value: 'teacher@example.test' } })
    fireEvent.change(screen.getByLabelText('Lozinka'), { target: { value: 'StrongPassword42!' } })
    fireEvent.click(screen.getByRole('button', { name: 'Prijavi se' }))

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Radni stol' })).toBeInTheDocument())
    const loginCall = vi.mocked(fetch).mock.calls.find(([input]) => String(input).endsWith('/auth/login'))
    expect(loginCall?.[1]).toMatchObject({ credentials: 'include', method: 'POST' })
    expect(new Headers(loginCall?.[1]?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token')
    expect(localStorageSpy).not.toHaveBeenCalled()
  })
})
