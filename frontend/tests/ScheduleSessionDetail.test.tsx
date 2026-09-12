import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ScheduleSessionDetailPage } from '../src/schedule/ScheduleSessionDetailPage.tsx'

const detail = {
  id: 'session-1', deliveryMode: 2, groupId: 'group-1', studentId: null, contextName: 'Grammar 8A',
  programId: 'program-1', programName: 'Grammar Focus', schoolGradeId: 'grade-1', schoolGrade: '8. razred',
  groupStatus: 1, capacity: 8, title: 'Present Perfect', notes: 'Ponoviti nepravilne glagole.',
  startsAtUtc: '2026-08-25T14:00:00Z', endsAtUtc: '2026-08-25T15:30:00Z', timeZoneId: 'Europe/Zagreb',
  locationId: 'location-1', locationName: 'Učionica 1', online: false, status: 1, isSeriesOccurrence: true,
  isSeriesException: false, createdAtUtc: '2026-08-12T09:24:00Z', updatedAtUtc: '2026-08-12T09:24:00Z', cancelledAtUtc: null,
  participants: [{ id: 'student-1', firstName: 'Ana', lastName: 'Kovač', schoolGradeId: 'grade-1', schoolGrade: '8. razred', status: 1 }],
}

function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function open() { return render(<MemoryRouter initialEntries={['/schedule/session-1']}><Routes><Route path="schedule/:sessionId" element={<ScheduleSessionDetailPage />} /></Routes></MemoryRouter>) }

describe('schedule session detail', () => {
  it('renders canonical hierarchy, real session facts and owner-projected participants', async () => {
    vi.mocked(fetch).mockResolvedValue(json(detail))
    open()
    expect(await screen.findByRole('heading', { level: 1, name: '3.2 Detalj termina' })).toBeInTheDocument()
    expect(screen.getAllByText('Grammar 8A')).not.toHaveLength(0)
    expect(screen.getByText('Grupni sat')).toBeInTheDocument()
    expect(screen.getByText('Program: Grammar Focus')).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: /Ana Kovač/ })[0]).toHaveAttribute('href', '/students/student-1')
    expect(screen.getByText('Ponoviti nepravilne glagole.')).toBeInTheDocument()
    expect(screen.getByText('Termin je kreiran iz rasporeda.')).toBeInTheDocument()
  })

  it('keeps future writes honest and disabled at the Phase 4.2 boundary', async () => {
    vi.mocked(fetch).mockResolvedValue(json(detail))
    open()
    await screen.findAllByText('Grammar 8A')
    expect(screen.getByRole('button', { name: /Uredi termin/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Pokreni sat/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Pripremi sat/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Dodaj domaću zadaću/ })).toBeDisabled()
    expect(screen.getByText(/Session je planirani kalendarski termin/)).toBeInTheDocument()
  })

  it('renders individual context without inventing group data', async () => {
    vi.mocked(fetch).mockResolvedValue(json({ ...detail, deliveryMode: 1, groupId: null, studentId: 'student-1', contextName: 'Ana Kovač', groupStatus: null, capacity: null, online: true, locationId: null, locationName: null }))
    open()
    expect(await screen.findByText('Individualni sat')).toBeInTheDocument()
    expect(screen.getAllByText('Online')).toHaveLength(2)
    expect(screen.getAllByText('Učenik')).toHaveLength(2)
  })

  it('explains an empty temporal roster without creating placeholder students', async () => {
    vi.mocked(fetch).mockResolvedValue(json({ ...detail, participants: [] }))
    open()
    expect(await screen.findByText('Nema učenika za ovaj termin')).toBeInTheDocument()
    expect(screen.getByText('Prikaz koristi članstva koja vrijede u vrijeme početka termina.')).toBeInTheDocument()
    expect(screen.queryByText('Ana Kovač')).not.toBeInTheDocument()
  })

  it('shows a privacy-preserving not-found state and can retry', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json({}, 404)).mockResolvedValueOnce(json(detail))
    open()
    expect(await screen.findByRole('alert')).toHaveTextContent('Termin nije pronađen ili mu nemate pristup.')
    fireEvent.click(screen.getByRole('button', { name: 'Pokušaj ponovno' }))
    expect(await screen.findByText('Grupni sat')).toBeInTheDocument()
  })
})
