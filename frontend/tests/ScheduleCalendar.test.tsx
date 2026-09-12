import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ScheduleCalendarPage } from '../src/schedule/ScheduleCalendarPage.tsx'
import { calendarRange, sessionLocal, startOfWeek } from '../src/schedule/calendarDate.ts'

const groupId = '10000000-0000-0000-0000-000000000001'
const programId = '20000000-0000-0000-0000-000000000001'
const locationId = '30000000-0000-0000-0000-000000000001'
const calendar = {
  timeZoneId: 'Europe/Zagreb',
  from: '2026-09-14',
  to: '2026-09-21',
  items: [
    { id: 'session-1', deliveryMode: 2, groupId, studentId: null, contextName: 'B1 Teens', programId, programName: 'Engleski B1', startsAtUtc: '2026-09-14T14:00:00Z', endsAtUtc: '2026-09-14T15:00:00Z', timeZoneId: 'Europe/Zagreb', locationId, locationName: 'Učionica 2', online: false, status: 1, memberCount: 4, capacity: 6 },
    { id: 'session-2', deliveryMode: 1, groupId: null, studentId: 'student-1', contextName: 'Ana Anić', programId, programName: 'Engleski B1', startsAtUtc: '2026-09-15T09:00:00Z', endsAtUtc: '2026-09-15T09:45:00Z', timeZoneId: 'Europe/Zagreb', locationId: null, locationName: null, online: true, status: 1, memberCount: 0, capacity: null },
  ],
  reminders: [],
  summary: { groupSessions: 1, individualSessions: 1, totalSessions: 2, uniqueStudents: 5, plannedAttendances: 5, availableSeats: 2 },
  groups: [{ id: groupId, name: 'B1 Teens' }],
  programs: [{ id: programId, name: 'Engleski B1' }],
  locations: [{ id: locationId, name: 'Učionica 2' }],
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
}

function open() {
  return render(<MemoryRouter initialEntries={['/schedule?date=2026-09-16']}><ScheduleCalendarPage /></MemoryRouter>)
}

describe('schedule calendar', () => {
  it('renders canonical calendar hierarchy, exact metrics and honest future actions', async () => {
    vi.mocked(fetch).mockImplementation(async () => json(calendar))
    open()

    expect(await screen.findByRole('heading', { level: 1, name: '3.1 Raspored' })).toBeInTheDocument()
    expect(screen.getByText('Pregledajte raspored svih grupa i individualnih sati.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+ Novi termin' })).toHaveAttribute('href', '/schedule/new?date=2026-09-16')
    expect(screen.getByRole('region', { name: /Kalendar termina/ })).toHaveAttribute('tabindex', '0')
    expect(screen.getAllByText('B1 Teens')).not.toHaveLength(0)
    expect(screen.getByRole('link', { name: /Otvori detalj termina B1 Teens/ })).toHaveAttribute('href', '/schedule/session-1')
    expect(screen.getByText('Ana Anić')).toBeInTheDocument()
    expect(screen.getByText('Jedinstvenih učenika').parentElement).toHaveTextContent('5')
    expect(screen.getByText('Planiranih dolazaka').parentElement).toHaveTextContent('5')
    expect(screen.getByRole('button', { name: /Pogledaj detaljan izvještaj/ })).toBeDisabled()
    expect(screen.getByRole('checkbox', { name: 'Prikaži samo moje termine' })).toBeChecked()
    expect(within(screen.getByRole('group', { name: 'Način prikaza' })).getByRole('button', { name: 'Tjedan' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('keeps date, view and filters in URL-driven query state', async () => {
    vi.mocked(fetch).mockImplementation(async () => json(calendar))
    open()
    await screen.findAllByText('B1 Teens')

    fireEvent.click(screen.getByRole('button', { name: 'Dan' }))
    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) => String(input).includes('from=2026-09-16&to=2026-09-17'))).toBe(true))
    fireEvent.change(screen.getByRole('combobox', { name: 'Grupa' }), { target: { value: groupId } })
    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) => String(input).includes(`groupId=${groupId}`))).toBe(true))
    expect(screen.getByRole('button', { name: 'Dan' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('shows retryable error and recovers without losing the selected period', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json({ code: 'request_failed' }, 500)).mockResolvedValueOnce(json(calendar))
    open()
    expect(await screen.findByRole('alert')).toHaveTextContent('Raspored trenutno nije moguće učitati.')
    fireEvent.click(screen.getByRole('button', { name: 'Pokušaj ponovno' }))
    expect(await screen.findAllByText('B1 Teens')).not.toHaveLength(0)
    expect(vi.mocked(fetch).mock.calls.every(([input]) => String(input).includes('from=2026-09-14&to=2026-09-21'))).toBe(true)
  })
})

describe('calendar date conversion', () => {
  it('uses Monday as week start and converts UTC sessions to Europe Zagreb local time', () => {
    expect(startOfWeek('2026-09-20')).toBe('2026-09-14')
    expect(calendarRange('2026-09-16', 'week')).toEqual({ from: '2026-09-14', to: '2026-09-21' })
    expect(sessionLocal('2026-09-14T14:00:00Z', 'Europe/Zagreb')).toMatchObject({ date: '2026-09-14', time: '16:00', minutes: 960 })
  })
})
