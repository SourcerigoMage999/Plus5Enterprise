import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { GroupListPage } from '../src/groups/GroupListPage.tsx'

const group = { id: 'group-1', name: 'Grupa Orion', programId: 'program-1', programName: 'Matematika 7', schoolGradeId: 'grade-1', schoolGrade: 'Sedmi razred', status: 'active', capacity: 8, memberCount: 1, rowVersion: 'AQ==', slots: [] }
const student = { id: 'student-1', firstName: 'Ana', lastName: 'Anić', schoolGrade: 'Sedmi razred', recommended: true, rowVersion: 'Ag==' }
function page<T>(items: T[]) { return { items, page: 1, pageSize: 8, totalCount: items.length, totalPages: items.length ? 1 : 0 } }
function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function mock(options: { full?: boolean; empty?: boolean; fail?: boolean; conflict?: boolean } = {}) {
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input)
    if (url.endsWith('/auth/csrf')) return json({ token: 'csrf-test' })
    if (init?.method === 'POST') return options.conflict ? json({ code: 'concurrency_conflict' }, 409) : new Response(null, { status: 204 })
    if (url.endsWith('/students/overview')) return json({ programs: [{ id: 'program-1', name: 'Matematika 7' }] })
    if (url.endsWith('/groups/overview')) return json({ totalGroups: 1, activeGroups: 1, students: 1, availableSeats: 7, sessionsThisWeek: 0, weekStartsOn: '2026-08-31' })
    if (url.includes('/groups?')) return options.fail ? json({}, 503) : json(page(options.empty || url.includes('status=2') ? [] : [group]))
    if (url.endsWith('/groups/group-1')) return json({ ...group, capacity: options.full ? 1 : 8 })
    if (url.includes('/students?')) return json(page([student]))
    if (url.includes('/candidates?')) return json(page([{ ...student, id: 'candidate-1', firstName: 'Borna' }]))
    if (url.includes('/sessions?')) return json(page([]))
    throw new Error(`Unexpected request: ${url}`)
  })
}
function open() { return render(<MemoryRouter><GroupListPage /></MemoryRouter>) }

describe('group screen', () => {
  it('renders canonical regions, neutral metrics and existing dossier/edit links', async () => {
    mock(); open()
    expect(await screen.findByRole('link', { name: 'Ana Anić' })).toHaveAttribute('href', '/students/student-1')
    expect(screen.getByRole('link', { name: 'Premjesti' })).toHaveAttribute('href', '/students/student-1/edit')
    expect(screen.getByRole('heading', { name: '2.7 Grupe' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Popis grupa' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Članovi grupe — vodoravno pomična tablica' })).toHaveAttribute('tabindex', '0')
    expect(screen.getByRole('link', { name: 'Ana Anić' }).querySelector('.groups-student-avatar')).toHaveAttribute('aria-hidden', 'true')
    expect(screen.getAllByText('Nije dostupno')).toHaveLength(2)
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: '+ Nova grupa' })).toBeDisabled()
    fireEvent.click(screen.getByRole('tab', { name: 'Raspored' }))
    expect(await screen.findByText('Nema nadolazećih termina.')).toBeInTheDocument()
    fireEvent.keyDown(screen.getByRole('tab', { name: 'Raspored' }), { key: 'ArrowRight' })
    expect(screen.getByRole('tab', { name: 'Materijali' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByText('Materijali još nisu dostupni')).toBeInTheDocument()
  })

  it('confirms membership changes, sends both versions and reports conflicts', async () => {
    mock({ conflict: true }); open()
    fireEvent.click(await screen.findByRole('button', { name: 'Ukloni Ana Anić' }))
    expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
    expect(screen.getByRole('group', { name: 'Potvrda promjene članstva' })).toHaveTextContent('Učenik ostaje u aplikaciji')
    fireEvent.click(screen.getByRole('button', { name: 'Potvrdi promjenu' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Podaci su se u međuvremenu promijenili')
    const [, request] = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'POST')!
    expect(JSON.parse(request!.body as string)).toEqual({ join: false, groupRowVersion: 'AQ==', studentRowVersion: 'Ag==' })
    expect(new Headers(request!.headers).get('X-CSRF-TOKEN')).toBe('csrf-test')
  })

  it('offers candidates and explains program assignment before saving', async () => {
    mock(); open()
    fireEvent.click(await screen.findByRole('button', { name: '+ Dodaj učenika u grupu' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Dodaj Borna Anić' }))
    expect(screen.getByRole('group', { name: 'Potvrda promjene članstva' })).toHaveTextContent('Učenik preuzima program Matematika 7')
    fireEvent.click(screen.getByRole('button', { name: 'Odustani' }))
    expect(screen.queryByRole('group', { name: 'Potvrda promjene članstva' })).not.toBeInTheDocument()
    expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('disables adding to a full group and preserves status filters', async () => {
    mock({ full: true }); open()
    expect(await screen.findByRole('button', { name: '+ Dodaj učenika u grupu' })).toBeDisabled()
    expect(screen.getByText('Grupa je popunjena.')).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Status grupe'), { target: { value: '2' } })
    expect(await screen.findByText('Nema grupa za odabrane filtre')).toBeInTheDocument()
    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) => String(input).includes('status=2'))).toBe(true))
  })

  it('shows empty and recoverable failure states without stale detail', async () => {
    mock({ fail: true }); open()
    const error = await screen.findByRole('alert')
    expect(within(error).getByRole('button', { name: 'Pokušaj ponovno' })).toBeInTheDocument()
    mock({ empty: true })
    fireEvent.click(within(error).getByRole('button', { name: 'Pokušaj ponovno' }))
    expect(await screen.findByText('Nema grupa za odabrane filtre')).toBeInTheDocument()
    expect(screen.getByText('Odaberite grupu za prikaz detalja.')).toBeInTheDocument()
  })
})
