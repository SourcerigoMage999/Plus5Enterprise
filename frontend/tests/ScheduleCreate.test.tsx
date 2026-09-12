import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ScheduleCreatePage } from '../src/schedule/ScheduleCreatePage.tsx'

Object.defineProperties(HTMLDialogElement.prototype, {
  showModal: { configurable: true, value() { this.setAttribute('open', '') } },
  close: { configurable: true, value() { this.removeAttribute('open') } },
})

const group = {
  id: 'group-1', name: 'Grammar 8A', programId: 'program-1', programName: 'Grammar Focus',
  schoolGradeId: 'grade-8', schoolGrade: '8. razred', status: 'active', capacity: 8,
  memberCount: 6, rowVersion: 'AQ==', slots: [{ dayOfWeek: 2, start: '16:00', end: '17:00', timeZoneId: 'Europe/Zagreb', location: 'Učionica 1', online: false }],
}
const student = {
  id: 'student-1', firstName: 'Petar', lastName: 'Horvat', nickname: null,
  schoolGrade: { id: 'grade-8', name: '8. razred', code: '8R' },
  program: { id: 'program-1', name: 'Grammar Focus', code: null }, deliveryMode: 'individual',
  group: null, status: 'active', lastSessionAtUtc: null,
}
const location = { id: 'location-1', name: 'Učionica 1' }
const page = (items: unknown[]) => ({ items, page: 1, pageSize: 25, totalCount: items.length, totalPages: items.length ? 1 : 0 })
const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })

function mockApi(options: { conflict?: boolean; duplicate?: boolean } = {}) {
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input)
    if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
    if (init?.method === 'POST') return options.conflict ? json({ code: 'schedule_conflict' }, 409) : json({ id: 'new-session', sessionCount: 1 }, 201)
    if (url.includes('/groups/create-locations')) return json(page([location]))
    if (url.includes('/groups?')) return json(page([group]))
    if (url.includes('/students?')) return json(page([student]))
    if (options.duplicate && url.endsWith('/schedule/source-session')) return json({
      id: 'source-session', deliveryMode: 1, groupId: null, studentId: student.id,
      contextName: 'Petar Horvat', programId: 'program-1', programName: 'Grammar Focus',
      schoolGradeId: 'grade-8', schoolGrade: '8. razred', groupStatus: null, capacity: null,
      title: 'Priprema za ispit', notes: null, startsAtUtc: '2026-09-15T14:00:00Z',
      endsAtUtc: '2026-09-15T15:00:00Z', timeZoneId: 'Europe/Zagreb', locationId: location.id,
      locationName: location.name, online: false, status: 1, isSeriesOccurrence: false,
      isSeriesException: false, createdAtUtc: '2026-09-12T08:00:00Z', updatedAtUtc: '2026-09-12T08:00:00Z',
      cancelledAtUtc: null, participants: [],
    })
    return json({}, 404)
  })
}

function open(entry = '/schedule/new?date=2026-09-14') {
  const router = createMemoryRouter([
    { path: '/schedule/new', element: <ScheduleCreatePage /> },
    { path: '/schedule/:sessionId', element: <h1>Detalj novog termina</h1> },
    { path: '/schedule', element: <h1>Raspored</h1> },
    { path: '/students/groups/:groupId/edit', element: <h1>Uredi grupu</h1> },
  ], { initialEntries: ['/schedule', entry], initialIndex: 1 })
  render(<RouterProvider router={router} />)
  return router
}

async function fillTime() {
  fireEvent.change(screen.getByLabelText(/Početak/), { target: { value: '16:00' } })
  fireEvent.change(screen.getByLabelText(/Završetak/), { target: { value: '17:30' } })
}

describe('schedule creation', () => {
  it('creates a concrete group session and navigates to its detail', async () => {
    mockApi(); open()
    fireEvent.change(await screen.findByLabelText('Grupa'), { target: { value: group.id } })
    expect(screen.getByText(/već ima redoviti raspored/)).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: /Redovno – svaki tjedan/ })).toBeDisabled()
    await fillTime()
    fireEvent.click(screen.getByRole('button', { name: 'Spremi termin' }))
    expect(await screen.findByRole('heading', { name: 'Detalj novog termina' })).toBeInTheDocument()
    const [, request] = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'POST')!
    expect(JSON.parse(request!.body as string)).toMatchObject({
      deliveryMode: 2, contextId: group.id, date: '2026-09-14', startsAt: '16:00', endsAt: '17:30',
      repeatWeekly: false, endsOn: null, locationId: null, onlineMeetingUrl: null, notes: null,
    })
    expect(new Headers(request!.headers).get('X-CSRF-TOKEN')).toBe('csrf')
  })

  it('creates an open-ended weekly individual recurrence and prevents duplicate submit', async () => {
    mockApi()
    const original = vi.mocked(fetch).getMockImplementation()!
    let complete: ((response: Response) => void) | undefined
    vi.mocked(fetch).mockImplementation((input, init) => init?.method === 'POST'
      ? new Promise(resolve => { complete = resolve })
      : original(input, init))
    open()
    fireEvent.click(screen.getByRole('radio', { name: /Individualni sat/ }))
    fireEvent.change(await screen.findByLabelText('Učenik'), { target: { value: student.id } })
    await fillTime()
    fireEvent.click(screen.getByRole('radio', { name: /Redovno – svaki tjedan/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Spremi termin' }))
    await waitFor(() => expect(complete).toBeDefined())
    expect(screen.getByRole('button', { name: 'Spremanje…' })).toBeDisabled()
    expect(vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST')).toHaveLength(1)
    const request = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'POST')![1]!
    expect(JSON.parse(request.body as string)).toMatchObject({ deliveryMode: 1, contextId: student.id, repeatWeekly: true, endsOn: null })
    complete!(json({ id: 'new-session', sessionCount: 12 }, 201))
    expect(await screen.findByRole('heading', { name: 'Detalj novog termina' })).toBeInTheDocument()
  })

  it('retains the form and explains a server-side conflict', async () => {
    mockApi({ conflict: true }); open()
    fireEvent.change(await screen.findByLabelText('Grupa'), { target: { value: group.id } })
    await fillTime()
    fireEvent.change(screen.getByLabelText(/Naziv termina/), { target: { value: 'Dodatni sat' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi termin' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Raspored se preklapa')
    expect(screen.getByLabelText(/Naziv termina/)).toHaveValue('Dodatni sat')
  })

  it('prefills a duplicate as a new concrete session and guards unsaved navigation', async () => {
    mockApi({ duplicate: true }); open('/schedule/new?duplicate=source-session')
    await waitFor(() => expect(screen.getByLabelText(/Naziv termina/)).toHaveValue('Priprema za ispit'))
    expect(screen.getByRole('radio', { name: /Individualni sat/ })).toBeChecked()
    expect(screen.getByLabelText(/Datum/)).toHaveValue('2026-09-15')
    fireEvent.change(screen.getByLabelText(/Naziv termina/), { target: { value: 'Priprema za ispit — kopija' } })
    fireEvent.click(screen.getByRole('link', { name: 'Odustani' }))
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Ostani na obrascu' }))
    expect(screen.getByLabelText(/Naziv termina/)).toHaveValue('Priprema za ispit — kopija')
  })
})
