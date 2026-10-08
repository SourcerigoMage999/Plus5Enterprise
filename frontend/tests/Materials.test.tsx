import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

Object.defineProperties(HTMLDialogElement.prototype, {
  showModal: { configurable: true, value() { this.setAttribute('open', '') } },
  close: { configurable: true, value() { this.removeAttribute('open') } },
})

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-10-01T12:00:00Z',
}

const material = {
  id: 'material-1',
  versionId: 'version-1',
  title: 'Present Perfect worksheet',
  description: 'Practice material',
  materialTypeCode: 'WORKSHEET',
  subject: 'Grammar',
  program: { id: 'program-1', name: 'English 8', code: null },
  schoolGrade: { id: 'grade-1', name: '8. razred', code: '8R' },
  proficiencyLevel: { id: 'level-1', name: 'B1', code: 'B1' },
  fileFormat: 'pdf',
  addedAtUtc: '2026-09-30T08:00:00Z',
  isOwner: true,
  shareAccess: null,
  tags: ['present perfect'],
}

const page = {
  items: [material],
  page: 1,
  pageSize: 24,
  totalCount: 1,
  totalPages: 1,
}

const overview = {
  subjects: ['Grammar'],
  programs: [{ id: 'program-1', name: 'English 8', code: null }],
  schoolGrades: [{ id: 'grade-1', name: '8. razred', code: '8R' }],
  materialTypes: ['WORKSHEET'],
  tags: ['present perfect'],
  materialTypeCounts: [{ code: 'WORKSHEET', count: 1 }],
  recentlyAdded: [{ id: 'material-1', title: 'Present Perfect worksheet', addedAtUtc: '2026-09-30T08:00:00Z' }],
}

const detail = {
  ...material,
  versionNumber: 1,
  languageCode: 'hr-HR',
  learningGoal: 'Učenik razlikuje i pravilno primjenjuje Present Perfect.',
  file: { format: 'pdf', originalFileName: 'present-perfect.pdf', mediaType: 'application/pdf', sizeBytes: 2048 },
  knowledgeComponents: [{ id: 'component-1', name: 'Present Perfect', knowledgeAreaName: 'Grammar', knowledgeModelCode: 'PLUS5-EN', knowledgeModelVersion: '2026', knowledgeModelStatus: 'published' }],
  curriculumOutcomes: [{ id: 'outcome-1', officialCode: 'ENG.8.1', title: 'Primjenjuje Present Perfect u kontekstu.', curriculumCode: 'ENG-8', curriculumName: 'Nacionalni kurikulum', curriculumVersion: '2026' }],
  tasks: [{ id: 'task-1', versionId: 'task-version-1', versionNumber: 1, sortOrder: 0, prompt: 'I ____ London twice.', taskTypeCode: 'SINGLE_CHOICE', difficulty: 1, evidenceType: 'recognition', correctAnswer: 'B – have visited', evaluationCriterion: null, maxPoints: 1, knowledgeComponents: [{ id: 'component-1', name: 'Present Perfect', knowledgeAreaName: 'Grammar', knowledgeModelCode: 'PLUS5-EN', knowledgeModelVersion: '2026', knowledgeModelStatus: 'published' }] }],
}

const editWorkspace = {
  materialId: 'material-1',
  rowVersion: 'AQIDBA==',
  visibility: 'private',
  currentVersionId: 'version-1',
  editableVersion: {
    id: 'version-1', versionNumber: 1, status: 'active', title: material.title,
    description: detail.description, materialTypeCode: material.materialTypeCode,
    subject: material.subject, languageCode: 'EN', programId: 'program-1', schoolGradeId: 'grade-1',
    proficiencyLevelId: 'level-1', learningGoal: detail.learningGoal, file: detail.file,
    tags: material.tags, knowledgeComponentIds: ['component-1'], curriculumOutcomeIds: ['outcome-1'],
    assessableTaskCount: 1,
  },
  history: [{ id: 'version-1', versionNumber: 1, status: 'active', title: material.title, createdAtUtc: material.addedAtUtc, activatedAtUtc: material.addedAtUtc, supersededAtUtc: null, isCurrent: true }],
  options: {
    programs: overview.programs, schoolGrades: overview.schoolGrades,
    proficiencyLevels: [{ id: 'level-1', name: 'B1', code: 'B1' }],
    knowledgeComponents: detail.knowledgeComponents,
    curriculumOutcomes: detail.curriculumOutcomes,
    materialTypeCodes: ['WORKSHEET', 'PRESENTATION'], languageCodes: ['EN'],
  },
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderMaterials(initialEntry = '/materials') {
  const router = createMemoryRouter([
    { path: '*', element: <AuthProvider><AppRoutes /></AuthProvider> },
  ], { initialEntries: [initialEntry] })
  return render(<RouterProvider router={router} />)
}

describe('material library', () => {
  it('renders actual owner materials, facets and honest future-action boundaries', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()

    expect(await screen.findByRole('heading', { level: 1, name: '4.1 Biblioteka materijala' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: material.title })).toBeInTheDocument()
    expect(screen.getByText('English 8 · 8R')).toBeInTheDocument()
    expect(screen.getAllByText('present perfect')).toHaveLength(2)
    expect(within(screen.getByRole('complementary', { name: 'Filtri i pregled materijala' })).getAllByText('Radni list')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Novi materijal' })).toHaveAttribute('href', '/materials/import')
    expect(screen.getByRole('button', { name: `Akcije za ${material.title}` })).toBeDisabled()
  })

  it('switches to server-authorized shared scope and shows its empty state', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))
      .mockResolvedValueOnce(json({ ...page, items: [], totalCount: 0, totalPages: 0 }))
      .mockResolvedValueOnce(json({ ...overview, materialTypeCounts: [], recentlyAdded: [] }))

    renderMaterials()
    await screen.findByRole('heading', { name: material.title })
    fireEvent.click(screen.getByRole('tab', { name: 'Dijeljeni sa mnom' }))

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('/materials?page=1&pageSize=24&ownership=2&sort=1'))).toBe(true))
    expect(await screen.findByRole('heading', { name: 'Nema materijala dijeljenih s vama' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Dijeljeni sa mnom' })).toHaveAttribute('aria-selected', 'true')
  })

  it('keeps structured filters in URL state and sends them to the API', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))
      .mockResolvedValueOnce(json(page))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()
    await screen.findByRole('heading', { name: material.title })
    fireEvent.change(screen.getByLabelText('Predmet'), { target: { value: 'Grammar' } })

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('subject=Grammar'))).toBe(true))
    expect(screen.getByLabelText('Predmet')).toHaveValue('Grammar')
    expect(screen.getByRole('button', { name: 'Poništi sve' })).toBeInTheDocument()
  })

  it('shows a recoverable error state', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ code: 'request_failed' }, 503))
      .mockResolvedValueOnce(json(overview))

    renderMaterials()

    expect(await screen.findByRole('heading', { name: 'Biblioteka nije dostupna' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pokušaj ponovno' })).toBeInTheDocument()
  })
})

describe('material detail', () => {
  it('renders the authorized current-version metadata and mapping snapshot', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(detail))

    renderMaterials('/materials/material-1')

    expect(await screen.findByRole('heading', { level: 1, name: '4.2 Pregled materijala' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: material.title })).toBeInTheDocument()
    expect(screen.getByText(detail.learningGoal)).toBeInTheDocument()
    expect(screen.getByText('Present Perfect')).toBeInTheDocument()
    expect(screen.getByText('ENG.8.1')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Otvori' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Preuzmi' })).toBeDisabled()
    expect(screen.getByText(/Sam materijal nije automatski dokaz znanja/)).toBeInTheDocument()
    expect(screen.getByText('I ____ London twice.')).toBeInTheDocument()
    expect(screen.getByText('Težina 1/5')).toBeInTheDocument()
    expect(screen.getByText('Prepoznavanje')).toBeInTheDocument()
    expect(screen.getByText('B – have visited')).toBeInTheDocument()
  })

  it('shows the same safe message for missing or inaccessible material', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({}, 404))

    renderMaterials('/materials/foreign-material')

    expect(await screen.findByText('Materijal nije pronađen ili mu nemate pristup.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Povratak na materijale' })).toHaveAttribute('href', '/materials')
  })

  it('keeps the detail error recoverable', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({}, 503))
      .mockResolvedValueOnce(json(detail))

    renderMaterials('/materials/material-1')
    fireEvent.click(await screen.findByRole('button', { name: 'Pokušaj ponovno' }))

    expect(await screen.findByRole('heading', { name: material.title })).toBeInTheDocument()
  })
})

describe('material import', () => {
  it('guides the teacher through file, metadata, mapping and review without AI dependency', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({
        programs: overview.programs,
        schoolGrades: overview.schoolGrades,
        proficiencyLevels: [{ id: 'level-1', name: 'B1', code: 'B1' }],
        knowledgeComponents: detail.knowledgeComponents.map((item) => ({
          id: item.id,
          name: item.name,
          knowledgeAreaName: item.knowledgeAreaName,
          knowledgeModelCode: item.knowledgeModelCode,
          knowledgeModelVersion: item.knowledgeModelVersion,
        })),
        curriculumOutcomes: detail.curriculumOutcomes.map((item) => ({
          id: item.id,
          title: item.title,
          officialCode: item.officialCode,
          curriculumCode: item.curriculumCode,
          curriculumVersion: item.curriculumVersion,
        })),
        materialTypeCodes: ['WORKSHEET'],
        languageCodes: ['EN'],
      }))

    renderMaterials('/materials/import')

    expect(await screen.findByRole('heading', { level: 1, name: '4.4 Uvoz vlastitog materijala' })).toBeInTheDocument()
    const input = screen.getByLabelText(/Povucite datoteku ovdje/)
    fireEvent.change(input, { target: { files: [new File(['%PDF-1.7\n'], 'lesson.pdf', { type: 'application/pdf' })] } })
    fireEvent.click(screen.getByRole('button', { name: /Nastavi/ }))

    await screen.findByRole('heading', { name: 'Osnovni podaci' })
    fireEvent.change(screen.getByLabelText(/Vrsta materijala/), { target: { value: 'WORKSHEET' } })
    fireEvent.click(screen.getByRole('button', { name: /Nastavi/ }))

    expect(await screen.findByRole('heading', { name: 'Cilj i pedagoško mapiranje' })).toBeInTheDocument()
    fireEvent.click(screen.getAllByRole('checkbox', { name: /Present Perfect/ })[0])
    fireEvent.click(screen.getByRole('button', { name: /Nastavi/ }))

    expect(await screen.findByRole('heading', { name: 'Pregled prije uvoza' })).toBeInTheDocument()
    expect(screen.getByText('lesson.pdf · 1 KB')).toBeInTheDocument()
    expect(screen.getByText('Present Perfect')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Potvrdi i uvezi/ })).toBeEnabled()
    expect(vi.mocked(fetch)).toHaveBeenCalledTimes(2)
  })
})

describe('material editing and version history', () => {
  it('renders the owner-only metadata editor without simulating a slide editor', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(editWorkspace))

    renderMaterials('/materials/material-1/edit')

    expect(await screen.findByRole('heading', { level: 1, name: '4.5 Uredi materijal' })).toBeInTheDocument()
    expect(screen.getByDisplayValue(material.title)).toBeInTheDocument()
    expect(screen.getByText(/Uređivač slajdova i sadržaja pripada Phase 7/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Objavi novu verziju/ })).toBeDisabled()
    expect(screen.getByText(/Vidljivost:/)).toBeInTheDocument()
  })

  it('opens immutable history and describes restore as a new draft', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(editWorkspace))
      .mockResolvedValueOnce(json(editWorkspace.editableVersion))

    renderMaterials('/materials/material-1/edit')
    fireEvent.click(await screen.findByRole('button', { name: /Povijest/ }))
    fireEvent.click(screen.getByRole('button', { name: new RegExp(`v1.*${material.title}`, 's') }))

    expect(await screen.findByRole('heading', { name: 'Verzija 1' })).toBeInTheDocument()
    expect(screen.getByText(/Vraćanje stvara novu skicu/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Vrati kao novu skicu/ })).toBeDisabled()
  })
})
