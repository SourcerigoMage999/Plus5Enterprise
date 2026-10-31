import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonGoalSelectionPage } from '../src/lessonPlanning/LessonGoalSelectionPage.tsx'

const participant = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Petar Horvat',
  schoolGrade: '8. razred',
  programName: 'Grammar Focus',
  memberCount: null,
}
const component = {
  id: '22222222-2222-2222-2222-222222222222',
  name: 'Present Perfect — use',
  areaName: 'Grammar',
  modelCode: 'ENG-G8',
  modelVersion: '2026',
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } })
}

describe('lesson goal selection', () => {
  it('uses real selections, keeps AI honest and creates a Draft goal', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.includes('/lesson-plans/participants')) return json({ items: [participant], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 })
      if (url.includes('/lesson-plans/knowledge-components')) return json({ items: [component], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 })
      if (url.includes('/lesson-plans/sessions')) return json([])
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf-token' })
      if (url.endsWith('/lesson-plans/') && init?.method === 'POST') return json({ lessonPlanId: 'plan-1', lessonPlanVersionId: 'version-1' }, 201)
      return json({ code: 'request_failed' }, 404)
    })
    render(<MemoryRouter><LessonGoalSelectionPage /></MemoryRouter>)

    expect(await screen.findByRole('heading', { level: 1, name: /Cilj sata/ })).toBeInTheDocument()
    expect(screen.getByText(/AI prijedlog cilja sata nije uključen/)).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('option', { name: /Petar Horvat/ })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Odaberite učenika'), { target: { value: participant.id } })
    fireEvent.change(screen.getByLabelText(/Unesite vlastiti cilj sata/), { target: { value: 'Razlikovati vremena.' } })
    fireEvent.click(await screen.findByLabelText(/Present Perfect — use/))
    fireEvent.submit(document.getElementById('lesson-goal-form')!)

    expect(await screen.findByText('Cilj sata je spremljen kao Draft.')).toBeInTheDocument()
    const post = vi.mocked(fetch).mock.calls.find(([url, init]) => String(url).endsWith('/lesson-plans/') && init?.method === 'POST')
    expect(post).toBeDefined()
    expect(JSON.parse(String(post?.[1]?.body))).toMatchObject({
      participantType: 'student',
      participantId: participant.id,
      sessionId: null,
      goal: 'Razlikovati vremena.',
      knowledgeComponentIds: [component.id],
    })
  })

  it('requires participant, goal and knowledge without sending a write', async () => {
    vi.mocked(fetch).mockImplementation(async (input) => {
      const url = String(input)
      if (url.includes('/lesson-plans/participants')) return json({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 })
      if (url.includes('/lesson-plans/knowledge-components')) return json({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 })
      return json([])
    })
    render(<MemoryRouter><LessonGoalSelectionPage /></MemoryRouter>)
    await screen.findByRole('heading', { level: 1, name: /Cilj sata/ })
    fireEvent.submit(document.getElementById('lesson-goal-form')!)
    expect(await screen.findByRole('alert')).toHaveTextContent('Odaberite učenika ili grupu')
    expect(vi.mocked(fetch).mock.calls.some(([url, init]) => String(url).endsWith('/lesson-plans/') && init?.method === 'POST')).toBe(false)
  })
})
