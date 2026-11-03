import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonStructurePage } from '../src/lessonPlanning/LessonStructurePage.tsx'

const view = {
  lessonPlanId: '11111111-1111-1111-1111-111111111111', lessonPlanVersionId: '22222222-2222-2222-2222-222222222222',
  rowVersion: 'AAAAAAAAAAA=', participantName: 'Petar Horvat', participantType: 'student', goal: 'Razlikovati vremena.',
  targetDurationMinutes: 60, knowledgeComponents: [{ id: 'kc-1', name: 'Present Perfect', modelCode: 'ENG-G8', modelVersion: '2026' }],
  activities: [
    { id: '00000000-0000-0000-0000-000000000000', sortOrder: 0, name: 'Small Talk', purpose: 'Engagement', durationMinutes: 5, knowledgeBlockCode: 'KB-001', knowledgeBlockName: 'Small Talk', knowledgeComponentIds: [], warningCodes: [] },
    { id: '00000000-0000-0000-0000-000000000000', sortOrder: 1, name: 'Provjera razumijevanja', purpose: 'CheckForUnderstanding', durationMinutes: 10, knowledgeBlockCode: null, knowledgeBlockName: null, knowledgeComponentIds: ['kc-1'], warningCodes: [] },
  ],
  balance: [{ code: 'Goal', label: 'Cilj', status: 1, message: 'Plan ima valjan cilj sata.' }, { code: 'MaterialRelevance', label: 'Relevantnost materijala', status: 3, message: 'Nije procijenjeno.' }],
  materialCandidateCount: 2, isApplied: false, canApply: true, policyCode: 'SYSTEM_BASELINE_V1',
}

function json(value: unknown) { return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } }) }
function open() { return render(<MemoryRouter initialEntries={[`/lesson-plans/${view.lessonPlanId}/structure`]}><Routes><Route path="lesson-plans/:lessonPlanId/structure" element={<LessonStructurePage />} /></Routes></MemoryRouter>) }

describe('lesson structure', () => {
  it('renders deterministic no-cost preview and applies only on confirmation', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.endsWith('/apply') && init?.method === 'POST') return json({ ...view, isApplied: true, canApply: false })
      return json(view)
    })
    open()
    expect(await screen.findByRole('heading', { name: /Plan sata za Petar Horvat/ })).toBeInTheDocument()
    expect(screen.getByText(/Nema dodatne naplate/)).toBeInTheDocument()
    expect(screen.getByText('SYSTEM_BASELINE_V1', { exact: false })).toBeInTheDocument()
    expect(screen.getByText('Relevantnost materijala')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Primijeni prijedlog' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Prijedlog je primijenjen' })).toBeDisabled())
    const write = vi.mocked(fetch).mock.calls.find(([url, init]) => String(url).endsWith('/apply') && init?.method === 'POST')
    expect(JSON.parse(String(write?.[1]?.body))).toEqual({ rowVersion: view.rowVersion })
  })

  it('updates target duration with optimistic row version', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.endsWith('/target-duration') && init?.method === 'PUT') return json({ ...view, targetDurationMinutes: 45 })
      return json(view)
    })
    open()
    const input = await screen.findByRole('spinbutton', { name: 'Minute' })
    fireEvent.change(input, { target: { value: '45' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi trajanje' }))
    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([url, init]) => String(url).endsWith('/target-duration') && init?.method === 'PUT')).toBe(true))
  })
})
