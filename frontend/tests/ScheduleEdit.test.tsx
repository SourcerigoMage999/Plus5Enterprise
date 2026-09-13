import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ScheduleEditPage } from '../src/schedule/ScheduleEditPage.tsx'

Object.defineProperties(HTMLDialogElement.prototype, {
  showModal: { configurable: true, value() { this.setAttribute('open', '') } },
  close: { configurable: true, value() { this.removeAttribute('open') } },
})

const item = {
  detail: {
    id: 'session-1', deliveryMode: 2, groupId: 'group-1', studentId: null, contextName: 'Grammar 8A',
    programId: 'program-1', programName: 'Grammar Focus', schoolGradeId: 'grade-8', schoolGrade: '8. razred',
    groupStatus: 1, capacity: 8, title: 'Present Perfect', notes: 'Redovna nastava.',
    startsAtUtc: '2026-09-15T14:00:00Z', endsAtUtc: '2026-09-15T15:30:00Z', timeZoneId: 'Europe/Zagreb',
    locationId: 'location-1', locationName: 'Učionica 1', online: false, status: 1, isSeriesOccurrence: true,
    isSeriesException: false, createdAtUtc: '2026-09-01T08:00:00Z', updatedAtUtc: '2026-09-01T08:00:00Z',
    cancelledAtUtc: null, participants: [{ id: 'student-1', firstName: 'Ana', lastName: 'Kovač', schoolGradeId: 'grade-8', schoolGrade: '8. razred', status: 1 }],
  },
  onlineMeetingUrl: null, rowVersion: 'AQIDBAUGBwg=', canEditFutureSeries: true,
}
const location = { id: 'location-1', name: 'Učionica 1' }
const json = (value: unknown, status = 200) => new Response(status === 204 ? null : JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })

function mockApi(options: { conflict?: boolean } = {}) {
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input)
    if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
    if (url.endsWith('/schedule/session-1/edit')) return json(item)
    if (url.includes('/groups/create-locations')) return json({ items: [location], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 })
    if (url.endsWith('/schedule/session-1/conflicts')) return json({ hasConflict: options.conflict ?? false })
    if (url.endsWith('/schedule/session-1/cancel')) return json(null, 204)
    if (url.endsWith('/schedule/session-1') && init?.method === 'PUT') return json({ id: 'updated-session', sessionCount: 1 })
    return json({}, 404)
  })
}

function open() {
  const router = createMemoryRouter([
    { path: '/schedule/:sessionId/edit', element: <ScheduleEditPage /> },
    { path: '/schedule/:sessionId', element: <h1>Detalj ažuriranog termina</h1> },
    { path: '/schedule', element: <h1>Raspored</h1> },
    { path: '/students/groups/:groupId/edit', element: <h1>Uredi grupu</h1> },
  ], { initialEntries: ['/schedule', '/schedule/session-1/edit'], initialIndex: 1 })
  render(<RouterProvider router={router} />)
  return router
}

describe('schedule editing', () => {
  it('prefills canonical data and exposes only safe future-series changes', async () => {
    mockApi(); open()
    expect(await screen.findByRole('heading', { name: '3.4 Uredi termin' })).toBeInTheDocument()
    expect(screen.getByLabelText('Grupa')).toHaveValue('group-1')
    expect(screen.getByLabelText(/Naziv termina/)).toHaveValue('Present Perfect')
    expect(screen.getByLabelText(/Datum/)).toHaveValue('2026-09-15')
    expect(screen.getByRole('radio', { name: /Svi budući termini/ })).toBeEnabled()
    fireEvent.click(screen.getByRole('radio', { name: /Svi budući termini/ }))
    expect(screen.getByLabelText(/Datum/)).toBeDisabled()
    expect(screen.getByLabelText(/Naziv termina/)).toBeDisabled()
    expect(screen.getByRole('link', { name: /Otvori 2.9 Uredi grupu/ })).toHaveAttribute('href', '/students/groups/group-1/edit')
  })

  it('updates only the selected occurrence and navigates to returned detail', async () => {
    mockApi(); open()
    fireEvent.change(await screen.findByLabelText(/Vrijeme početka/), { target: { value: '17:00' } })
    fireEvent.change(screen.getByLabelText(/Vrijeme završetka/), { target: { value: '18:30' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi promjene' }))
    expect(await screen.findByRole('heading', { name: 'Detalj ažuriranog termina' })).toBeInTheDocument()
    const [, request] = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'PUT')!
    expect(JSON.parse(request!.body as string)).toMatchObject({
      date: '2026-09-15', startsAt: '17:00', endsAt: '18:30', scope: 1,
      rowVersion: 'AQIDBAUGBwg=',
    })
    expect(new Headers(request!.headers).get('X-CSRF-TOKEN')).toBe('csrf')
  })
  it('shows the authoritative conflict preview and blocks save', async () => {
    mockApi({ conflict: true }); open()
    await screen.findByRole('heading', { name: '3.4 Uredi termin' })
    await waitFor(() => expect(screen.getByText('⚠ Konflikt termina')).toBeInTheDocument(), { timeout: 1500 })
    expect(screen.getByRole('button', { name: 'Spremi promjene' })).toBeDisabled()
    expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(false)
  })

  it('confirms cancellation without deleting and guards unsaved navigation', async () => {
    mockApi(); open()
    await screen.findByRole('heading', { name: '3.4 Uredi termin' })
    fireEvent.click(screen.getByRole('button', { name: '⊘ Otkaži termin' }))
    expect(screen.getByRole('dialog')).toHaveTextContent('Termin se neće izbrisati')
    fireEvent.click(screen.getByRole('button', { name: 'Otkaži termin' }))
    expect(await screen.findByRole('heading', { name: 'Detalj ažuriranog termina' })).toBeInTheDocument()
    expect(vi.mocked(fetch).mock.calls.some(([input]) => String(input).endsWith('/cancel'))).toBe(true)
  })
})
