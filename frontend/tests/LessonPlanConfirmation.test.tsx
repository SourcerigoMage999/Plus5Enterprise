import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { LessonPlanConfirmationPage } from '../src/lessonPlanning/LessonPlanConfirmationPage.tsx'

const planId = '11111111-1111-1111-1111-111111111111'
const base = {
  lessonPlanId: planId, lessonPlanVersionId: '22222222-2222-2222-2222-222222222222', versionNumber: 1,
  status: 'Draft', planRowVersion: 'AAAAAAAAAAA=', versionRowVersion: 'AQAAAAAAAAA=',
  participantName: 'Petar Horvat', participantType: 'student', session: null,
  goal: 'Razlikovati Present Perfect i Past Simple.', targetDurationMinutes: 60, plannedDurationMinutes: 60,
  knowledgeComponents: [{ id: 'kc-1', name: 'Present Perfect', modelCode: 'ENG-G8', modelVersion: '2026' }],
  activities: [{ id: 'act-1', sortOrder: 0, name: 'Provjera razumijevanja', purpose: 'CheckForUnderstanding',
    workMode: 'Pair', durationMinutes: 60, assessableTaskCount: 0, materials: [] }],
  materials: [], balance: [{ code: 'Time', label: 'Vrijeme', status: 1, message: 'Trajanje odgovara cilju.' }],
  validationMessages: [], canConfirm: true, canEdit: true, canStartBoard: false,
}

function json(value: unknown) { return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } }) }
function open() { return render(<MemoryRouter initialEntries={[`/lesson-plans/${planId}/confirmation`]}><Routes><Route path="lesson-plans/:lessonPlanId/confirmation" element={<LessonPlanConfirmationPage />} /></Routes></MemoryRouter>) }

describe('confirmed lesson plan', () => {
  it('confirms the draft and exposes a truthful Ready snapshot without starting Board', async () => {
    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input)
      if (url.endsWith('/auth/csrf')) return json({ token: 'csrf' })
      if (url.endsWith('/confirm') && init?.method === 'POST') return json({ ...base, status: 'Ready', canConfirm: false })
      return json(base)
    })
    open()
    fireEvent.click(await screen.findByRole('button', { name: 'Potvrdi pripremu' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Sat je spreman' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: /Pokreni sat na PLUS 5 Ploči/ })).toBeDisabled()
    expect(screen.getByText(/Nema jedinstvene ukupne procjene/)).toBeInTheDocument()
    const write = vi.mocked(fetch).mock.calls.find(([url]) => String(url).endsWith('/confirm'))
    expect(JSON.parse(String(write?.[1]?.body))).toEqual({ planRowVersion: base.planRowVersion, versionRowVersion: base.versionRowVersion })
  })

  it('shows blocking validation without fabricating readiness', async () => {
    vi.mocked(fetch).mockResolvedValue(json({ ...base, activities: [], plannedDurationMinutes: 0,
      validationMessages: ['Dodajte najmanje jednu aktivnost.'], canConfirm: false }))
    open()
    expect(await screen.findByText('Dodajte najmanje jednu aktivnost.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Potvrdi pripremu' })).toBeDisabled()
    expect(screen.queryByText('68%')).not.toBeInTheDocument()
  })
})
