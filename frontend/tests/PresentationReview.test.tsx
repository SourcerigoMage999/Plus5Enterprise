import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const materialId = '20000000-0000-0000-0000-000000000001'
const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-10-23T12:00:00Z',
}
const review = {
  materialId,
  materialVersionId: '30000000-0000-0000-0000-000000000001',
  materialVersionNumber: 2,
  materialRowVersion: 'AQIDBAUGBwg=',
  title: 'Present Continuous – pravila',
  description: 'Vizualno objašnjenje i kratka provjera.',
  subject: 'Engleski jezik',
  languageCode: 'hr',
  visibility: 'Private',
  programName: 'Engleski A2',
  schoolGradeName: '7. razred',
  proficiencyLevelName: 'A2',
  learningGoal: 'Razlikovati Present Simple i Present Continuous.',
  tags: ['gramatika', 'A2'],
  knowledgeComponents: [{
    id: '40000000-0000-0000-0000-000000000001',
    name: 'Present Continuous',
    knowledgeAreaName: 'Grammar',
    knowledgeModelCode: 'ENG-7',
    knowledgeModelVersion: '2026',
    knowledgeModelStatus: 'Published',
    knowledgeComponentStatus: 'Active',
  }],
  curriculumOutcomes: [{
    id: '50000000-0000-0000-0000-000000000001',
    officialCode: 'OŠ EJ A.7.1.',
    title: 'Razumije i koristi poznate gramatičke strukture.',
  }],
  quickCheckCount: 1,
  document: {
    documentSchemaVersion: 1,
    canvasWidth: 1600,
    canvasHeight: 900,
    rowVersion: 'CAcGBQQDAgE=',
    updatedAtUtc: '2026-10-23T08:00:00Z',
    slides: [{
      id: '60000000-0000-0000-0000-000000000001',
      sortOrder: 0,
      elements: [{
        id: '70000000-0000-0000-0000-000000000001',
        elementTypeCode: 'TEXT',
        elementSchemaVersion: 1,
        zIndex: 0,
        x: 160,
        y: 100,
        width: 1280,
        height: 180,
        payloadJson: JSON.stringify({ text: 'Present Continuous' }),
        asset: null,
        quickCheck: null,
      }],
    }],
  },
  publishBlockers: [],
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderReview() {
  const router = createMemoryRouter([{
    path: '*',
    element: <AuthProvider><AppRoutes /></AuthProvider>,
  }], { initialEntries: [`/materials/${materialId}/presentation/review`] })
  return render(<RouterProvider router={router} />)
}

describe('presentation review and save', () => {
  it('shows the real metadata, pedagogical links and presentation counts', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(review))

    renderReview()

    expect(await screen.findByRole('heading', { name: 'Pregled i spremanje' })).toBeInTheDocument()
    expect(screen.getAllByText(review.title).length).toBeGreaterThan(0)
    expect(screen.getByText('Present Continuous', { selector: 'li' })).toBeInTheDocument()
    expect(screen.getByText(/OŠ EJ A\.7\.1\./)).toBeInTheDocument()
    expect(screen.getAllByText('1', { selector: 'dt' })).toHaveLength(4)
    expect(screen.getByRole('button', { name: /Spremi materijal/ })).toBeEnabled()
  })

  it('publishes with both material and document revisions', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(review))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ published: true }))
      .mockResolvedValueOnce(json({}))

    renderReview()
    fireEvent.click(await screen.findByRole('button', { name: /Spremi materijal/ }))

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([, init]) =>
      init?.method === 'POST' && String(init.body).includes('expectedDocumentRowVersion'))).toBe(true))
    const publishCall = vi.mocked(fetch).mock.calls.find(([, init]) =>
      init?.method === 'POST' && String(init.body).includes('expectedDocumentRowVersion'))
    expect(JSON.parse(String(publishCall?.[1]?.body))).toEqual({
      materialVersionId: review.materialVersionId,
      expectedMaterialRowVersion: review.materialRowVersion,
      expectedDocumentRowVersion: review.document.rowVersion,
    })
  })
})
