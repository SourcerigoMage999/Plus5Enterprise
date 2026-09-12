import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { GroupEditPage } from '../src/groups/GroupEditPage.tsx'

Object.defineProperties(HTMLDialogElement.prototype, {
  showModal: { configurable: true, value() { this.setAttribute('open', '') } },
  close: { configurable: true, value() { this.removeAttribute('open') } },
})

const options = { programs: [{ id: 'p1', name: 'Grammar' }, { id: 'p2', name: 'Conversation' }], schoolGrades: [{ id: 'g1', name: 'Sedmi razred' }], groups: [] }
const group = { id: 'group', name: 'Grammar 7', description: 'Opis', programId: 'p1', programName: 'Grammar', schoolGradeId: 'g1', schoolGrade: 'Sedmi razred', status: 1, capacity: 6, memberCount: 1, rowVersion: 'AAAAAAAAAAE=', slots: [{ seriesId: 'series', dayOfWeek: 1, start: '16:00:00', end: '17:00:00', startsOn: '2026-09-01', endsOn: null, locationId: null, locationName: null }] }
const member = { id: 'student', firstName: 'Ana', lastName: 'Anić', schoolGrade: 'Sedmi razred', recommended: true, rowVersion: 'AAAAAAAAAAI=' }
function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function mock(memberCount = 1, putStatus = 200) {
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input)
    if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
    if (init?.method === 'PUT') return putStatus === 200 ? json({ id: 'group', sessionCount: 12 }) : json({ code: 'schedule_conflict' }, putStatus)
    if (init?.method === 'POST') return new Response(null, { status: 204 })
    if (url.includes('/groups/group/edit')) return json({ ...group, memberCount })
    if (url.includes('create-options')) return json(options)
    if (url.includes('create-locations')) return json({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 })
    if (url.includes('/groups/group/students')) return json({ items: memberCount ? [member] : [], page: 1, pageSize: 8, totalCount: memberCount, totalPages: memberCount ? 1 : 0 })
    return json({ items: [member], page: 1, pageSize: 8, totalCount: 1, totalPages: 1 })
  })
}
function open() {
  const router = createMemoryRouter([{ path: '/students/groups/:groupId/edit', element: <GroupEditPage /> }, { path: '/students/groups', element: <h1>Grupe</h1> }, { path: '/students/:studentId', element: <h1>Dosje</h1> }], { initialEntries: ['/students/groups/group/edit'] })
  render(<RouterProvider router={router} />)
  return router
}

describe('group editing', () => {
  it('loads canonical data, locks program with active members and performs no implicit writes', async () => {
    mock(); open()
    expect(await screen.findByRole('heading', { name: '2.9 Uredi grupu' })).toBeInTheDocument()
    expect(screen.getByLabelText(/Program \/ fokus/)).toBeDisabled()
    expect(screen.getByText(/Program nije moguće promijeniti dok grupa ima aktivne učenike/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Maksimalan broj/)).toHaveAttribute('min', '1')
    expect(screen.getByRole('button', { name: 'Spremi promjene' })).toBeDisabled()
    expect(vi.mocked(fetch).mock.calls.every(([, init]) => !init?.method || init.method === 'GET')).toBe(true)
  })

  it('allows program change for an empty group and saves explicit contract', async () => {
    mock(0); open()
    await screen.findByRole('heading', { name: '2.9 Uredi grupu' })
    fireEvent.change(screen.getByLabelText(/Program \/ fokus/), { target: { value: 'p2' } })
    fireEvent.change(screen.getByLabelText(/Naziv grupe/), { target: { value: 'Conversation 7' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi promjene' }))
    expect(await screen.findByRole('heading', { name: 'Grupe' })).toBeInTheDocument()
    const [, request] = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'PUT')!
    expect(JSON.parse(request!.body as string)).toMatchObject({ programId: 'p2', name: 'Conversation 7', rowVersion: 'AAAAAAAAAAE=', slots: [{ dayOfWeek: 1, start: '16:00', end: '17:00' }] })
    expect(new Headers(request!.headers).get('X-CSRF-TOKEN')).toBe('csrf')
  })

  it('keeps form data after a server schedule conflict', async () => {
    mock(1, 409); open()
    await screen.findByRole('heading', { name: '2.9 Uredi grupu' })
    fireEvent.change(screen.getByLabelText('Početak 1. termina'), { target: { value: '18:00' } })
    fireEvent.change(screen.getByLabelText('Završetak 1. termina'), { target: { value: '19:00' } })
    fireEvent.change(screen.getByLabelText(/Datum početka/), { target: { value: '2026-09-20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi promjene' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Raspored se preklapa')
    expect(screen.getByLabelText('Početak 1. termina')).toHaveValue('18:00')
  })

  it('does not mix membership writes with dirty form data', async () => {
    mock(); open()
    expect(await screen.findByRole('button', { name: 'Ukloni' })).toBeEnabled()
    fireEvent.change(screen.getByLabelText(/Naziv grupe/), { target: { value: 'Nespremljeno' } })
    expect(screen.getByRole('button', { name: 'Ukloni' })).toBeDisabled()
    expect(screen.getByText(/Najprije spremite ili odbacite promjene/)).toBeInTheDocument()
  })

  it('guards navigation when the form is dirty', async () => {
    mock(); const router = open()
    await screen.findByRole('heading', { name: '2.9 Uredi grupu' })
    fireEvent.change(screen.getByLabelText(/Opis grupe/), { target: { value: 'Promjena' } })
    void router.navigate('/students/groups')
    fireEvent.click(await screen.findByRole('button', { name: 'Ostani na obrascu' }))
    expect(screen.getByLabelText(/Opis grupe/)).toHaveValue('Promjena')
    await waitFor(() => expect(screen.getByRole('heading', { name: '2.9 Uredi grupu' })).toBeInTheDocument())
  })
})
