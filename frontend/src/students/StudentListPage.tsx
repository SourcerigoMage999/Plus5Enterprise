import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  getStudentOverview,
  getStudents,
  type PagedStudents,
  type StudentDeliveryMode,
  type StudentListFilters,
  type StudentListItem,
  type StudentListOverview,
  type StudentStatus,
} from './studentsApi.ts'
import './StudentListPage.css'

const pageSize = 25

export function StudentListPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const rawSearch = searchParams.get('search') ?? ''
  const rawProgramId = searchParams.get('programId') ?? ''
  const rawDeliveryMode = searchParams.get('deliveryMode') ?? ''
  const rawStatus = searchParams.get('status') ?? ''
  const rawSchoolGradeId = searchParams.get('schoolGradeId') ?? ''
  const rawPage = searchParams.get('page') ?? ''
  const filters = useMemo(() => readFilters(
    rawSearch,
    rawProgramId,
    rawDeliveryMode,
    rawStatus,
    rawSchoolGradeId,
    rawPage,
  ), [
    rawDeliveryMode,
    rawPage,
    rawProgramId,
    rawSchoolGradeId,
    rawSearch,
    rawStatus,
  ])
  const [searchInput, setSearchInput] = useState(() => filters.search)
  const [students, setStudents] = useState<PagedStudents | null>(null)
  const [overview, setOverview] = useState<StudentListOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [requestVersion, setRequestVersion] = useState(0)
  const view = searchParams.get('view') === 'cards' ? 'cards' : 'table'

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([
      getStudents(filters, controller.signal),
      getStudentOverview(controller.signal),
    ])
      .then(([studentPage, studentOverview]) => {
        setStudents(studentPage)
        setOverview(studentOverview)
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError
          ? requestError.message
          : 'Popis učenika trenutačno nije moguće učitati.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [
    filters,
    requestVersion,
  ])

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    updateParams({ search: searchInput.trim(), page: null })
  }

  function updateParams(values: Record<string, string | null>) {
    if (Object.keys(values).some((key) => key !== 'view')) {
      setLoading(true)
      setError(null)
    }
    const next = new URLSearchParams(searchParams)
    Object.entries(values).forEach(([key, value]) => {
      if (value) next.set(key, value)
      else next.delete(key)
    })
    setSearchParams(next)
  }

  const hasFilters = Boolean(
    filters.search
      || filters.programId
      || filters.deliveryMode
      || filters.status
      || filters.schoolGradeId,
  )

  return (
    <section className="student-list-page" aria-labelledby="student-list-title">
      <header className="student-list-hero">
        <div>
          <p className="student-list-eyebrow">Učenici</p>
          <h1 id="student-list-title">Popis učenika</h1>
          <p>Pregledajte i upravljajte svojim učenicima.</p>
        </div>
        <div className="student-list-add-boundary">
          <button className="student-primary-action" type="button" disabled>
            <span aria-hidden="true">+</span> Dodaj učenika
          </button>
          <small>Dodavanje dolazi u sljedećoj podfazi.</small>
        </div>
      </header>

      <div className="student-controls">
        <form className="student-filters" aria-label="Pretraživanje i filtriranje učenika" onSubmit={submitSearch}>
        <label className="student-search">
          <span>Pretraži učenike</span>
          <span className="student-search__control">
            <input
              maxLength={100}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Pretraži učenike..."
              type="search"
              value={searchInput}
            />
            <button type="submit">Pretraži</button>
          </span>
        </label>

        <FilterSelect
          label="Program"
          allLabel="Svi programi"
          value={filters.programId}
          onChange={(value) => updateParams({ programId: value, page: null })}
          options={overview?.programs.map((option) => ({ value: option.id, label: option.name })) ?? []}
        />
        <FilterSelect
          label="Način rada"
          allLabel="Svi načini rada"
          value={filters.deliveryMode}
          onChange={(value) => updateParams({ deliveryMode: value, page: null })}
          options={[
            { value: '1', label: 'Individualno' },
            { value: '2', label: 'Grupa' },
          ]}
        />
        <FilterSelect
          label="Status"
          allLabel="Svi statusi"
          value={filters.status}
          onChange={(value) => updateParams({ status: value, page: null })}
          options={[
            { value: '1', label: 'Aktivan' },
            { value: '2', label: 'Na čekanju' },
            { value: '3', label: 'Neaktivan' },
          ]}
        />
        <FilterSelect
          label="Razred"
          allLabel="Svi razredi"
          value={filters.schoolGradeId}
          onChange={(value) => updateParams({ schoolGradeId: value, page: null })}
          options={overview?.schoolGrades.map((option) => ({
            value: option.id,
            label: option.code ? `${option.code} — ${option.name}` : option.name,
          })) ?? []}
        />

        {hasFilters && (
          <button
            className="student-clear-filters"
            type="button"
            onClick={() => {
              setSearchInput('')
              setLoading(true)
              setError(null)
              setSearchParams(view === 'cards' ? { view: 'cards' } : {})
            }}
          >
            Očisti filtre
          </button>
        )}
        </form>
        <div className="student-view-switch" aria-label="Način prikaza">
          <button
            aria-pressed={view === 'cards'}
            onClick={() => updateParams({ view: 'cards' })}
            type="button"
          >
            Kartice
          </button>
          <button
            aria-pressed={view === 'table'}
            onClick={() => updateParams({ view: null })}
            type="button"
          >
            Tablica
          </button>
        </div>
      </div>

      <div className="student-list-layout">
        <div className="student-list-content">
          <p className="visually-hidden" aria-live="polite">
            {students ? `${students.totalCount} ${studentCountLabel(students.totalCount)}` : 'Učitavanje popisa'}
          </p>

          {loading && <StudentListLoading />}
          {!loading && error && (
            <StudentListMessage title="Popis nije dostupan" message={error}>
              <button
                type="button"
                onClick={() => {
                  setLoading(true)
                  setError(null)
                  setRequestVersion((version) => version + 1)
                }}
              >
                Pokušaj ponovno
              </button>
            </StudentListMessage>
          )}
          {!loading && !error && students?.items.length === 0 && (
            <StudentListMessage
              title={hasFilters ? 'Nema učenika za odabrane filtre' : 'Još nema učenika'}
              message={hasFilters
                ? 'Promijenite ili očistite filtre kako biste proširili rezultate.'
                : 'Učenici će se prikazati ovdje nakon što ih dodate u sljedećoj podfazi.'}
            />
          )}
          {!loading && !error && students && students.items.length > 0 && (
            view === 'table'
              ? <StudentTable students={students.items} />
              : <StudentCards students={students.items} />
          )}

          {!loading && !error && students && students.totalPages > 1 && (
            <nav className="student-pagination" aria-label="Stranice popisa učenika">
              <span className="student-pagination__count">
                Prikazano {(students.page - 1) * students.pageSize + 1} – {Math.min(students.page * students.pageSize, students.totalCount)} od {students.totalCount} učenika
              </span>
              <span className="student-pagination__controls">
                <button
                  disabled={students.page <= 1}
                  onClick={() => updateParams({ page: String(students.page - 1) })}
                  type="button"
                  aria-label="Prethodna stranica"
                >
                  ‹
                </button>
                <strong aria-current="page">{students.page}</strong>
                <span>od {students.totalPages}</span>
                <button
                  disabled={students.page >= students.totalPages}
                  onClick={() => updateParams({ page: String(students.page + 1) })}
                  type="button"
                  aria-label="Sljedeća stranica"
                >
                  ›
                </button>
              </span>
            </nav>
          )}
        </div>

        <StudentOverview overview={overview} loading={loading && !overview} />
      </div>
      <p className="student-guidance">
        <span aria-hidden="true">ⓘ</span>
        Detaljni dosje i uređivanje podataka bit će dostupni u sljedećim fazama.
      </p>
    </section>
  )
}

function StudentTable({ students }: { readonly students: readonly StudentListItem[] }) {
  return (
    <div className="student-table-wrap">
      <table className="student-table">
        <thead>
          <tr>
            <th scope="col">Učenik</th>
            <th scope="col">Razred</th>
            <th scope="col">Program</th>
            <th scope="col">Način rada</th>
            <th scope="col">Grupa</th>
            <th scope="col">Status</th>
            <th scope="col">Zadnji sat</th>
            <th scope="col">Napredak</th>
            <th scope="col"><span className="visually-hidden">Akcije</span></th>
          </tr>
        </thead>
        <tbody>
          {students.map((student) => (
            <tr key={student.id}>
              <td><StudentIdentity student={student} /></td>
              <td>{student.schoolGrade.code ?? student.schoolGrade.name}</td>
              <td>{student.program?.name ?? '—'}</td>
              <td>{deliveryModeLabel(student.deliveryMode)}</td>
              <td>{student.group?.name ?? '—'}</td>
              <td><StudentStatusBadge status={student.status} /></td>
              <td>{formatLastSession(student.lastSessionAtUtc)}</td>
              <td><NeutralProgress /></td>
              <td><StudentActions /></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function StudentCards({ students }: { readonly students: readonly StudentListItem[] }) {
  return (
    <div className="student-cards">
      {students.map((student) => (
        <article className="student-card" key={student.id}>
          <div className="student-card__top">
            <StudentIdentity student={student} />
            <StudentStatusBadge status={student.status} />
          </div>
          <dl>
            <StudentCardDetail label="Razred" value={student.schoolGrade.code ?? student.schoolGrade.name} />
            <StudentCardDetail label="Program" value={student.program?.name ?? '—'} />
            <StudentCardDetail label="Način rada" value={deliveryModeLabel(student.deliveryMode)} />
            <StudentCardDetail label="Grupa" value={student.group?.name ?? '—'} />
            <StudentCardDetail label="Zadnji sat" value={formatLastSession(student.lastSessionAtUtc)} />
            <StudentCardDetail label="Napredak" value="Nije dostupno" />
          </dl>
          <button className="student-card__action" disabled type="button">Otvori dosje — dolazi u Phase 3.3</button>
        </article>
      ))}
    </div>
  )
}

function StudentActions() {
  return (
    <span className="student-actions" aria-label="Akcije učenika">
      <button disabled type="button" title="Dosje dolazi u Phase 3.3" aria-label="Otvori dosje">◉</button>
      <button disabled type="button" title="Komunikacija dolazi u kasnijoj fazi" aria-label="Komunikacija">▢</button>
      <button disabled type="button" title="Uređivanje dolazi u Phase 3.4" aria-label="Više akcija">⋮</button>
    </span>
  )
}

function NeutralProgress() {
  return (
    <span className="student-neutral-value" title="Napredak nije dostupan">
      <span>—</span><i aria-hidden="true" />
      <span className="visually-hidden">Nije dostupno</span>
    </span>
  )
}

function StudentIdentity({ student }: { readonly student: StudentListItem }) {
  const initials = `${student.firstName[0] ?? ''}${student.lastName[0] ?? ''}`.toLocaleUpperCase('hr')
  return (
    <span className="student-identity">
      <span className="student-avatar" aria-hidden="true">{initials}</span>
      <span>
        <strong>{student.firstName} {student.lastName}</strong>
        {student.nickname && <small>“{student.nickname}”</small>}
      </span>
    </span>
  )
}

function StudentStatusBadge({ status }: { readonly status: StudentStatus }) {
  return <span className={`student-status student-status--${status}`}>{statusLabel(status)}</span>
}

function StudentOverview({ overview, loading }: {
  readonly overview: StudentListOverview | null
  readonly loading: boolean
}) {
  return (
    <aside className="student-overview" aria-label="Pregled baze učenika">
      <section className="student-overview-card">
        <p className="student-overview-card__eyebrow">Pregled učenika</p>
        <h2
          className="student-overview-total"
          aria-label={`${loading ? '—' : overview?.totalCount ?? 0} ukupno učenika`}
        >
          <strong>{loading ? '—' : overview?.totalCount ?? 0}</strong>
          <span>Ukupno učenika</span>
        </h2>
        <dl className="student-overview-stats">
          <div><dt>Aktivnih</dt><dd>{overview?.activeCount ?? 0}</dd></div>
          <div><dt>Na čekanju</dt><dd>{overview?.onHoldCount ?? 0}</dd></div>
          <div><dt>Neaktivnih</dt><dd>{overview?.inactiveCount ?? 0}</dd></div>
        </dl>
      </section>

      <section className="student-overview-card">
        <p className="student-overview-card__eyebrow">Programi</p>
        <h2>Raspodjela učenika</h2>
        {overview && overview.programCounts.length > 0 ? (
          <ul className="student-program-counts">
            {overview.programCounts.map((program) => (
              <li key={program.programId}><span>{program.name}</span><strong>{program.studentCount}</strong></li>
            ))}
            {overview.withoutProgramCount > 0 && (
              <li><span>Bez programa</span><strong>{overview.withoutProgramCount}</strong></li>
            )}
          </ul>
        ) : (
          <p className="student-overview-empty">Nema podataka o programima.</p>
        )}
      </section>
    </aside>
  )
}

function StudentListLoading() {
  return (
    <div className="student-list-loading" role="status">
      <span className="visually-hidden">Učitavanje učenika</span>
      {Array.from({ length: 5 }, (_, index) => <span key={index} />)}
    </div>
  )
}

function StudentListMessage({ title, message, children }: {
  readonly title: string
  readonly message: string
  readonly children?: ReactNode
}) {
  return (
    <section className="student-list-message">
      <span className="student-list-message__mark" aria-hidden="true">5</span>
      <h2>{title}</h2>
      <p>{message}</p>
      {children}
    </section>
  )
}

function FilterSelect({ label, allLabel, value, options, onChange }: {
  readonly label: string
  readonly allLabel: string
  readonly value: string
  readonly options: readonly { readonly value: string; readonly label: string }[]
  readonly onChange: (value: string) => void
}) {
  return (
    <label>
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{allLabel}</option>
        {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    </label>
  )
}

function StudentCardDetail({ label, value }: { readonly label: string; readonly value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>
}

function readFilters(
  search: string,
  programId: string,
  deliveryMode: string,
  status: string,
  schoolGradeId: string,
  pageValue: string,
): StudentListFilters {
  const page = Number(pageValue)

  return {
    search: search.slice(0, 100),
    programId,
    deliveryMode: deliveryMode === '1' || deliveryMode === '2' ? deliveryMode : '',
    status: status === '1' || status === '2' || status === '3' ? status : '',
    schoolGradeId,
    page: Number.isInteger(page) && page > 0 ? page : 1,
    pageSize,
  }
}

function deliveryModeLabel(mode: StudentDeliveryMode | null) {
  if (mode === 'individual') return 'Individualno'
  if (mode === 'group') return 'Grupa'
  return '—'
}

function statusLabel(status: StudentStatus) {
  if (status === 'active') return 'Aktivan'
  if (status === 'on_hold') return 'Na čekanju'
  return 'Neaktivan'
}

function formatLastSession(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('hr-HR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

function studentCountLabel(count: number) {
  return count === 1 ? 'učenik' : 'učenika'
}
