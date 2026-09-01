import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = { email: 'teacher@example.test', accountType: 'Teacher', expiresAtUtc: '2026-09-03T12:00:00Z' }
const options = {
  schoolGrades: [{ id: 'grade-1', name: 'Sedmi razred', code: '7R' }],
  programs: [{ id: 'program-1', name: 'Engleski 7', code: null }],
  groups: [{ id: 'group-1', name: 'Orion', programId: 'program-1', activeMemberCount: 3, capacity: 6 }],
}
const edit = {
  id: 'student-1', firstName: 'Ana', lastName: 'Anić', nickname: 'Ani',
  dateOfBirth: '2013-04-12', schoolName: 'OŠ Plus', gender: 'Žensko',
  email: 'ana@example.test', phone: null, schoolGradeId: 'grade-1',
  programId: 'program-1', deliveryMode: 'group', groupId: 'group-1', status: 'active',
  updatedAtUtc: '2026-09-02T08:00:00Z', rowVersion: 'AQID',
  guardians: [{ id: 'guardian-1', firstName: 'Iva', lastName: 'Anić', relationship: 'Majka', email: 'iva@example.test', phone: null, isPrimary: true }],
}
const dossier = {
  id: 'student-1', firstName: 'Anamarija', lastName: 'Anić', nickname: 'Ani',
  dateOfBirth: '2013-04-12', schoolName: 'OŠ Plus', gender: 'Žensko', email: 'ana@example.test', phone: null,
  status: 'active', schoolGrade: options.schoolGrades[0], program: options.programs[0], deliveryMode: 'group',
  group: { id: 'group-1', name: 'Orion', code: null }, primaryGuardian: null, nextSession: null, lastHeldSession: null,
}

function json(value: unknown, status = 200) { return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } }) }
function renderEdit() { return render(<MemoryRouter initialEntries={['/students/student-1/edit']}><AuthProvider><AppRoutes /></AuthProvider></MemoryRouter>) }

describe('student editing', () => {
  it('loads real data, saves with rowversion and returns to the dossier', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(edit))
      .mockResolvedValueOnce(json(options))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ rowVersion: 'BAUG' }))
      .mockResolvedValueOnce(json(dossier))

    renderEdit()
    await screen.findByRole('heading', { level: 1, name: '2.6 Uredi učenika' })
    fireEvent.change(screen.getAllByLabelText('Ime *')[0], { target: { value: 'Anamarija' } })
    fireEvent.click(screen.getAllByRole('button', { name: 'Spremi promjene' })[0])

    expect(await screen.findByRole('heading', { level: 1, name: 'Anamarija Anić' })).toBeInTheDocument()
    const updateCall = vi.mocked(fetch).mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(updateCall).toBeDefined()
    expect(new Headers(updateCall?.[1]?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token')
    expect(JSON.parse(String(updateCall?.[1]?.body))).toEqual(expect.objectContaining({ rowVersion: 'AQID', firstName: 'Anamarija', groupId: 'group-1' }))
  })

  it('shows an explicit archive confirmation and honest future boundaries', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(json(session)).mockResolvedValueOnce(json(edit)).mockResolvedValueOnce(json(options))
    renderEdit()
    await screen.findByRole('heading', { level: 1, name: '2.6 Uredi učenika' })
    expect(screen.getByText(/Procjenu znanja ne uređuje nastavnik/)).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Arhiviraj učenika' }))
    expect(await screen.findByRole('dialog', { name: 'Arhivirati učenika?' })).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText(/Podaci se ne brišu/)).toBeInTheDocument())
  })
})
