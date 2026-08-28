import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../src/app/AppRoutes.tsx'
import { AuthProvider } from '../src/auth/AuthContext.tsx'

const session = {
  email: 'teacher@example.test',
  accountType: 'Teacher',
  expiresAtUtc: '2026-08-29T12:00:00Z',
}

const overview = {
  totalCount: 1,
  activeCount: 1,
  onHoldCount: 0,
  inactiveCount: 0,
  withoutProgramCount: 0,
  programCounts: [{ programId: 'program-1', name: 'Matematika 7', studentCount: 1 }],
  programs: [{ id: 'program-1', name: 'Matematika 7', code: null }],
  schoolGrades: [{ id: 'grade-1', name: 'Sedmi razred', code: '7R' }],
}

const studentPage = {
  items: [{
    id: 'student-1',
    firstName: 'Ana',
    lastName: 'Anić',
    nickname: 'Ani',
    schoolGrade: { id: 'grade-1', name: 'Sedmi razred', code: '7R' },
    program: { id: 'program-1', name: 'Matematika 7', code: null },
    deliveryMode: 'group',
    group: { id: 'group-1', name: 'Grupa Orion', code: null },
    status: 'active',
    lastSessionAtUtc: '2026-08-27T16:00:00Z',
  }],
  page: 1,
  pageSize: 25,
  totalCount: 1,
  totalPages: 1,
}

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function renderStudents() {
  return render(
    <MemoryRouter initialEntries={['/students']}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('student list', () => {
  it('renders owner data, overview and neutral progress without inventing a percentage', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(studentPage))
      .mockResolvedValueOnce(json(overview))

    renderStudents()

    expect(await screen.findByRole('heading', { level: 1, name: 'Popis učenika' })).toBeInTheDocument()
    const table = await screen.findByRole('table')
    expect(within(table).getByText('Ana Anić')).toBeInTheDocument()
    expect(within(table).getByText('Grupa Orion')).toBeInTheDocument()
    expect(within(table).getByText('Nije dostupno')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: '1 ukupno učenika' })).toBeInTheDocument()
    expect(screen.queryByText(/\d+\s*%/)).not.toBeInTheDocument()
  })

  it('keeps filters in route state and requests the selected status', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(studentPage))
      .mockResolvedValueOnce(json(overview))
      .mockResolvedValueOnce(json({ ...studentPage, items: [], totalCount: 0, totalPages: 0 }))
      .mockResolvedValueOnce(json(overview))

    renderStudents()
    await screen.findByRole('table')
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: '2' } })

    await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) =>
      String(input).includes('/students?page=1&pageSize=25&status=2'))).toBe(true))
    expect(await screen.findByRole('heading', { name: 'Nema učenika za odabrane filtre' }))
      .toBeInTheDocument()
  })

  it('shows an explicit recoverable error state', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json({ code: 'request_failed' }, 503))
      .mockResolvedValueOnce(json(overview))

    renderStudents()

    expect(await screen.findByRole('heading', { name: 'Popis nije dostupan' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pokušaj ponovno' })).toBeInTheDocument()
  })
})
