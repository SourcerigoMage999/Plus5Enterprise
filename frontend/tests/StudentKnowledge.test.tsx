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
const vocabularyComponent = {
  ...component, knowledgeComponentId: 'vocabulary-1', parentKnowledgeComponentId: null, name: 'Hobbies & Free Time',
  score: 0.86, confidence: 'High', readiness: 'Ready', evidenceCount: 4, effectiveEvidenceWeight: 3.1,
}
const vocabularyChild = {
  ...vocabularyComponent, knowledgeComponentId: 'vocabulary-1-1', parentKnowledgeComponentId: 'vocabulary-1', name: 'Equipment',
  score: 0.72, confidence: 'Medium', readiness: 'Developing', evidenceCount: 2, effectiveEvidenceWeight: 1.6,
}
const vocabularyLeaf = {
  ...vocabularyChild, knowledgeComponentId: 'vocabulary-1-1-1', parentKnowledgeComponentId: 'vocabulary-1-1', name: 'Helmet',
  score: 0.67, confidence: 'Medium', readiness: 'Developing', evidenceCount: 2, effectiveEvidenceWeight: 1.4,
}
const readingComponent = {
  ...component, knowledgeComponentId: 'reading-1', parentKnowledgeComponentId: null, name: 'Main Idea',
  score: 0.62, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.2,
}
const readingChild = {
  ...readingComponent, knowledgeComponentId: 'reading-1-1', parentKnowledgeComponentId: 'reading-1',
  name: 'Razlikovanje glavne ideje od detalja', score: 0.57, evidenceCount: 2, effectiveEvidenceWeight: 1.4,
}
const readingNoData = {
  ...readingComponent, knowledgeComponentId: 'reading-2', parentKnowledgeComponentId: null, name: 'Inference',
  score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0,
}
const listeningComponent = {
  ...component, knowledgeComponentId: 'listening-1', parentKnowledgeComponentId: null, name: 'Main Idea',
  score: 0.58, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2,
}
const listeningChild = {
  ...listeningComponent, knowledgeComponentId: 'listening-1-1', parentKnowledgeComponentId: 'listening-1',
  name: 'Glavna ideja u duljem audiozapisu', score: 0.49, evidenceCount: 2, effectiveEvidenceWeight: 1.2,
}
const listeningNoData = {
  ...listeningComponent, knowledgeComponentId: 'listening-2', parentKnowledgeComponentId: null, name: 'Listening Inference',
  score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0,
}
const speakingComponent = {
  ...component, knowledgeComponentId: 'speaking-1', parentKnowledgeComponentId: null, name: 'Fluentnost',
  score: 0.52, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.1,
}
const speakingChild = {
  ...speakingComponent, knowledgeComponentId: 'speaking-1-1', parentKnowledgeComponentId: 'speaking-1',
  name: 'Smanjenje dugih pauza', score: 0.46, evidenceCount: 2, effectiveEvidenceWeight: 1.3,
}
const speakingNoData = {
  ...speakingComponent, knowledgeComponentId: 'speaking-2', parentKnowledgeComponentId: null, name: 'Samostalnost u govoru',
  score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0,
}
const snapshot = {
  studentId: 'student-1', firstName: 'Ana', lastName: 'Anić', schoolGradeName: 'Sedmi razred',
  schoolGradeCode: '7R', schoolName: 'OŠ Plus 5', programName: 'Grammar Focus', groupName: 'Grupa 7A',
  models: [{
    knowledgeModelId: 'model-1', code: 'ENGLISH-7', version: '2026.1', status: 'Published',
    areas: [
      { knowledgeAreaId: 'area-1', name: 'Grammar', sortOrder: 1, score: 0.74, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.4, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [component, childComponent] },
      { knowledgeAreaId: 'area-2', name: 'Vocabulary', sortOrder: 2, score: 0.82, confidence: 'High', readiness: 'Ready', evidenceCount: 4, effectiveEvidenceWeight: 3.1, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [vocabularyComponent, vocabularyChild, vocabularyLeaf] },
      { knowledgeAreaId: 'area-3', name: 'Reading', sortOrder: 3, score: 0.62, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.2, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [readingComponent, readingChild, readingNoData] },
      { knowledgeAreaId: 'area-4', name: 'Listening', sortOrder: 4, score: 0.58, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [listeningComponent, listeningChild, listeningNoData] },
      { knowledgeAreaId: 'area-5', name: 'Speaking', sortOrder: 5, score: 0.52, confidence: 'Medium', readiness: 'Developing', evidenceCount: 3, effectiveEvidenceWeight: 2.1, calculatedAtUtc: '2026-09-26T10:00:00Z', algorithmVersion: 'readiness-v1', components: [speakingComponent, speakingChild, speakingNoData] },
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
    const row = screen.getByRole('button', { name: 'Listening Inference' }).closest('tr')
    expect(row).not.toBeNull()
    expect(within(row!).getByText('—')).toBeInTheDocument()
    expect(within(row!).getByText('Nedovoljno podataka')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Natrag na procjenu/ })).toHaveAttribute('href', '/students/student-1/readiness')
  })

  it('uses the model hierarchy for Vocabulary topics and arbitrary-depth drill-down', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Detalj znanja učenika' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: /Vocabulary/ }))

    expect(screen.getByRole('button', { name: 'Hobbies & Free Time' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('heading', { name: 'Hobbies & Free Time' })).toBeInTheDocument()
    expect(screen.getByText('Vocabulary › Hobbies & Free Time')).toBeInTheDocument()

    const childRegion = screen.getByRole('region', { name: 'Podređene komponente' })
    fireEvent.click(within(childRegion).getByRole('button', { name: /Equipment/ }))
    expect(screen.getByRole('heading', { name: 'Equipment' })).toBeInTheDocument()
    expect(screen.getByText('Vocabulary › Hobbies & Free Time › Equipment')).toBeInTheDocument()

    fireEvent.click(within(screen.getByRole('region', { name: 'Podređene komponente' })).getByRole('button', { name: /Helmet/ }))
    expect(screen.getByRole('heading', { name: 'Helmet' })).toBeInTheDocument()
    expect(screen.getByText('Vocabulary › Hobbies & Free Time › Equipment › Helmet')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Podređene komponente' })).not.toBeInTheDocument()
  })

  it('renders Reading as model-defined skills and preserves component no-data', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Detalj znanja učenika' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: /Reading/ }))

    expect(screen.getByRole('button', { name: 'Main Idea' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('heading', { name: 'Main Idea' })).toBeInTheDocument()
    expect(screen.getByText('Reading › Main Idea')).toBeInTheDocument()

    const childRegion = screen.getByRole('region', { name: 'Podređene komponente' })
    fireEvent.click(within(childRegion).getByRole('button', { name: /Razlikovanje glavne ideje od detalja/ }))
    expect(screen.getByText('Reading › Main Idea › Razlikovanje glavne ideje od detalja')).toBeInTheDocument()

    const inferenceButton = screen.getByRole('button', { name: 'Inference' })
    fireEvent.click(inferenceButton)
    const inferenceRow = inferenceButton.closest('tr')
    expect(inferenceRow).not.toBeNull()
    expect(within(inferenceRow!).getByText('—')).toBeInTheDocument()
    expect(within(inferenceRow!).getByText('Nedovoljno podataka')).toBeInTheDocument()
    const detail = screen.getByRole('heading', { name: 'Inference' }).closest('aside')
    expect(detail).not.toBeNull()
    expect(within(detail!).getByText('Nema dovoljno podataka za rezultat')).toBeInTheDocument()
    expect(within(detail!).queryByText('0 %')).not.toBeInTheDocument()
  })

  it('keeps Listening skills distinct and does not invent audio context metadata', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Detalj znanja učenika' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: /Listening/ }))

    expect(screen.getByRole('button', { name: 'Main Idea' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('heading', { name: 'Main Idea' })).toBeInTheDocument()
    expect(screen.getByText('Listening › Main Idea')).toBeInTheDocument()

    fireEvent.click(within(screen.getByRole('region', { name: 'Podređene komponente' })).getByRole('button', { name: /Glavna ideja u duljem audiozapisu/ }))
    expect(screen.getByText('Listening › Main Idea › Glavna ideja u duljem audiozapisu')).toBeInTheDocument()

    const inferenceButton = screen.getByRole('button', { name: 'Listening Inference' })
    fireEvent.click(inferenceButton)
    const detail = screen.getByRole('heading', { name: 'Listening Inference' }).closest('aside')
    expect(detail).not.toBeNull()
    expect(within(detail!).getByText('Nema dovoljno podataka za rezultat')).toBeInTheDocument()
    expect(within(detail!).queryByText('0 %')).not.toBeInTheDocument()
    expect(screen.queryByText(/broj slušanja|brzina govora|broj govornika/i)).not.toBeInTheDocument()
  })

  it('renders Speaking from observable model skills without a synthetic assessment write', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderKnowledge()

    expect(await screen.findByRole('heading', { name: 'Detalj znanja učenika' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: /Speaking/ }))

    expect(screen.getByRole('button', { name: 'Fluentnost' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('heading', { name: 'Fluentnost' })).toBeInTheDocument()
    expect(screen.getByText('Speaking › Fluentnost')).toBeInTheDocument()

    fireEvent.click(within(screen.getByRole('region', { name: 'Podređene komponente' })).getByRole('button', { name: /Smanjenje dugih pauza/ }))
    expect(screen.getByText('Speaking › Fluentnost › Smanjenje dugih pauza')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Samostalnost u govoru' }))
    const detail = screen.getByRole('heading', { name: 'Samostalnost u govoru' }).closest('aside')
    expect(detail).not.toBeNull()
    expect(within(detail!).getByText('Nema dovoljno podataka za rezultat')).toBeInTheDocument()
    expect(within(detail!).queryByText('0 %')).not.toBeInTheDocument()
    expect(screen.queryByText('Samopouzdanje')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Odlično|Dobro|Potrebno uvježbati|Potrebna pomoć/ })).not.toBeInTheDocument()
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
