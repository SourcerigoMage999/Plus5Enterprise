import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { GroupCreatePage } from '../src/groups/GroupCreatePage.tsx'

// jsdom has no native modal API; real focus trapping/Escape are verified in Edge.
Object.defineProperties(HTMLDialogElement.prototype, {
  showModal: { configurable: true, value() { this.setAttribute('open', '') } },
  close: { configurable: true, value() { this.removeAttribute('open') } },
})

const options = { programs: [{ id: 'p', name: 'Matematika' }], schoolGrades: [{ id: 'g', name: 'Sedmi razred' }], groups: [] }
const ana = { id: 'a', firstName: 'Ana', lastName: 'Anić', schoolGrade: 'Sedmi razred', programName: 'Matematika', recommended: true, rowVersion: 'AAAAAAAAAAE=' }
function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function mock(fail = false, empty = false) {
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input)
    if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
    if (init?.method === 'POST') return json({ id: 'created', sessionCount: 0 }, 201)
    if (url.includes('create-options')) return json(options)
    const second = url.includes('page=2')
    return fail ? json({}, 503) : json({ items: empty ? [] : [{ ...ana, ...(second ? { id: 'b', firstName: 'Borna' } : {}) }], page: second ? 2 : 1, pageSize: 8, totalCount: empty ? 0 : 9, totalPages: empty ? 0 : 2 })
  })
}
function open() {
  const router = createMemoryRouter([{ path: '/students/groups/new', element: <GroupCreatePage /> }, { path: '/students', element: <h1>Popis učenika</h1> }, { path: '/students/groups', element: <h1>Grupe</h1> }], { initialEntries: ['/students', '/students/groups/new', '/students/groups'], initialIndex: 1 })
  render(<RouterProvider router={router} />)
  return router
}
async function choose() {
  await screen.findByRole('option', { name: 'Matematika' })
  fireEvent.change(screen.getByLabelText(/Program \/ fokus/), { target: { value: 'p' } })
  fireEvent.change(screen.getByLabelText(/Razred \/ razina/), { target: { value: 'g' } })
}

describe('group creation', () => {
  it('uses real options and makes no writes until explicit submit', async () => {
    mock(); open(); await choose()
    expect(await screen.findByRole('checkbox', { name: /Ana Anić/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Kreiraj grupu' })).toBeEnabled()
    expect(screen.queryByText('B1')).not.toBeInTheDocument()
    expect(vi.mocked(fetch).mock.calls.every(([, init]) => !init?.method || init.method === 'GET')).toBe(true)
  })
  it('retains selection across pages and enforces capacity', async () => {
    mock(); open(); await choose()
    fireEvent.change(screen.getByLabelText(/Maksimalan broj/), { target: { value: '1' } })
    fireEvent.click(await screen.findByRole('checkbox', { name: /Ana Anić/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Sljedeća' }))
    expect(await screen.findByRole('checkbox', { name: /Borna Anić/ })).toBeDisabled()
    fireEvent.change(screen.getByLabelText(/Maksimalan broj/), { target: { value: '0' } })
    expect(screen.getByRole('alert')).toHaveTextContent('Kapacitet ne može biti manji')
    expect(screen.getByLabelText(/Maksimalan broj/)).toHaveValue(1)
    fireEvent.click(screen.getByRole('button', { name: 'Ukloni odabir Ana Anić' }))
    expect(screen.getByRole('checkbox', { name: /Borna Anić/ })).toBeEnabled()
  })
  it('shows query errors, retries and shows empty state', async () => {
    mock(true); open(); await choose()
    expect(await screen.findByRole('alert')).toHaveTextContent('Učenike nije moguće učitati')
    mock(false, true)
    fireEvent.click(screen.getByRole('button', { name: 'Pokušaj ponovno' }))
    expect(await screen.findByText('Nema dostupnih učenika za ovu pretragu.')).toBeInTheDocument()
  })
  it('warns before leaving using the all-students link', async () => {
    mock(); open(); await choose()
    fireEvent.click(screen.getByRole('link', { name: 'Pogledaj sve učenike' }))
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Ostani na obrascu' }))
    expect(screen.getByRole('heading', { name: '2.8 Nova grupa' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('link', { name: 'Pogledaj sve učenike' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Napusti obrazac' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Popis učenika' })).toBeInTheDocument())
  })
  it('creates an active empty group without schedule and navigates after success', async () => {
    mock(); open(); await choose()
    fireEvent.change(screen.getByLabelText(/Naziv grupe/), { target: { value: 'Prazna grupa' } })
    fireEvent.change(screen.getByLabelText(/Maksimalan broj/), { target: { value: '6' } })
    fireEvent.click(screen.getByRole('button', { name: 'Kreiraj grupu' }))
    expect(await screen.findByRole('heading', { name: 'Grupe' })).toBeInTheDocument()
    const [, request] = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'POST')!
    expect(JSON.parse(request!.body as string)).toMatchObject({ members: [], slots: [], endsOn: null, startsOn: null })
    expect(new Headers(request!.headers).get('X-CSRF-TOKEN')).toBe('csrf')
  })
  it.each([-1, 1])('blocks history navigation %s without losing input', async direction => {
    mock(); const router = open(); await choose()
    fireEvent.change(screen.getByLabelText(/Naziv grupe/), { target: { value: 'Nespremljeno' } })
    void router.navigate(direction)
    fireEvent.click(await screen.findByRole('button', { name: 'Ostani na obrascu' }))
    expect(screen.getByLabelText(/Naziv grupe/)).toHaveValue('Nespremljeno')
  })
  it('sends optional open-ended schedule and prevents duplicate submit while pending', async () => {
    mock()
    const original = vi.mocked(fetch).getMockImplementation()!
    let complete: ((response: Response) => void) | undefined
    vi.mocked(fetch).mockImplementation((input, init) => init?.method === 'POST' ? new Promise(resolve => { complete = resolve }) : original(input, init))
    open(); await choose()
    fireEvent.change(screen.getByLabelText(/Naziv grupe/), { target: { value: 'Raspored' } })
    fireEvent.change(screen.getByLabelText(/Maksimalan broj/), { target: { value: '6' } })
    fireEvent.click(screen.getByRole('button', { name: '+ Dodaj termin' }))
    fireEvent.change(screen.getByLabelText('Početak 1. termina'), { target: { value: '16:00' } })
    fireEvent.change(screen.getByLabelText('Završetak 1. termina'), { target: { value: '17:00' } })
    fireEvent.change(screen.getByLabelText(/Datum početka/), { target: { value: '2026-09-21' } })
    fireEvent.click(screen.getByRole('button', { name: 'Kreiraj grupu' }))
    await waitFor(() => expect(complete).toBeDefined())
    expect(screen.getByRole('button', { name: 'Spremanje…' })).toBeDisabled()
    const writes = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST')
    expect(writes).toHaveLength(1)
    expect(JSON.parse(writes[0][1]!.body as string)).toMatchObject({ startsOn: '2026-09-21', endsOn: null, slots: [{ start: '16:00', end: '17:00' }] })
    complete!(json({ code: 'schedule_conflict' }, 409))
    expect(await screen.findByRole('alert')).toHaveTextContent('Raspored se preklapa')
    expect(screen.getByLabelText(/Naziv grupe/)).toHaveValue('Raspored')
    expect(screen.getByRole('button', { name: 'Kreiraj grupu' })).toBeEnabled()
  })
})
