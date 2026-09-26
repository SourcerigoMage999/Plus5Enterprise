import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = { email: 'teacher@example.test', accountType: 'Teacher', expiresAtUtc: '2026-09-27T12:00:00Z' }
const snapshot = {
  studentId: 'student-1', firstName: 'Ana', lastName: 'Anić',
  schoolGradeName: 'Sedmi razred', schoolGradeCode: '7R',
  areas: [
    { knowledgeAreaId: 'area-1', knowledgeModelCode: 'ENGLISH-7', knowledgeModelVersion: '2026.1', name: 'Grammar', sortOrder: 1, score: 0.72, confidence: 'Medium', readiness: 'Developing', evidenceCount: 2, effectiveEvidenceWeight: 2.5, calculatedAtUtc: '2026-09-26T08:30:00Z', algorithmVersion: 'readiness-v1' },
    { knowledgeAreaId: 'area-2', knowledgeModelCode: 'ENGLISH-7', knowledgeModelVersion: '2026.1', name: 'Listening', sortOrder: 2, score: null, confidence: 'NoData', readiness: 'InsufficientData', evidenceCount: 0, effectiveEvidenceWeight: 0, calculatedAtUtc: '2026-09-26T08:30:00Z', algorithmVersion: 'readiness-v1' },
  ],
}

function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function renderReadiness() { return render(<MemoryRouter initialEntries={['/students/student-1/readiness']}><AuthProvider><AppRoutes /></AuthProvider></MemoryRouter>) }

describe('student readiness', () => {
  it('renders projection score together with confidence, evidence and calculation time', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(snapshot))
    renderReadiness()

    expect(await screen.findByRole('heading', { level: 1, name: 'Ana Anić' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Grammar' })).toBeInTheDocument()
    expect(screen.getByText('72')).toBeInTheDocument()
    expect(screen.getByText('Srednja pouzdanost')).toBeInTheDocument()
    expect(screen.getByText('U razvoju')).toBeInTheDocument()
    expect(screen.getByText('Nema dovoljno podataka za izračun rezultata.')).toBeInTheDocument()
    expect(screen.getByText(/nije školska ocjena/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Natrag na dosje/ })).toHaveAttribute('href', '/students/student-1')
  })

  it('renders an honest empty state instead of a synthetic percentage', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ ...snapshot, areas: [] }))
    renderReadiness()

    expect(await screen.findByRole('heading', { name: 'Nema dovoljno podataka za procjenu spremnosti' })).toBeInTheDocument()
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
  })

  it('keeps missing and foreign students indistinguishable', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ code: 'not_found' }, 404))
    renderReadiness()

    expect(await screen.findByRole('heading', { name: 'Učenik nije pronađen' })).toBeInTheDocument()
    expect(screen.getByText(/ne postoji, arhiviran je ili nije dio vašeg računa/)).toBeInTheDocument()
  })
})
