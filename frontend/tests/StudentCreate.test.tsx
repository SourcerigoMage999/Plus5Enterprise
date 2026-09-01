import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-09-02T12:00:00Z',
}

const options = {
  schoolGrades: [{ id: 'grade-1', name: 'Sedmi razred', code: '7R' }],
  programs: [{ id: 'program-1', name: 'Matematika 7', code: null }],
  groups: [],
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderCreate() {
  return render(
    <MemoryRouter initialEntries={['/students/new']}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('student creation', () => {
  it('creates a student without requiring a program and opens the phase boundary', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(options))
      .mockResolvedValueOnce(json({ token: 'csrf-token' }))
      .mockResolvedValueOnce(json({ id: 'student-1' }, 201))

    renderCreate()

    await screen.findByRole('heading', { level: 1, name: 'Novi učenik' })
    fireEvent.change(screen.getByLabelText('Ime *'), { target: { value: 'Ana' } })
    fireEvent.change(screen.getByLabelText('Prezime *'), { target: { value: 'Anić' } })
    fireEvent.change(screen.getByLabelText('Razred *'), { target: { value: 'grade-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Spremi učenika' }))

    expect(await screen.findByRole('heading', { name: 'Učenik je uspješno spremljen' }))
      .toBeInTheDocument()
    const createCall = vi.mocked(fetch).mock.calls.find(([input]) =>
      String(input).endsWith('/students') && (input as string) !== '/students')
    const request = createCall?.[1]
    expect(request?.method).toBe('POST')
    expect(new Headers(request?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token')
    expect(JSON.parse(String(request?.body))).toEqual(expect.objectContaining({
      firstName: 'Ana',
      lastName: 'Anić',
      schoolGradeId: 'grade-1',
      programId: null,
      deliveryMode: null,
      guardian: null,
    }))
  })

  it('loads eligible groups only after group delivery is selected', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(options))
      .mockResolvedValueOnce(json({
        ...options,
        groups: [{
          id: 'group-1', name: 'Grupa Orion', programId: 'program-1',
          activeMemberCount: 3, capacity: 6,
        }],
      }))

    renderCreate()
    await screen.findByRole('heading', { level: 1, name: 'Novi učenik' })
    fireEvent.change(screen.getByLabelText('Program'), { target: { value: 'program-1' } })

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('/students/create-options?programId=program-1'))).toBe(true))
    fireEvent.change(screen.getByLabelText('Način rada'), { target: { value: 'group' } })
    expect(await screen.findByRole('option', { name: 'Grupa Orion (3/6)' })).toBeInTheDocument()
  })

  it('blocks saving when the required school-grade catalog is empty', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ ...options, schoolGrades: [] }))

    renderCreate()

    expect(await screen.findByText(/Nije konfiguriran nijedan školski razred/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Spremi učenika' })).toBeDisabled()
  })
})
