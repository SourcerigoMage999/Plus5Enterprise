import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'
import { findNavigationItem, navigationItems } from '../src/app/navigation.ts'

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-10-25T16:00:00Z',
}

const dashboard = {
  timeZoneId: 'Europe/Zagreb',
  today: '2026-10-25',
  generatedAtUtc: '2026-10-25T08:00:00Z',
  todaySessions: [],
  nextSession: null,
  students: { totalCount: 0, activeCount: 0, onHoldCount: 0, inactiveCount: 0 },
}

function json(value: unknown) {
  return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

function mockDashboardRoute() {
  vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(dashboard))
}

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('application shell', () => {
  it('renders every documented primary navigation item in canonical order', async () => {
    mockDashboardRoute()
    renderRoute('/')

    const navigation = await screen.findByRole('navigation', { name: 'Glavna navigacija' })
    const links = within(navigation).getAllByRole('link')

    expect(links).toHaveLength(navigationItems.length)
    expect(links.map((link) => link.textContent?.replace(/^\d{2}/, ''))).toEqual(
      navigationItems.map((item) => item.label),
    )
  })

  it('marks the current route and renders the material library feature', async () => {
    renderRoute('/materials')

    expect(await screen.findByRole('link', { name: /Materijali/ })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('heading', { level: 1, name: '4.1 Biblioteka materijala' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Novi materijal' })).toHaveAttribute('href', '/materials/import')
  })

  it('provides a skip link and a named main navigation landmark', async () => {
    mockDashboardRoute()
    renderRoute('/')

    const brand = await screen.findByRole('link', { name: 'PLUS 5 — Radni stol' })
    expect(brand.querySelector('img')).toHaveAttribute('alt', 'PLUS 5')
    expect(await screen.findByRole('link', { name: 'Preskoči na glavni sadržaj' })).toHaveAttribute(
      'href',
      '#main-content',
    )
    expect(screen.getByRole('main')).toHaveAttribute('id', 'main-content')
    expect(screen.getByRole('navigation')).toHaveAccessibleName('Glavna navigacija')
  })

  it('renders an explicit not-found state with a safe return route', async () => {
    renderRoute('/not-a-defined-module')

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Stranica nije pronađena' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Povratak na Radni stol' })).toHaveAttribute(
      'href',
      '/',
    )
  })
})

describe('navigation contract', () => {
  it('uses unique absolute paths and resolves a trailing slash', () => {
    const paths = navigationItems.map((item) => item.path)

    expect(new Set(paths).size).toBe(paths.length)
    expect(paths.every((path) => path.startsWith('/'))).toBe(true)
    expect(findNavigationItem('/students/')?.id).toBe('students')
  })
})
