import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonActivityDetailPage } from '../src/lessonPlanning/LessonActivityDetailPage.tsx'

const planId = '11111111-1111-1111-1111-111111111111'
const activityId = '22222222-2222-2222-2222-222222222222'
const detail = {
  lessonPlanId: planId, lessonPlanVersionId: '33333333-3333-3333-3333-333333333333', rowVersion: 'AAAAAAAAAAA=',
  participantName: 'Petar Horvat', participantType: 'student', session: null,
  goal: 'Primijeniti Present Perfect.', targetDurationMinutes: 60, activityId, sortOrder: 1,
  name: 'Mini dijalog', purpose: 'Practice', durationMinutes: 15, workMode: 'Pair',
  description: 'Rad u paru.', purposeDescription: 'Primjena jezika.', teacherInstructions: 'Podijeli uloge.',
  studentInstructions: 'Izvedite dijalog.', notes: 'Privatna bilješka.',
  showTeacherInstructionsOnBoard: true, showStudentInstructionsOnBoard: true, showProgressOnBoard: false,
  knowledgeBlockVersionId: '44444444-4444-4444-4444-444444444444', knowledgeBlockCode: 'KB-005', knowledgeBlockName: 'Interaktivni zadaci',
  availableKnowledgeComponents: [{ id: 'kc-1', name: 'Present Perfect', modelCode: 'ENG-G8', modelVersion: '2026' }],
  selectedKnowledgeComponentIds: ['kc-1'], materials: [],
  aiStatus: { enabled: false, paidExternalService: true, automaticCalls: false, inAppBilling: false },
}

function json(value: unknown) { return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } }) }
function open() { return render(<MemoryRouter initialEntries={[`/lesson-plans/${planId}/activities/${activityId}`]}><Routes><Route path="lesson-plans/:lessonPlanId/activities/:activityId" element={<LessonActivityDetailPage />} /></Routes></MemoryRouter>) }

describe('lesson activity detail editing', () => {
  it('renders the canonical detail fields and keeps paid AI off by default', async () => {
    vi.mocked(fetch).mockResolvedValue(json(detail))
    open()
    expect(await screen.findByRole('heading', { name: 'Mini dijalog' })).toBeInTheDocument()
    expect(screen.getByDisplayValue('Privatna bilješka.')).toBeInTheDocument()
    expect(screen.getByText(/AI pomoć je isključena/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Generiraj prijedlog' })).not.toBeInTheDocument()
    expect(screen.getByText(/Bilješke se nikad ne šalju AI provideru/)).toBeInTheDocument()
  })

  it('saves manual fields with optimistic concurrency and blocks duplicate while dirty', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      if (String(input).endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (init?.method === 'PUT') return json({ ...detail, name: 'Novi naziv', rowVersion: 'AQAAAAAAAAA=' })
      return json(detail)
    })
    open()
    const name = await screen.findByDisplayValue('Mini dijalog')
    fireEvent.change(name, { target: { value: 'Novi naziv' } })
    expect(screen.getByRole('button', { name: 'Dupliciraj' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Spremi' }))
    await waitFor(() => expect(screen.getByDisplayValue('Novi naziv')).toBeInTheDocument())
    const write = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(JSON.parse(String(write?.[1]?.body)).rowVersion).toBe(detail.rowVersion)
  })
})
