import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  getStudentReadiness,
  type ReadinessConfidence,
  type ReadinessStatus,
  type StudentReadinessArea,
  type StudentReadinessSnapshot,
} from './studentsApi.ts'
import './StudentReadinessPage.css'

export function StudentReadinessPage() {
  const { studentId = '' } = useParams()
  const [snapshot, setSnapshot] = useState<StudentReadinessSnapshot | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<{ message: string; notFound: boolean } | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getStudentReadiness(studentId, controller.signal)
      .then(setSnapshot)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError({
          message: requestError instanceof ApiError ? requestError.message : 'Procjenu trenutačno nije moguće učitati.',
          notFound: requestError instanceof ApiError && requestError.status === 404,
        })
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [studentId, version])

  const models = useMemo(() => groupByModel(snapshot?.areas ?? []), [snapshot])

  if (loading) return <ReadinessState title="Učitavanje procjene" message="Pripremamo najnovije izračunate podatke…" />
  if (error) return (
    <ReadinessState
      title={error.notFound ? 'Učenik nije pronađen' : 'Procjena nije dostupna'}
      message={error.notFound ? 'Učenik ne postoji, arhiviran je ili nije dio vašeg računa.' : error.message}
    >
      {!error.notFound && <button onClick={() => { setLoading(true); setError(null); setVersion((value) => value + 1) }} type="button">Pokušaj ponovno</button>}
      <Link to="/students">Povratak na popis</Link>
    </ReadinessState>
  )
  if (!snapshot) return null

  const name = `${snapshot.firstName} ${snapshot.lastName}`
  const latestCalculation = latestCalculatedAt(snapshot.areas)

  return (
    <section className="readiness-page" aria-labelledby="readiness-title">
      <nav className="readiness-breadcrumb" aria-label="Putanja">
        <Link to="/students">Učenici</Link><span>›</span>
        <Link to={`/students/${studentId}`}>{name}</Link><span>›</span>
        <span>Procjena spremnosti</span>
      </nav>

      <header className="readiness-hero">
        <div>
          <p>Procjena spremnosti učenika</p>
          <h1 id="readiness-title">{name}</h1>
          <span>{snapshot.schoolGradeCode ?? snapshot.schoolGradeName}</span>
        </div>
        <div className="readiness-hero__actions">
          <Link to={`/students/${studentId}/knowledge`}>Detalj znanja</Link>
          <Link to={`/students/${studentId}`}>← Natrag na dosje</Link>
        </div>
      </header>

      <section className="readiness-intro" aria-labelledby="readiness-summary-title">
        <div className="readiness-intro__icon" aria-hidden="true">◎</div>
        <div>
          <h2 id="readiness-summary-title">Trenutačna slika po područjima</h2>
          <p>PLUS 5 prikazuje determinističku procjenu iz evidentiranih aktivnosti. Ovo nije školska ocjena ni jamstvo budućeg rezultata.</p>
        </div>
        <dl>
          <div><dt>Područja s podatcima</dt><dd>{snapshot.areas.filter((area) => area.score !== null).length} / {snapshot.areas.length}</dd></div>
          <div><dt>Zadnji izračun</dt><dd>{latestCalculation ? formatDateTime(latestCalculation) : 'Nije izračunato'}</dd></div>
        </dl>
      </section>

      {models.length === 0 ? (
        <section className="readiness-empty">
          <span aria-hidden="true">◇</span>
          <h2>Nema dovoljno podataka za procjenu spremnosti</h2>
          <p>Kako se evidentiraju aktivnosti povezane s komponentama znanja, ovdje će se pojaviti rezultat, pouzdanost i broj neovisnih lanaca dokaza za svako područje.</p>
        </section>
      ) : models.map((model, modelIndex) => (
        <section className="readiness-model" key={model.key} aria-labelledby={`readiness-model-${modelIndex}`}>
          <header>
            <div><p>Model znanja</p><h2 id={`readiness-model-${modelIndex}`}>{model.code}</h2></div>
            <span>Verzija {model.version}</span>
          </header>
          <div className="readiness-area-grid">
            {model.areas.map((area) => <AreaCard key={area.knowledgeAreaId} area={area} studentId={studentId} />)}
          </div>
        </section>
      ))}

      <p className="readiness-guidance"><span aria-hidden="true">ⓘ</span> Rezultati se ne unose ručno. Prikazana procjena koristi algoritam <code>readiness-v1</code>, a učitelj zadržava konačnu pedagošku prosudbu.</p>
    </section>
  )
}

function AreaCard({ area, studentId }: { readonly area: StudentReadinessArea; readonly studentId: string }) {
  const hasScore = area.score !== null
  const percentage = hasScore ? Math.round(area.score! * 100) : null
  return (
    <article className={`readiness-area readiness-area--${statusTone(area.readiness)}`}>
      <header><h3>{area.name}</h3><span>{readinessLabel(area.readiness)}</span></header>
      {hasScore ? (
        <>
          <div className="readiness-score"><strong>{percentage}<small>%</small></strong><span>{confidenceLabel(area.confidence)} pouzdanost</span></div>
          <div className="readiness-meter" aria-label={`${area.name}: ${percentage} %`}><i style={{ width: `${percentage}%` }} /></div>
        </>
      ) : <p className="readiness-area__empty">Nema dovoljno podataka za izračun rezultata.</p>}
      <dl>
        <div><dt>Lanci dokaza</dt><dd>{area.evidenceCount}</dd></div>
        <div><dt>Efektivna težina</dt><dd>{formatWeight(area.effectiveEvidenceWeight)}</dd></div>
        <div><dt>Izračunato</dt><dd>{formatDateTime(area.calculatedAtUtc)}</dd></div>
      </dl>
      <Link className="readiness-area__link" to={`/students/${studentId}/knowledge?areaId=${encodeURIComponent(area.knowledgeAreaId)}`}>Otvori komponente →</Link>
    </article>
  )
}

function ReadinessState({ title, message, children }: { readonly title: string; readonly message: string; readonly children?: ReactNode }) {
  return <section className="readiness-state"><span aria-hidden="true">5</span><h1>{title}</h1><p>{message}</p><div>{children}</div></section>
}

function groupByModel(areas: readonly StudentReadinessArea[]) {
  const groups = new Map<string, { key: string; code: string; version: string; areas: StudentReadinessArea[] }>()
  for (const area of areas) {
    const key = JSON.stringify([area.knowledgeModelCode, area.knowledgeModelVersion])
    const group = groups.get(key) ?? { key, code: area.knowledgeModelCode, version: area.knowledgeModelVersion, areas: [] }
    group.areas.push(area)
    groups.set(key, group)
  }
  return [...groups.values()]
}

function readinessLabel(value: ReadinessStatus) {
  return ({ InsufficientData: 'Nedovoljno podataka', NeedsWork: 'Potreban rad', Developing: 'U razvoju', Ready: 'Spreman', Strong: 'Snažno ovladano' })[value]
}
function confidenceLabel(value: ReadinessConfidence) {
  return ({ NoData: 'Nema', VeryLow: 'Vrlo niska', Low: 'Niska', Medium: 'Srednja', High: 'Visoka' })[value]
}
function statusTone(value: ReadinessStatus) { return value === 'Strong' || value === 'Ready' ? 'positive' : value === 'NeedsWork' ? 'attention' : 'neutral' }
function formatWeight(value: number) { return new Intl.NumberFormat('hr-HR', { maximumFractionDigits: 2 }).format(value) }
function formatDateTime(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
function latestCalculatedAt(areas: readonly StudentReadinessArea[]) { return areas.reduce<string | null>((latest, area) => !latest || area.calculatedAtUtc > latest ? area.calculatedAtUtc : latest, null) }
