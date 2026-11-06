import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonPlanEditorPage } from '../src/lessonPlanning/LessonPlanEditorPage.tsx'

const planId = '11111111-1111-1111-1111-111111111111'
const activityId = '22222222-2222-2222-2222-222222222222'
const versionId = '33333333-3333-3333-3333-333333333333'
const attachment = {
  materialId: '44444444-4444-4444-4444-444444444444', materialVersionId: versionId,
  versionNumber: 3, title: 'Present Perfect worksheet', materialTypeCode: 'WORKSHEET',
  subject: 'Grammar', isCurrentVersion: true, isOwner: true, matchingKnowledgeComponentCount: 1,
}
const view = {
  lessonPlanId: planId, lessonPlanVersionId: '55555555-5555-5555-5555-555555555555', rowVersion: 'AAAAAAAAAAA=',
  participantName: 'Petar Horvat', participantType: 'student', session: null,
  goal: 'Razlikovati Present Perfect i Past Simple.', targetDurationMinutes: 60,
  knowledgeComponents: [{ id: 'kc-1', name: 'Present Perfect', modelCode: 'ENG-G8', modelVersion: '2026' }],
  activities: [{ id: activityId, sortOrder: 0, name: 'Aktivacija predznanja', purpose: 'ActivatePriorKnowledge',
    durationMinutes: 10, knowledgeBlockCode: 'KB-002', knowledgeComponentIds: ['kc-1'], materials: [] }],
  balance: [{ code: 'MaterialRelevance', label: 'Relevantnost materijala', status: 3,
    message: 'Materijali nisu obavezni; relevantnost će se prikazati nakon odabira.' }],
  attachedMaterials: [],
}

function json(value: unknown) {
  return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

function open() {
  return render(<MemoryRouter initialEntries={[`/lesson-plans/${planId}/editor`]}><Routes>
    <Route path="lesson-plans/:lessonPlanId/editor" element={<LessonPlanEditorPage />} />
  </Routes></MemoryRouter>)
}

describe('lesson plan editor material attachments', () => {
  it('selects and attaches an exact current material version with optimistic concurrency', async () => {
    const attachedView = {
      ...view,
      rowVersion: 'AQAAAAAAAAA=',
      activities: [{ ...view.activities[0], materials: [attachment] }],
      attachedMaterials: [attachment],
      balance: [{ code: 'MaterialRelevance', label: 'Relevantnost materijala', status: 1,
        message: 'Povezani materijali podudaraju se s komponentama znanja svojih aktivnosti.' }],
    }
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.includes('/material-candidates')) return json({ items: [{ ...attachment, isAttached: false }], page: 1, pageSize: 20, totalCount: 1 })
      if (url.endsWith(`/materials/${versionId}`) && init?.method === 'POST') return json(attachedView)
      return json(view)
    })
    open()

    expect(await screen.findByRole('heading', { name: 'Plan sata za Petar Horvat' })).toBeInTheDocument()
    expect(screen.getByText(/exact verzioniranih ciljeva/)).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: '+ Dodaj materijal' }))
    expect(await screen.findByText('Present Perfect worksheet')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Dodaj' }))

    await waitFor(() => expect(screen.getByText('v3 · 1 KC podudaranja')).toBeInTheDocument())
    const write = vi.mocked(fetch).mock.calls.find(([url, init]) =>
      String(url).endsWith(`/materials/${versionId}`) && init?.method === 'POST')
    expect(JSON.parse(String(write?.[1]?.body))).toEqual({ rowVersion: view.rowVersion })
    expect(screen.queryByText(/predviđena ocjena|AI preporuka/i)).not.toBeInTheDocument()
  })

  it('removes the exact attachment and keeps future phases visibly disabled', async () => {
    const attachedView = {
      ...view,
      activities: [{ ...view.activities[0], materials: [attachment] }],
      attachedMaterials: [attachment],
    }
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.endsWith(`/materials/${versionId}`) && init?.method === 'DELETE') return json(view)
      return json(attachedView)
    })
    open()
    fireEvent.click(await screen.findByRole('button', { name: 'Ukloni Present Perfect worksheet' }))
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Ukloni Present Perfect worksheet' })).not.toBeInTheDocument())
    expect(screen.getByRole('link', { name: /Dodaj aktivnost/ })).toHaveAttribute(
      'href', `/lesson-plans/${planId}/activity-selection`,
    )
    expect(screen.getByRole('button', { name: 'Spremi i nastavi' })).toBeDisabled()
  })
})
