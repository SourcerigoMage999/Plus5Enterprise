import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-10-01T12:00:00Z',
}

const material = {
  id: 'material-1',
  versionId: 'version-1',
  title: 'Present Perfect worksheet',
  description: 'Practice material',
  materialTypeCode: 'WORKSHEET',
  subject: 'Grammar',
  program: { id: 'program-1', name: 'English 8', code: null },
  schoolGrade: { id: 'grade-1', name: '8. razred', code: '8R' },
  proficiencyLevel: { id: 'level-1', name: 'B1', code: 'B1' },
  fileFormat: 'pdf',
  addedAtUtc: '2026-09-30T08:00:00Z',
  isOwner: true,
  shareAccess: null,
  tags: ['present perfect'],
}

const page = {
  items: [material],
  page: 1,
  pageSize: 24,
  totalCount: 1,
  totalPages: 1,
}

const overview = {
  subjects: ['Grammar'],
  programs: [{ id: 'program-1', name: 'English 8', code: null }],
  schoolGrades: [{ id: 'grade-1', name: '8. razred', code: '8R' }],
  materialTypes: ['WORKSHEET'],
  tags: ['present perfect'],
  materialTypeCounts: [{ code: 'WORKSHEET', count: 1 }],
  recentlyAdded: [{ id: 'material-1', title: 'Present Perfect worksheet', addedAtUtc: '2026-09-30T08:00:00Z' }],
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderMaterials(initialEntry = '/materials') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('material library', () => {
  it('renders actual owner materials, facets and honest future-action boundaries', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()

    expect(await screen.findByRole('heading', { level: 1, name: '4.1 Biblioteka materijala' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: material.title })).toBeInTheDocument()
    expect(screen.getByText('English 8 · 8R')).toBeInTheDocument()
    expect(screen.getAllByText('present perfect')).toHaveLength(2)
    expect(within(screen.getByRole('complementary', { name: 'Filtri i pregled materijala' })).getAllByText('Radni list')).toHaveLength(2)
    expect(screen.getByRole('button', { name: 'Novi materijal' })).toBeDisabled()
    expect(screen.getByRole('button', { name: `Akcije za ${material.title}` })).toBeDisabled()
  })

  it('switches to server-authorized shared scope and shows its empty state', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))
      .mockResolvedValueOnce(json({ ...page, items: [], totalCount: 0, totalPages: 0 }))
      .mockResolvedValueOnce(json({ ...overview, materialTypeCounts: [], recentlyAdded: [] }))

    renderMaterials()
    await screen.findByRole('heading', { name: material.title })
    fireEvent.click(screen.getByRole('tab', { name: 'Dijeljeni sa mnom' }))

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('/materials?page=1&pageSize=24&ownership=2&sort=1'))).toBe(true))
    expect(await screen.findByRole('heading', { name: 'Nema materijala dijeljenih s vama' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Dijeljeni sa mnom' })).toHaveAttribute('aria-selected', 'true')
  })

  it('keeps structured filters in URL state and sends them to the API', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()
    await screen.findByRole('heading', { name: material.title })
    fireEvent.change(screen.getByLabelText('Predmet'), { target: { value: 'Grammar' } })

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('subject=Grammar'))).toBe(true))
    expect(screen.getByLabelText('Predmet')).toHaveValue('Grammar')
    expect(screen.getByRole('button', { name: 'Poništi sve' })).toBeInTheDocument()
  })

  it('shows a recoverable error state', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ code: 'request_failed' }, 503))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()

    expect(await screen.findByRole('heading', { name: 'Biblioteka nije dostupna' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pokušaj ponovno' })).toBeInTheDocument()
  })
})
