import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'
import { findNavigationItem, navigationItems } from '../src/app/navigation.ts'

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('application shell', () => {
  it('renders every documented primary navigation item in canonical order', async () => {
    renderRoute('/')

    const navigation = await screen.findByRole('navigation', { name: 'Glavna navigacija' })
    const links = within(navigation).getAllByRole('link')

    expect(links).toHaveLength(navigationItems.length)
    expect(links.map((link) => link.textContent?.replace(/^\d{2}/, ''))).toEqual(
      navigationItems.map((item) => item.label),
    )
  })

  it('marks the current route and renders its neutral foundation state', async () => {
    renderRoute('/schedule')

    expect(await screen.findByRole('link', { name: /Raspored/ })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('heading', { level: 1, name: 'Raspored' })).toBeInTheDocument()
    expect(screen.getByText(/Bez lažnih podataka/)).toBeInTheDocument()
  })

  it('provides a skip link and a named main navigation landmark', async () => {
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
