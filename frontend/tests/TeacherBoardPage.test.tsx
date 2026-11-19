import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { BoardLandingPage } from '../src/board/BoardLandingPage.tsx'
import { TeacherBoardPage } from '../src/board/TeacherBoardPage.tsx'

const activityA = { id: 'activity-a', sourceLessonActivityId: 'source-a', sortOrder: 0,
  origin: 'Planned', status: 'Pending', name: 'Uvod', purpose: 'Engagement', durationMinutes: 10,
  workMode: 'Individual', description: null, studentInstructions: null,
  showStudentInstructionsOnBoard: false, showProgressOnBoard: false,
  materialVersionIds: [], assessableTaskVersionIds: [] }
const activityB = { ...activityA, id: 'activity-b', sourceLessonActivityId: 'source-b',
  sortOrder: 1, name: 'Vježba', purpose: 'Practice', durationMinutes: 35 }
const session = { id: 'session-101', lessonPlanId: 'plan-101', lessonPlanVersionId: 'version-101',
  lessonPlanVersionNumber: 3, calendarSessionId: null, participantType: 'student',
  participantName: 'Lana Kovač', goal: 'Primijeniti ciljano znanje.', status: 'InProgress',
  startedAtUtc: '2026-11-15T09:00:00Z', scheduledStartsAtUtc: null, scheduledEndsAtUtc: null,
  scheduledTimeZoneId: null, lastCommittedAtUtc: '2026-11-15T09:00:00Z', revision: 1,
  rowVersion: 'AQAAAAAAAAA=', activities: [activityA, activityB] }

function json(value: unknown) { return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } }) }

describe('PLUS 5 Teacher Board runtime', () => {
  it('recovers an active runtime and persists a revision-safe timeline reorder', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.endsWith('/mutations') && init?.method === 'POST') return json({ ...session, revision: 2, activities: [activityB, activityA] })
      return json(session)
    })
    render(<MemoryRouter initialEntries={['/board/sessions/session-101']}><Routes><Route path="board/sessions/:lessonSessionId" element={<TeacherBoardPage />} /></Routes></MemoryRouter>)

    expect(await screen.findByRole('heading', { name: 'Ploča učitelja' })).toBeInTheDocument()
    expect(screen.getByText('Ready snapshot')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Završi sat · Phase 10.5' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '+ Dodaj aktivnost · Phase 10.2' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Pomakni Vježba gore' }))
    await waitFor(() => expect(screen.getByText('Revizija 2')).toBeInTheDocument())
    const mutation = vi.mocked(fetch).mock.calls.find(([url]) => String(url).endsWith('/mutations'))
    expect(JSON.parse(String(mutation?.[1]?.body))).toMatchObject({
      type: 'ReorderTimeline', expectedRevision: 1, activityIds: ['activity-b', 'activity-a'],
    })
  })

  it('shows a chooser when multiple active sessions exist', async () => {
    vi.mocked(fetch).mockResolvedValue(json([
      { id: 'session-a', lessonPlanId: 'plan-a', lessonPlanVersionId: 'version-a', calendarSessionId: null,
        participantType: 'student', participantName: 'Lana Kovač', goal: 'Prvi cilj',
        startedAtUtc: '2026-11-15T09:00:00Z', lastCommittedAtUtc: '2026-11-15T09:10:00Z', revision: 2, activityCount: 2 },
      { id: 'session-b', lessonPlanId: 'plan-b', lessonPlanVersionId: 'version-b', calendarSessionId: null,
        participantType: 'group', participantName: 'Grupa A', goal: 'Drugi cilj',
        startedAtUtc: '2026-11-15T10:00:00Z', lastCommittedAtUtc: '2026-11-15T10:05:00Z', revision: 1, activityCount: 3 },
    ]))
    render(<MemoryRouter initialEntries={['/board']}><Routes><Route path="board" element={<BoardLandingPage />} /></Routes></MemoryRouter>)

    expect(await screen.findByRole('heading', { name: 'Aktivni satovi' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Lana Kovač' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Grupa A' })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'Nastavi sat' })).toHaveLength(2)
  })
})
