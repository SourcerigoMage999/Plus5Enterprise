import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonActivitySelectionPage } from '../src/lessonPlanning/LessonActivitySelectionPage.tsx'

const planId = '11111111-1111-1111-1111-111111111111'
const blockId = '22222222-2222-2222-2222-222222222222'
const view = {
  lessonPlanId: planId, lessonPlanVersionId: 'version-1', rowVersion: 'AAAAAAAAAAA=',
  participantName: 'Petar Horvat', participantType: 'student' as const, session: null,
  goal: 'Razlikovati Present Perfect i Past Simple.', targetDurationMinutes: 45,
  knowledgeComponents: [{ id: 'kc-1', name: 'Present Perfect', modelCode: 'ENG-G8', modelVersion: '2026' }],
  activities: [], balance: [], attachedMaterials: [],
}
const candidate = {
  knowledgeBlockVersionId: blockId, code: 'KB-005', name: 'Interaktivni zadaci', versionNumber: 1,
  minDurationMinutes: 10, optimalDurationMinutes: 20, maxDurationMinutes: 30,
  participantApplicability: 'Both', assessmentCapability: 'MayContainAssessment', existingActivityCount: 1,
}

function json(value: unknown) {
  return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

function open() {
  return render(<MemoryRouter initialEntries={[`/lesson-plans/${planId}/activity-selection`]}><Routes>
    <Route path="lesson-plans/:lessonPlanId/activity-selection" element={<LessonActivitySelectionPage />} />
    <Route path="lesson-plans/:lessonPlanId/editor" element={<h1>Editor plana</h1>} />
  </Routes></MemoryRouter>)
}

describe('lesson activity and Knowledge Block selection', () => {
  it('shows only server-approved candidates and appends the selected exact version', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.includes('/activity-candidates')) return json([candidate])
      if (url.endsWith('/editor/activities') && init?.method === 'POST') return json(view)
      return json(view)
    })
    open()

    expect(await screen.findByRole('heading', { name: 'Odabir aktivnosti / Knowledge Blocka' })).toBeInTheDocument()
    expect(await screen.findAllByText('Interaktivni zadaci')).toHaveLength(2)
    expect(screen.getByText('Već u planu: 1')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Dodaj u plan' }))

    expect(await screen.findByRole('heading', { name: 'Editor plana' })).toBeInTheDocument()
    const write = vi.mocked(fetch).mock.calls.find(([url, init]) =>
      String(url).endsWith('/editor/activities') && init?.method === 'POST')
    expect(JSON.parse(String(write?.[1]?.body))).toEqual({
      knowledgeBlockVersionId: blockId,
      rowVersion: view.rowVersion,
    })
  })

  it('keeps the unapproved Activity Template catalog honest and filters on the server', async () => {
    vi.mocked(fetch).mockImplementation(async (input) => {
      const url = String(input)
      if (url.includes('/activity-candidates')) return json([candidate])
      return json(view)
    })
    open()
    await screen.findAllByText('Interaktivni zadaci')

    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Small Talk' } })
    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([url]) =>
      String(url).includes('search=Small+Talk'))).toBe(true))
    fireEvent.click(screen.getByRole('tab', { name: 'Aktivnosti' }))
    expect(screen.getByRole('heading', { name: 'Nema objavljenih predložaka aktivnosti' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Dodaj u plan' })).toBeDisabled()
  })
})
