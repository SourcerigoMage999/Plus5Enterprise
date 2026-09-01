import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = { email: 'teacher@example.test', accountType: 'Teacher', expiresAtUtc: '2026-09-03T12:00:00Z' }
const dossier = {
  id: 'student-1', firstName: 'Ana', lastName: 'Anić', nickname: 'Ani',
  dateOfBirth: '2013-04-12', schoolName: 'OŠ Plus', gender: 'Žensko',
  email: 'ana@example.test', phone: '+385 91 123 4567', status: 'active',
  schoolGrade: { id: 'grade-1', name: 'Sedmi razred', code: '7R' },
  program: { id: 'program-1', name: 'Engleski 7', code: null },
  deliveryMode: 'group', group: { id: 'group-1', name: 'Grupa Orion', code: null },
  primaryGuardian: { id: 'guardian-1', firstName: 'Iva', lastName: 'Anić', relationship: 'Majka', email: 'iva@example.test', phone: null },
  nextSession: { id: 'next-1', title: 'Present Perfect', startsAtUtc: '2026-09-04T14:00:00Z', endsAtUtc: '2026-09-04T15:00:00Z', timeZoneId: 'Europe/Zagreb', deliveryMode: 'group', group: { id: 'group-1', name: 'Grupa Orion', code: null } },
  lastHeldSession: null,
}

function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function renderDossier() { return render(<MemoryRouter initialEntries={['/students/student-1']}><AuthProvider><AppRoutes /></AuthProvider></MemoryRouter>) }

describe('student dossier', () => {
  it('renders stored core data and explicit neutral future states', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(dossier))
    renderDossier()
    expect(await screen.findByRole('heading', { level: 1, name: 'Ana Anić' })).toBeInTheDocument()
    expect(screen.getByText('Engleski 7')).toBeInTheDocument()
    expect(screen.getAllByText('Grupa Orion').length).toBeGreaterThan(0)
    expect(screen.getByText('Iva Anić')).toBeInTheDocument()
    expect(screen.getByText('Present Perfect')).toBeInTheDocument()
    expect(screen.getByText('Procjena još nije dostupna')).toBeInTheDocument()
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
    const actions = screen.getByRole('button', { name: /Poruka roditelju/ }).parentElement
    expect(actions).not.toBeNull()
    expect(within(actions!).getAllByRole('button').every((button) => button.hasAttribute('disabled'))).toBe(true)
    expect(within(actions!).getByRole('link', { name: /Uredi učenika/ })).toHaveAttribute('href', '/students/student-1/edit')
  })

  it('does not reveal whether a missing student belongs to another account', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json({ code: 'not_found' }, 404))
    renderDossier()
    expect(await screen.findByRole('heading', { name: 'Učenik nije pronađen' })).toBeInTheDocument()
    expect(screen.getByText(/ne postoji, arhiviran je ili nije dio vašeg računa/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Povratak na popis' })).toHaveAttribute('href', '/students')
  })
})
