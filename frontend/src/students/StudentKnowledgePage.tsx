import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  getStudentKnowledgeDetail,
  type ReadinessConfidence,
  type ReadinessStatus,
  type StudentKnowledgeAreaDetail,
  type StudentKnowledgeComponentDetail,
  type StudentKnowledgeDetailSnapshot,
  type StudentKnowledgeModelDetail,
} from './studentsApi.ts'
import './StudentKnowledgePage.css'

export function StudentKnowledgePage() {
  const { studentId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const requestedAreaId = searchParams.get('areaId')
  const [snapshot, setSnapshot] = useState<StudentKnowledgeDetailSnapshot | null>(null)
  const [selectedAreas, setSelectedAreas] = useState<Record<string, string>>({})
  const [selectedComponents, setSelectedComponents] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<{ message: string; notFound: boolean } | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getStudentKnowledgeDetail(studentId, controller.signal)
      .then((result) => {
        setSnapshot(result)
        setSelectedAreas(createInitialSelection(result.models, requestedAreaId))
        setSelectedComponents(createInitialComponentSelection(result.models, requestedAreaId))
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError({
          message: requestError instanceof ApiError ? requestError.message : 'Detalj znanja trenutačno nije moguće učitati.',
          notFound: requestError instanceof ApiError && requestError.status === 404,
        })
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [studentId, requestedAreaId, version])

  if (loading) return <KnowledgeState title="Učitavanje detalja znanja" message="Pripremamo postojeće projekcije komponenti…" />
  if (error) return (
    <KnowledgeState
      title={error.notFound ? 'Učenik nije pronađen' : 'Detalj znanja nije dostupan'}
      message={error.notFound ? 'Učenik ne postoji, arhiviran je ili nije dio vašeg računa.' : error.message}
    >
      {!error.notFound && <button onClick={() => { setLoading(true); setError(null); setVersion((value) => value + 1) }} type="button">Pokušaj ponovno</button>}
      <Link to="/students">Povratak na popis</Link>
    </KnowledgeState>
  )
  if (!snapshot) return null

  const name = `${snapshot.firstName} ${snapshot.lastName}`
  const projectionCount = snapshot.models.reduce((total, model) => total + model.areas.reduce((areaTotal, area) => areaTotal + area.components.length, 0), 0)

  return (
    <section className="knowledge-page" aria-labelledby="knowledge-title">
      <nav className="knowledge-breadcrumb" aria-label="Putanja">
        <Link to="/students">Učenici</Link><span>›</span>
        <Link to={`/students/${studentId}`}>{name}</Link><span>›</span>
        <span>Detalj znanja</span>
      </nav>

      <header className="knowledge-hero">
        <div>
          <h1 id="knowledge-title">Detalj znanja učenika</h1>
          <p>Stvarna, verzionirana slika znanja po područjima i komponentama.</p>
        </div>
        <Link to={`/students/${studentId}/readiness`}>← Natrag na procjenu</Link>
      </header>

      <section className="knowledge-student" aria-label="Sažetak učenika">
        <div className="knowledge-student__avatar" aria-hidden="true">{snapshot.firstName[0]}{snapshot.lastName[0]}</div>
        <div><h2>{name}</h2><p>{[snapshot.schoolGradeCode ?? snapshot.schoolGradeName, snapshot.schoolName].filter(Boolean).join(' · ')}</p></div>
        <dl>
          <div><dt>Program</dt><dd>{snapshot.programName ?? 'Nije dodijeljen'}</dd></div>
          <div><dt>Grupa</dt><dd>{snapshot.groupName ?? 'Nije dodijeljena'}</dd></div>
          <div><dt>Komponente s projekcijom</dt><dd>{projectionCount}</dd></div>
        </dl>
        <aside><strong>Kako čitati rezultate?</strong><p>Postotak je deterministička procjena iz evidentiranih dokaza. Pouzdanost i broj lanaca dokaza uvijek se čitaju zajedno s rezultatom.</p></aside>
      </section>

      {snapshot.models.length === 0 ? (
        <section className="knowledge-empty">
          <span aria-hidden="true">◇</span>
          <h2>Nema dovoljno podataka za detalj znanja</h2>
          <p>Komponente će se pojaviti tek nakon što stvarne evidentirane aktivnosti proizvedu projekcije. Nedostatak podatka nije rezultat nula posto.</p>
        </section>
      ) : snapshot.models.map((model) => (
        <ModelKnowledge
          key={model.knowledgeModelId}
          model={model}
          selectedAreaId={selectedAreas[model.knowledgeModelId] ?? model.areas[0]?.knowledgeAreaId}
          selectedComponentId={selectedComponents[model.knowledgeModelId]}
          onSelectArea={(areaId) => {
            setSelectedAreas((current) => ({ ...current, [model.knowledgeModelId]: areaId }))
            const area = model.areas.find((candidate) => candidate.knowledgeAreaId === areaId)
            setSelectedComponents((current) => ({ ...current, [model.knowledgeModelId]: area?.components[0]?.knowledgeComponentId ?? '' }))
          }}
          onSelectComponent={(componentId) => setSelectedComponents((current) => ({ ...current, [model.knowledgeModelId]: componentId }))}
        />
      ))}

      <p className="knowledge-guidance"><span aria-hidden="true">ⓘ</span> Rezultati se automatski osvježavaju iz Evidence lanca. Ovaj ekran ne izračunava ocjenu, trend ni preporuke i ne dopušta ručni unos postotka.</p>
    </section>
  )
}

function ModelKnowledge({ model, selectedAreaId, selectedComponentId, onSelectArea, onSelectComponent }: {
  readonly model: StudentKnowledgeModelDetail
  readonly selectedAreaId?: string
  readonly selectedComponentId?: string
  readonly onSelectArea: (areaId: string) => void
  readonly onSelectComponent: (componentId: string) => void
}) {
  const selectedArea = model.areas.find((area) => area.knowledgeAreaId === selectedAreaId) ?? model.areas[0]
  return (
    <section className="knowledge-model" aria-labelledby={`model-${model.knowledgeModelId}`}>
      <header>
        <div><p>Model znanja</p><h2 id={`model-${model.knowledgeModelId}`}>{model.code} <span>v{model.version}</span></h2></div>
        <span className={`knowledge-model__status knowledge-model__status--${model.status.toLowerCase()}`}>{modelStatusLabel(model.status)}</span>
      </header>
      <div className="knowledge-tabs" role="tablist" aria-label={`${model.code} područja`}>
        {model.areas.map((area) => (
          <button
            aria-selected={area.knowledgeAreaId === selectedArea?.knowledgeAreaId}
            key={area.knowledgeAreaId}
            onClick={() => onSelectArea(area.knowledgeAreaId)}
            role="tab"
            type="button"
          >{area.name}<small>{formatScore(area.score)}</small></button>
        ))}
      </div>
      {selectedArea && <AreaDetail area={selectedArea} modelLabel={`${model.code} v${model.version}`} selectedComponentId={selectedComponentId} onSelectComponent={onSelectComponent} />}
    </section>
  )
}

function AreaDetail({ area, modelLabel, selectedComponentId, onSelectComponent }: {
  readonly area: StudentKnowledgeAreaDetail
  readonly modelLabel: string
  readonly selectedComponentId?: string
  readonly onSelectComponent: (componentId: string) => void
}) {
  const depths = useMemo(() => componentDepths(area.components), [area.components])
  const selectedComponent = area.components.find((component) => component.knowledgeComponentId === selectedComponentId) ?? area.components[0]
  return (
    <div className="knowledge-analysis">
      <header>
        <div><p>Detalj po komponentama</p><h3>{area.name}</h3></div>
        <div className="knowledge-analysis__summary">
          <strong>{formatScore(area.score)}</strong>
          <span>{confidenceLabel(area.confidence)} pouzdanost · {area.evidenceCount} {chainLabel(area.evidenceCount)}</span>
        </div>
      </header>
      {area.components.length === 0 ? <p className="knowledge-components-empty">Nema projekcija komponenti za ovo područje.</p> : (
        <div className="knowledge-component-layout">
          <div className="knowledge-table-wrap">
            <table className="knowledge-table">
              <thead><tr><th>Komponenta znanja</th><th>Rezultat</th><th>Status</th><th>Pouzdanost</th><th>Lanci dokaza</th><th>Izračunato</th></tr></thead>
              <tbody>{area.components.map((component) => (
                <ComponentRow
                  component={component}
                  depth={depths.get(component.knowledgeComponentId) ?? 0}
                  isSelected={component.knowledgeComponentId === selectedComponent?.knowledgeComponentId}
                  key={component.knowledgeComponentId}
                  onSelect={() => onSelectComponent(component.knowledgeComponentId)}
                />
              ))}</tbody>
            </table>
          </div>
          {selectedComponent && <ComponentDetail area={area} component={selectedComponent} modelLabel={modelLabel} onSelectComponent={onSelectComponent} />}
        </div>
      )}
      <p className="knowledge-analysis__meta">Efektivna težina područja: {formatWeight(area.effectiveEvidenceWeight)} · {area.algorithmVersion ?? 'Algoritam nije primijenjen'}{area.calculatedAtUtc ? ` · ${formatDateTime(area.calculatedAtUtc)}` : ''}</p>
    </div>
  )
}

function ComponentRow({ component, depth, isSelected, onSelect }: {
  readonly component: StudentKnowledgeComponentDetail
  readonly depth: number
  readonly isSelected: boolean
  readonly onSelect: () => void
}) {
  return (
    <tr className={isSelected ? 'knowledge-table__selected' : undefined}>
      <th scope="row"><button aria-pressed={isSelected} onClick={onSelect} style={{ '--knowledge-depth': depth } as CSSProperties} type="button">{component.name}</button>{component.status === 'Deprecated' && <small>Zastarjela komponenta</small>}</th>
      <td><strong>{formatScore(component.score)}</strong>{component.score !== null && <i className="knowledge-score-bar"><b style={{ width: `${Math.round(component.score * 100)}%` }} /></i>}</td>
      <td><span className={`knowledge-readiness knowledge-readiness--${statusTone(component.readiness)}`}>{readinessLabel(component.readiness)}</span></td>
      <td>{confidenceLabel(component.confidence)}</td>
      <td>{component.evidenceCount}</td>
      <td>{formatDateTime(component.calculatedAtUtc)}</td>
    </tr>
  )
}

function ComponentDetail({ area, component, modelLabel, onSelectComponent }: {
  readonly area: StudentKnowledgeAreaDetail
  readonly component: StudentKnowledgeComponentDetail
  readonly modelLabel: string
  readonly onSelectComponent: (componentId: string) => void
}) {
  const byId = new Map(area.components.map((candidate) => [candidate.knowledgeComponentId, candidate]))
  const path = componentPath(component, byId)
  const pathLabels = path[0]?.name === area.name ? path.map((item) => item.name) : [area.name, ...path.map((item) => item.name)]
  const children = area.components.filter((candidate) => candidate.parentKnowledgeComponentId === component.knowledgeComponentId)

  return (
    <aside className="knowledge-component-detail" aria-labelledby={`component-${component.knowledgeComponentId}`}>
      <p className="knowledge-component-detail__eyebrow">Odabrana komponenta</p>
      <div className="knowledge-component-detail__title">
        <div><h4 id={`component-${component.knowledgeComponentId}`}>{component.name}</h4><p>{pathLabels.join(' › ')}</p></div>
        <span className={`knowledge-readiness knowledge-readiness--${statusTone(component.readiness)}`}>{readinessLabel(component.readiness)}</span>
      </div>
      <div className="knowledge-component-detail__score">
        <strong>{formatScore(component.score)}</strong>
        <span>{component.score === null ? 'Nema dovoljno podataka za rezultat' : 'Trenutačna projekcija ovladanosti'}</span>
        {component.score !== null && <i className="knowledge-score-bar"><b style={{ width: `${Math.round(component.score * 100)}%` }} /></i>}
      </div>
      <dl>
        <div><dt>Pouzdanost</dt><dd>{confidenceLabel(component.confidence)}</dd></div>
        <div><dt>Lanci dokaza</dt><dd>{component.evidenceCount}</dd></div>
        <div><dt>Efektivna težina</dt><dd>{formatWeight(component.effectiveEvidenceWeight)}</dd></div>
        <div><dt>Izračunato</dt><dd>{formatDateTime(component.calculatedAtUtc)}</dd></div>
        <div><dt>Algoritam</dt><dd>{component.algorithmVersion}</dd></div>
        <div><dt>Model</dt><dd>{modelLabel}</dd></div>
      </dl>
      {children.length > 0 && (
        <section className="knowledge-component-detail__children" aria-label="Podređene komponente">
          <h5>Podređene komponente</h5>
          {children.map((child) => <button key={child.knowledgeComponentId} onClick={() => onSelectComponent(child.knowledgeComponentId)} type="button"><span>{child.name}</span><strong>{formatScore(child.score)}</strong><i aria-hidden="true">→</i></button>)}
        </section>
      )}
      <p className="knowledge-component-detail__notice">Pojedinačne aktivnosti i trend nisu prikazani jer postojeći Evidence contract ne nosi njihov provjerljiv prikazni kontekst.</p>
    </aside>
  )
}

function KnowledgeState({ title, message, children }: { readonly title: string; readonly message: string; readonly children?: ReactNode }) {
  return <section className="knowledge-state"><span aria-hidden="true">5</span><h1>{title}</h1><p>{message}</p><div>{children}</div></section>
}

function createInitialSelection(models: readonly StudentKnowledgeModelDetail[], requestedAreaId: string | null) {
  return Object.fromEntries(models.map((model) => {
    const requested = model.areas.find((area) => area.knowledgeAreaId === requestedAreaId)
    return [model.knowledgeModelId, requested?.knowledgeAreaId ?? model.areas[0]?.knowledgeAreaId ?? '']
  }))
}

function createInitialComponentSelection(models: readonly StudentKnowledgeModelDetail[], requestedAreaId: string | null) {
  return Object.fromEntries(models.map((model) => {
    const area = model.areas.find((candidate) => candidate.knowledgeAreaId === requestedAreaId) ?? model.areas[0]
    return [model.knowledgeModelId, area?.components[0]?.knowledgeComponentId ?? '']
  }))
}

function componentPath(component: StudentKnowledgeComponentDetail, byId: ReadonlyMap<string, StudentKnowledgeComponentDetail>) {
  const path: StudentKnowledgeComponentDetail[] = [component]
  const visited = new Set([component.knowledgeComponentId])
  let parentId = component.parentKnowledgeComponentId
  while (parentId && !visited.has(parentId)) {
    const parent = byId.get(parentId)
    if (!parent) break
    path.unshift(parent)
    visited.add(parentId)
    parentId = parent.parentKnowledgeComponentId
  }
  return path
}

function componentDepths(components: readonly StudentKnowledgeComponentDetail[]) {
  const byId = new Map(components.map((component) => [component.knowledgeComponentId, component]))
  const result = new Map<string, number>()
  const resolve = (component: StudentKnowledgeComponentDetail, visited = new Set<string>()): number => {
    if (!component.parentKnowledgeComponentId || visited.has(component.knowledgeComponentId)) return 0
    const parent = byId.get(component.parentKnowledgeComponentId)
    if (!parent) return 0
    visited.add(component.knowledgeComponentId)
    return 1 + resolve(parent, visited)
  }
  for (const component of components) result.set(component.knowledgeComponentId, resolve(component))
  return result
}

function formatScore(score: number | null) { return score === null ? '—' : `${Math.round(score * 100)} %` }
function readinessLabel(value: ReadinessStatus) { return ({ InsufficientData: 'Nedovoljno podataka', NeedsWork: 'Potreban rad', Developing: 'U razvoju', Ready: 'Spreman', Strong: 'Snažno ovladano' })[value] }
function confidenceLabel(value: ReadinessConfidence) { return ({ NoData: 'Nema', VeryLow: 'Vrlo niska', Low: 'Niska', Medium: 'Srednja', High: 'Visoka' })[value] }
function statusTone(value: ReadinessStatus) { return value === 'Strong' || value === 'Ready' ? 'positive' : value === 'NeedsWork' ? 'attention' : 'neutral' }
function modelStatusLabel(value: StudentKnowledgeModelDetail['status']) { return ({ Draft: 'Radna verzija', Published: 'Objavljen', Retired: 'Povijesna verzija' })[value] }
function chainLabel(count: number) { return count === 1 ? 'lanac dokaza' : 'lanaca dokaza' }
function formatWeight(value: number) { return new Intl.NumberFormat('hr-HR', { maximumFractionDigits: 2 }).format(value) }
function formatDateTime(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
