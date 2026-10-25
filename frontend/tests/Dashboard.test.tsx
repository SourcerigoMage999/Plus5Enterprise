import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { DashboardPage } from '../src/dashboard/DashboardPage.tsx'

const individualSession = {
  id: 'session-1',
  deliveryMode: 1,
  groupId: null,
  studentId: 'student-1',
  contextName: 'Petar Horvat',
  schoolGradeName: '8. razred',
  programId: 'program-1',
  programName: 'General English',
  title: 'Past Simple',
  startsAtUtc: '2026-10-25T14:00:00Z',
  endsAtUtc: '2026-10-25T14:45:00Z',
  timeZoneId: 'Europe/Zagreb',
  locationId: 'location-1',
  locationName: 'Učionica 1',
  online: false,
  status: 1,
  participantCount: 1,
} as const

const dashboard = {
  timeZoneId: 'Europe/Zagreb',
  today: '2026-10-25',
  generatedAtUtc: '2026-10-25T08:00:00Z',
  todaySessions: [
    individualSession,
    {
      id: 'session-2',
      deliveryMode: 2,
      groupId: 'group-1',
      studentId: null,
      contextName: 'Grammar 8A',
      schoolGradeName: '8. razred',
      programId: 'program-1',
      programName: 'Grammar Focus',
      title: null,
      startsAtUtc: '2026-10-25T16:00:00Z',
      endsAtUtc: '2026-10-25T16:45:00Z',
      timeZoneId: 'Europe/Zagreb',
      locationId: null,
      locationName: null,
      online: true,
      status: 2,
      participantCount: 4,
    },
  ],
  nextSession: individualSession,
  students: { totalCount: 12, activeCount: 8, onHoldCount: 3, inactiveCount: 1 },
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
}

function open() {
  return render(<MemoryRouter><DashboardPage /></MemoryRouter>)
}

describe('teacher dashboard', () => {
  it('renders real dashboard projections and keeps future modules honestly disabled', async () => {
    vi.mocked(fetch).mockResolvedValue(json(dashboard))
    open()

    expect(await screen.findByRole('heading', { level: 1, name: /Dobro došli/ })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Dodaj učenika/ })).toHaveAttribute('href', '/students/new')
    expect(screen.getByRole('link', { name: /Materijali/ })).toHaveAttribute('href', '/materials')
    expect(screen.getByRole('button', { name: /Odgovori roditelju/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Pošalji domaću/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Kreiraj test\/ispit/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Financije/ })).toBeDisabled()

    const schedule = screen.getByRole('region', { name: 'Današnji raspored' })
    expect(within(schedule).getByRole('link', { name: 'Petar Horvat' })).toHaveAttribute('href', '/students/student-1')
    expect(within(schedule).getByRole('link', { name: 'Otvori termin Petar Horvat' })).toHaveAttribute('href', '/schedule/session-1')
    expect(within(schedule).getByText('Grupa (4)')).toBeInTheDocument()
    expect(within(schedule).getByText('U tijeku')).toBeInTheDocument()

    expect(screen.getByRole('heading', { level: 3, name: 'Past Simple' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Otvori termin' })).toHaveAttribute('href', '/schedule/session-1')
    expect(screen.getByRole('button', { name: 'Otvori plan sata' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Pokreni sat' })).toBeDisabled()
    expect(screen.getByText('Ukupno: 12')).toBeInTheDocument()
    expect(screen.getByText('Statusi su administrativni i ne predstavljaju procjenu znanja ili spremnosti učenika.')).toBeInTheDocument()
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
  })

  it('renders honest empty states', async () => {
    vi.mocked(fetch).mockResolvedValue(json({
      ...dashboard,
      todaySessions: [],
      nextSession: null,
      students: { totalCount: 0, activeCount: 0, onHoldCount: 0, inactiveCount: 0 },
    }))
    open()

    expect(await screen.findByText('Danas nema zakazanih termina')).toBeInTheDocument()
    expect(screen.getByText('Nema budućeg zakazanog termina')).toBeInTheDocument()
    expect(screen.getByText('Ukupno: 0')).toBeInTheDocument()
  })

  it('shows a retryable error and recovers', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json({ code: 'request_failed' }, 503))
      .mockResolvedValueOnce(json(dashboard))
    open()

    expect(await screen.findByRole('alert')).toHaveTextContent('Radni stol trenutačno nije moguće učitati.')
    fireEvent.click(screen.getByRole('button', { name: 'Pokušaj ponovno' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Današnji raspored' })).toBeInTheDocument())
  })
})
