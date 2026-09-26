import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = { email: 'teacher@example.test', accountType: 'Teacher', expiresAtUtc: '2026-09-27T12:00:00Z' }
const component = {
  knowledgeComponentId: 'component-1', parentKnowledgeComponentId: null, name: 'Past Simple', sortOrder: 1,
  status: 'Active', score: 0.81, confidence: 'High', readiness: 'Ready', evidenceCount: 3,
  effectiveEvidenceWeight: 2.4, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1',
}
const childComponent = {
  ...component, knowledgeComponentId: 'component-1-1', parentKnowledgeComponentId: 'component-1', name: 'Irregular verbs',
  score: 0.48, confidence: 'Medium', readiness: 'Developing', evidenceCount: 2, effectiveEvidenceWeight: 1.25,
}
const snapshot = {
  studentId: 'student-1', firstName: 'Ana', lastName: 'Anić', schoolGradeName: 'Sedmi razred',
  schoolGradeCode: '7R', schoolName: 'OŠ Plus 5', programName: 'Grammar Focus', groupName: 'Grupa 7A',
  models: [{
    knowledgeModelId: 'model-1', code: 'ENGLISH-7', version: '2026.1', status: 'Published',
    areas: [
      { knowledgeAreaId: 'area-1', name: 'Grammar', sortOrder: 1, score: 0.74, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.4, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [component, childComponent] },
      { knowledgeAreaId: 'area-2', name: 'Listening', sortOrder: 2, score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [{ ...component, knowledgeComponentId: 'component-2', name: 'Main idea', score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0 }] },
    ],
  }],
}

function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function renderKnowledge() { return render(<MemoryRouter initialEntries={['/students/student-1/knowledge']}><AuthProvider><AppRoutes /></AuthProvider></MemoryRouter>) }

describe('student knowledge detail', () => {
  it('renders real component projections and switches model-defined areas', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Detalj znanja učenika' })).toBeInTheDocument()
    expect(screen.getByText(/OŠ Plus 5/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Past Simple' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getAllByText('81 %')).toHaveLength(2)
    expect(screen.getAllByText('Visoka').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('Objavljen')).toBeInTheDocument()
    expect(screen.getByText(/ne izračunava ocjenu, trend ni preporuke/i)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Past Simple' })).toBeInTheDocument()
    expect(screen.getByText('Grammar › Past Simple')).toBeInTheDocument()
    expect(screen.getByText('ENGLISH-7 v2026.1')).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Podređene komponente' })).toBeInTheDocument()

    fireEvent.click(screen.getAllByRole('button', { name: 'Irregular verbs' })[0])
    expect(screen.getByRole('heading', { name: 'Irregular verbs' })).toBeInTheDocument()
    expect(screen.getByText('Grammar › Past Simple › Irregular verbs')).toBeInTheDocument()
    expect(screen.getByText(/Pojedinačne aktivnosti i trend nisu prikazani/)).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: /Listening/ }))
    const row = screen.getByRole('button', { name: 'Main idea' }).closest('tr')
    expect(row).not.toBeNull()
    expect(within(row!).getByText('—')).toBeInTheDocument()
    expect(within(row!).getByText('Nedovoljno podataka')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Natrag na procjenu/ })).toHaveAttribute('href', '/students/student-1/readiness')
  })

  it('shows no-data honestly without a synthetic percentage', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ ...snapshot, models: [] }))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Nema dovoljno podataka za detalj znanja' })).toBeInTheDocument()
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
  })

  it('keeps missing and foreign students indistinguishable', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ code: 'not_found' }, 404))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Učenik nije pronađen' })).toBeInTheDocument()
    expect(screen.getByText(/ne postoji, arhiviran je ili nije dio vašeg računa/)).toBeInTheDocument()
  })
})
