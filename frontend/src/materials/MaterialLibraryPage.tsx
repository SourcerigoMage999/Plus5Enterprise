import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  getMaterialOverview,
  getMaterials,
  type MaterialLibraryFilters,
  type MaterialLibraryItem,
  type MaterialLibraryOverview,
  type MaterialOwnership,
  type PagedMaterials,
} from './materialsApi.ts'
import './MaterialLibraryPage.css'

const pageSize = 24

export function MaterialLibraryPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const rawParams = searchParams.toString()
  const filters = useMemo(() => readFilters(new URLSearchParams(rawParams)), [rawParams])
  const view = searchParams.get('view') === 'list' ? 'list' : 'grid'
  const [searchInput, setSearchInput] = useState(filters.search)
  const [materials, setMaterials] = useState<PagedMaterials | null>(null)
  const [overview, setOverview] = useState<MaterialLibraryOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [requestVersion, setRequestVersion] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([
      getMaterials(filters, controller.signal),
      getMaterialOverview(filters.ownership, controller.signal),
    ])
      .then(([page, nextOverview]) => {
        setMaterials(page)
        setOverview(nextOverview)
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError
          ? requestError.message
          : 'Biblioteku materijala trenutačno nije moguće učitati.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [filters, requestVersion])

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

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    updateParams({ search: searchInput.trim(), page: null })
  }

  function setOwnership(ownership: MaterialOwnership) {
    updateParams({ ownership: ownership === 'mine' ? null : 'shared', page: null })
  }

  const hasFilters = Boolean(
    filters.search
      || filters.subject
      || filters.programId
      || filters.schoolGradeId
      || filters.materialTypeCode
      || filters.tag,
  )

  return (
    <section className="material-library" aria-labelledby="material-library-title">
      <header className="material-library__hero">
        <div>
          <h1 id="material-library-title">4.1 Biblioteka materijala</h1>
          <p>Pregledajte, pretražujte i upravljajte svim svojim materijalima.</p>
        </div>
        <form className="material-library__search" onSubmit={submitSearch} role="search">
          <label>
            <span className="visually-hidden">Pretraži materijale</span>
            <input
              maxLength={100}
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="Pretraži materijale..."
              type="search"
              value={searchInput}
            />
          </label>
          <button type="submit">Pretraži</button>
        </form>
        <button
          className="material-library__new"
          disabled
          title="Dodavanje materijala dolazi u Phase 6.6"
          type="button"
        >
          <span aria-hidden="true">＋</span> Novi materijal
        </button>
      </header>

      <div className="material-library__layout">
        <div className="material-library__main">
          <div className="material-library__tabs" role="tablist" aria-label="Vlasništvo materijala">
            <button
              aria-selected={filters.ownership === 'mine'}
              onClick={() => setOwnership('mine')}
              role="tab"
              type="button"
            >
              Moji materijali
            </button>
            <button
              aria-selected={filters.ownership === 'shared'}
              onClick={() => setOwnership('shared')}
              role="tab"
              type="button"
            >
              Dijeljeni sa mnom
            </button>
          </div>

          <div className="material-library__toolbar">
            <strong>{materials?.totalCount ?? 0} {materialCountLabel(materials?.totalCount ?? 0)}</strong>
            <div>
              <label>
                <span>Sortiraj prema:</span>
                <select
                  aria-label="Sortiraj prema"
                  onChange={(event) => updateParams({ sort: event.target.value === 'newest' ? null : event.target.value, page: null })}
                  value={filters.sort}
                >
                  <option value="newest">Zadnje dodano</option>
                  <option value="oldest">Najstarije</option>
                  <option value="title">Naziv A–Ž</option>
                </select>
              </label>
              <span className="material-library__view" aria-label="Način prikaza">
                <button aria-pressed={view === 'grid'} onClick={() => updateParams({ view: null })} type="button">Mreža</button>
                <button aria-pressed={view === 'list'} onClick={() => updateParams({ view: 'list' })} type="button">Lista</button>
              </span>
            </div>
          </div>

          <p className="visually-hidden" aria-live="polite">
            {loading ? 'Učitavanje biblioteke materijala' : `${materials?.totalCount ?? 0} dostupnih materijala`}
          </p>
          {loading && <MaterialLoading />}
          {!loading && error && (
            <MaterialMessage title="Biblioteka nije dostupna" message={error}>
              <button
                onClick={() => {
                  setLoading(true)
                  setError(null)
                  setRequestVersion((version) => version + 1)
                }}
                type="button"
              >
                Pokušaj ponovno
              </button>
            </MaterialMessage>
          )}
          {!loading && !error && materials?.items.length === 0 && (
            <MaterialMessage
              title={hasFilters ? 'Nema materijala za odabrane filtre' : filters.ownership === 'mine' ? 'Još nema vaših materijala' : 'Nema materijala dijeljenih s vama'}
              message={hasFilters
                ? 'Promijenite ili poništite filtre kako biste proširili rezultate.'
                : filters.ownership === 'mine'
                  ? 'Novi materijal moći ćete dodati nakon završetka sigurnog import workflowa.'
                  : 'Kada vam drugi učitelj podijeli materijal, pojavit će se ovdje.'}
            />
          )}
          {!loading && !error && materials && materials.items.length > 0 && (
            view === 'grid'
              ? <MaterialGrid items={materials.items} />
              : <MaterialList items={materials.items} />
          )}

          {!loading && !error && materials && materials.totalPages > 1 && (
            <nav className="material-library__pagination" aria-label="Stranice biblioteke materijala">
              <button
                aria-label="Prethodna stranica"
                disabled={materials.page <= 1}
                onClick={() => updateParams({ page: String(materials.page - 1) })}
                type="button"
              >
                ‹
              </button>
              <span>Stranica <strong>{materials.page}</strong> od {materials.totalPages}</span>
              <button
                aria-label="Sljedeća stranica"
                disabled={materials.page >= materials.totalPages}
                onClick={() => updateParams({ page: String(materials.page + 1) })}
                type="button"
              >
                ›
              </button>
            </nav>
          )}

          <p className="material-library__guidance">
            <span aria-hidden="true">ⓘ</span>
            Otvaranje, uređivanje, dupliciranje, dijeljenje i dodavanje u pripremu aktiviraju se u sljedećim Material fazama.
          </p>
        </div>

        <MaterialSidebar
          filters={filters}
          hasFilters={hasFilters}
          overview={overview}
          onChange={updateParams}
          onClear={() => {
            setSearchInput('')
            setLoading(true)
            setError(null)
            setSearchParams(filters.ownership === 'shared' ? { ownership: 'shared' } : {})
          }}
        />
      </div>
    </section>
  )
}

function MaterialGrid({ items }: { readonly items: readonly MaterialLibraryItem[] }) {
  return (
    <div className="material-grid">
      {items.map((item) => <MaterialCard item={item} key={item.id} />)}
    </div>
  )
}

function MaterialCard({ item }: { readonly item: MaterialLibraryItem }) {
  return (
    <article className="material-card">
      <div className={`material-card__preview material-card__preview--${item.fileFormat}`} aria-hidden="true">
        <span>{formatMark(item.fileFormat)}</span>
        <small>{item.fileFormat.toUpperCase()}</small>
      </div>
      <div className="material-card__body">
        <span className="material-card__type">{materialTypeLabel(item.materialTypeCode)}</span>
        <h2>{item.title}</h2>
        <p>{[item.program?.name ?? item.subject, item.schoolGrade?.code ?? item.schoolGrade?.name].filter(Boolean).join(' · ') || 'Bez dodatnih oznaka'}</p>
        {item.tags.length > 0 && <p className="material-card__tags">{item.tags.slice(0, 2).map((tag) => <span key={tag}>{tag}</span>)}</p>}
        <footer>
          <time dateTime={item.addedAtUtc}>{formatDate(item.addedAtUtc)}</time>
          <button disabled title="Akcije materijala dolaze u sljedećim fazama" type="button" aria-label={`Akcije za ${item.title}`}>•••</button>
        </footer>
      </div>
    </article>
  )
}

function MaterialList({ items }: { readonly items: readonly MaterialLibraryItem[] }) {
  return (
    <div className="material-list" role="list">
      {items.map((item) => (
        <article className="material-list__item" key={item.id} role="listitem">
          <span className={`material-list__mark material-list__mark--${item.fileFormat}`} aria-hidden="true">{formatMark(item.fileFormat)}</span>
          <div>
            <h2>{item.title}</h2>
            <p>{materialTypeLabel(item.materialTypeCode)} · {item.subject ?? 'Bez predmeta'}</p>
          </div>
          <span>{item.program?.name ?? 'Bez programa'}</span>
          <time dateTime={item.addedAtUtc}>{formatDate(item.addedAtUtc)}</time>
          <button disabled title="Akcije materijala dolaze u sljedećim fazama" type="button" aria-label={`Akcije za ${item.title}`}>•••</button>
        </article>
      ))}
    </div>
  )
}

function MaterialSidebar({ filters, overview, hasFilters, onChange, onClear }: {
  readonly filters: MaterialLibraryFilters
  readonly overview: MaterialLibraryOverview | null
  readonly hasFilters: boolean
  readonly onChange: (values: Record<string, string | null>) => void
  readonly onClear: () => void
}) {
  return (
    <aside className="material-sidebar" aria-label="Filtri i pregled materijala">
      <section className="material-sidebar__card material-sidebar__filters">
        <header><h2>Filtri</h2>{hasFilters && <button onClick={onClear} type="button">Poništi sve</button>}</header>
        <FilterSelect label="Predmet" value={filters.subject} allLabel="Svi predmeti" options={(overview?.subjects ?? []).map(valueOption)} onChange={(value) => onChange({ subject: value, page: null })} />
        <FilterSelect label="Program" value={filters.programId} allLabel="Svi programi" options={(overview?.programs ?? []).map((item) => ({ value: item.id, label: item.name }))} onChange={(value) => onChange({ programId: value, page: null })} />
        <FilterSelect label="Razred" value={filters.schoolGradeId} allLabel="Svi razredi" options={(overview?.schoolGrades ?? []).map((item) => ({ value: item.id, label: item.code ?? item.name }))} onChange={(value) => onChange({ schoolGradeId: value, page: null })} />
        <FilterSelect label="Vrsta materijala" value={filters.materialTypeCode} allLabel="Sve vrste" options={(overview?.materialTypes ?? []).map((value) => ({ value, label: materialTypeLabel(value) }))} onChange={(value) => onChange({ materialTypeCode: value, page: null })} />
        <FilterSelect label="Oznake" value={filters.tag} allLabel="Sve oznake" options={(overview?.tags ?? []).map(valueOption)} onChange={(value) => onChange({ tag: value, page: null })} />
        <label className="material-sidebar__ownership">
          <input
            checked={filters.ownership === 'mine'}
            onChange={(event) => onChange({ ownership: event.target.checked ? null : 'shared', page: null })}
            type="checkbox"
          />
          Prikaži samo moje materijale
        </label>
      </section>

      <section className="material-sidebar__card">
        <h2>Vrste materijala</h2>
        {overview && overview.materialTypeCounts.length > 0 ? (
          <ul className="material-sidebar__counts">
            {overview.materialTypeCounts.map((item) => (
              <li key={item.code}><span>{materialTypeLabel(item.code)}</span><strong>{item.count}</strong></li>
            ))}
          </ul>
        ) : <p className="material-sidebar__empty">Nema dostupnih vrsta.</p>}
      </section>

      <section className="material-sidebar__card">
        <h2>Nedavno dodano</h2>
        {overview && overview.recentlyAdded.length > 0 ? (
          <ol className="material-sidebar__recent">
            {overview.recentlyAdded.map((item) => (
              <li key={item.id}><strong>{item.title}</strong><time dateTime={item.addedAtUtc}>{formatDate(item.addedAtUtc)}</time></li>
            ))}
          </ol>
        ) : <p className="material-sidebar__empty">Nema nedavno dodanih materijala.</p>}
      </section>
    </aside>
  )
}

function FilterSelect({ label, value, allLabel, options, onChange }: {
  readonly label: string
  readonly value: string
  readonly allLabel: string
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

function MaterialLoading() {
  return <div className="material-loading" role="status"><span className="visually-hidden">Učitavanje materijala</span>{Array.from({ length: 8 }, (_, index) => <span key={index} />)}</div>
}

function MaterialMessage({ title, message, children }: { readonly title: string; readonly message: string; readonly children?: ReactNode }) {
  return <section className="material-message"><span aria-hidden="true">5</span><h2>{title}</h2><p>{message}</p>{children}</section>
}

function readFilters(params: URLSearchParams): MaterialLibraryFilters {
  const page = Number(params.get('page'))
  const sort = params.get('sort')
  return {
    page: Number.isInteger(page) && page > 0 ? page : 1,
    pageSize,
    ownership: params.get('ownership') === 'shared' ? 'shared' : 'mine',
    sort: sort === 'oldest' || sort === 'title' ? sort : 'newest',
    search: (params.get('search') ?? '').slice(0, 100),
    subject: params.get('subject') ?? '',
    programId: params.get('programId') ?? '',
    schoolGradeId: params.get('schoolGradeId') ?? '',
    materialTypeCode: params.get('materialTypeCode') ?? '',
    tag: params.get('tag') ?? '',
  }
}

function valueOption(value: string) {
  return { value, label: value }
}

function materialTypeLabel(code: string) {
  const labels: Record<string, string> = {
    PRESENTATION: 'Prezentacija',
    WORKSHEET: 'Radni list',
    CONVERSATION_CARDS: 'Kartice za razgovor',
    INTERACTIVE_EXERCISE: 'Interaktivna vježba',
    VIDEO: 'Video',
    AUDIO: 'Audio',
    QUIZ: 'Kviz',
    IMAGE: 'Slika',
    POSTER: 'Plakat',
    MAP: 'Mapa',
  }
  return labels[code] ?? code.toLocaleLowerCase('hr').replaceAll('_', ' ')
}

function formatMark(format: MaterialLibraryItem['fileFormat']) {
  if (format === 'pptx') return '▤'
  if (format === 'mp4') return '▶'
  if (format === 'zip') return '▱'
  if (format === 'docx') return '▧'
  return '▥'
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value))
}

function materialCountLabel(count: number) {
  return count === 1 ? 'materijal' : 'materijala'
}
