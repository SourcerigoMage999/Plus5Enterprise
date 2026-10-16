import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-10-10T12:00:00Z',
}

const slideOne = { id: '10000000-0000-0000-0000-000000000001', sortOrder: 0, elements: [] }
const slideTwo = { id: '10000000-0000-0000-0000-000000000002', sortOrder: 1, elements: [] }
const workspace = {
  materialId: '20000000-0000-0000-0000-000000000001',
  materialVersionId: '30000000-0000-0000-0000-000000000001',
  materialVersionNumber: 2,
  title: 'Present Perfect – pravila',
  materialRowVersion: 'AQIDBAUGBwg=',
  document: {
    documentSchemaVersion: 1,
    canvasWidth: 1600,
    canvasHeight: 900,
    rowVersion: 'CAcGBQQDAgE=',
    updatedAtUtc: '2026-10-09T12:00:00Z',
    slides: [slideOne, slideTwo],
  },
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderEditor() {
  const router = createMemoryRouter([
    { path: '*', element: <AuthProvider><AppRoutes /></AuthProvider> },
  ], { initialEntries: [`/materials/${workspace.materialId}/presentation`] })
  return render(<RouterProvider router={router} />)
}

describe('presentation slide editor', () => {
  it('starts a version-bound blank document only after explicit teacher action', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ ...workspace, document: null }))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ slideId: slideOne.id }))
      .mockResolvedValueOnce(json({ ...workspace, document: { ...workspace.document, slides: [slideOne] } }))

    renderEditor()
    fireEvent.click(await screen.findByRole('button', { name: /Započni prezentaciju/ }))

    expect(await screen.findByRole('heading', { name: 'Prazan slajd' })).toBeInTheDocument()
    const createCall = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'POST')
    expect(JSON.parse(String(createCall?.[1]?.body))).toEqual({
      materialVersionId: workspace.materialVersionId,
      expectedMaterialRowVersion: workspace.materialRowVersion,
    })
  })

  it('renders canonical slide structure and adds an empty slide with document concurrency', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(workspace))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ slideId: 'slide-3' }))
      .mockResolvedValueOnce(json({
        ...workspace,
        document: {
          ...workspace.document,
          rowVersion: 'CQgHBgUEAwI=',
          slides: [...workspace.document.slides, { id: 'slide-3', sortOrder: 2, elements: [] }],
        },
      }))

    renderEditor()
    expect(await screen.findByRole('heading', { level: 1, name: '4.3 Novi materijal – Prezentacija' })).toBeInTheDocument()
    const slideList = screen.getByRole('complementary', { name: 'Slajdovi prezentacije' })
    expect(within(slideList).getAllByText('Prazan slajd')).toHaveLength(2)
    expect(screen.getByRole('button', { name: /Spremi nacrt/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Tema/ })).toBeDisabled()

    fireEvent.click(screen.getAllByRole('button', { name: /Dodaj slajd/ })[0])
    await waitFor(() => expect(within(slideList).getAllByText('Prazan slajd')).toHaveLength(3))
    const addCall = vi.mocked(fetch).mock.calls.find(([, init]) =>
      init?.method === 'POST' && String(init.body).includes('afterSlideId'))
    expect(JSON.parse(String(addCall?.[1]?.body))).toEqual({
      expectedDocumentRowVersion: workspace.document.rowVersion,
      afterSlideId: slideOne.id,
    })
  })

  it('reorders selected slides with the current document revision', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(workspace))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ slideId: null }))
      .mockResolvedValueOnce(json({
        ...workspace,
        document: { ...workspace.document, slides: [slideTwo, slideOne] },
      }))

    renderEditor()
    await screen.findByRole('heading', { name: 'Prazan slajd' })
    fireEvent.click(screen.getByRole('button', { name: 'Pomakni slajd dolje' }))

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([, init]) =>
      init?.method === 'PUT')).toBe(true))
    const reorderCall = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(JSON.parse(String(reorderCall?.[1]?.body))).toEqual({
      expectedDocumentRowVersion: workspace.document.rowVersion,
      orderedSlideIds: [slideTwo.id, slideOne.id],
    })
  })

  it('protects the required final slide in the UI', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({
        ...workspace,
        document: { ...workspace.document, slides: [slideOne] },
      }))
    renderEditor()
    expect(await screen.findByRole('button', { name: /Obriši odabrani slajd/ })).toBeDisabled()
  })

  it('creates a structured text element with the current document revision', async () => {
    const textElement = {
      id: '40000000-0000-0000-0000-000000000001',
      elementTypeCode: 'TEXT',
      elementSchemaVersion: 1,
      zIndex: 0,
      x: 160,
      y: 100,
      width: 1280,
      height: 180,
      payloadJson: JSON.stringify({ text: 'Novi tekst', fontSize: 52, fontWeight: 'bold', color: '#173E67', align: 'left' }),
      asset: null,
    }
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(workspace))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ elementId: textElement.id }))
      .mockResolvedValueOnce(json({
        ...workspace,
        document: {
          ...workspace.document,
          rowVersion: 'CQgHBgUEAwI=',
          slides: [{ ...slideOne, elements: [textElement] }, slideTwo],
        },
      }))

    renderEditor()
    fireEvent.click(await screen.findByRole('button', { name: 'Tekst' }))

    expect(await screen.findByDisplayValue('Novi tekst')).toBeInTheDocument()
    const createCall = vi.mocked(fetch).mock.calls.find(([, init]) =>
      init?.method === 'POST' && String(init.body).includes('elementTypeCode'))
    const body = JSON.parse(String(createCall?.[1]?.body))
    expect(body.expectedDocumentRowVersion).toBe(workspace.document.rowVersion)
    expect(body.elementTypeCode).toBe('TEXT')
    expect(JSON.parse(body.payloadJson)).toEqual({
      text: 'Novi tekst',
      fontSize: 52,
      fontWeight: 'bold',
      color: '#173E67',
      align: 'left',
    })
  })
})
