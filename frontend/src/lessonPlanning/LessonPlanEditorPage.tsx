import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  attachLessonMaterial,
  detachLessonMaterial,
  getLessonMaterialCandidates,
  getLessonPlanEditor,
  type LessonBalanceArea,
  type LessonMaterialCandidate,
  type LessonPlanEditor,
  type LessonPlanEditorActivity,
} from './lessonPlanningApi.ts'
import './LessonPlanEditorPage.css'

export function LessonPlanEditorPage() {
  const { lessonPlanId = '' } = useParams()
  const [view, setView] = useState<LessonPlanEditor | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [pickerActivityId, setPickerActivityId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [candidates, setCandidates] = useState<LessonMaterialCandidate[]>([])
  const [candidateLoading, setCandidateLoading] = useState(false)
  const [savingKey, setSavingKey] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getLessonPlanEditor(lessonPlanId, controller.signal)
      .then(setView)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Plan sata trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [lessonPlanId])

  useEffect(() => {
    if (!pickerActivityId) return
    const controller = new AbortController()
    getLessonMaterialCandidates(lessonPlanId, pickerActivityId, search, controller.signal)
      .then((result) => setCandidates(result.items))
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Materijale trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setCandidateLoading(false) })
    return () => controller.abort()
  }, [lessonPlanId, pickerActivityId, search])

  async function attach(activityId: string, materialVersionId: string) {
    if (!view || savingKey) return
    setSavingKey(materialVersionId); setError(null)
    try {
      setView(await attachLessonMaterial(lessonPlanId, activityId, materialVersionId, view.rowVersion))
      setPickerActivityId(null); setSearch('')
    } catch (requestError) { setError(message(requestError, 'Materijal nije povezan.')) }
    finally { setSavingKey(null) }
  }

  async function detach(activityId: string, materialVersionId: string) {
    if (!view || savingKey) return
    setSavingKey(materialVersionId); setError(null)
    try { setView(await detachLessonMaterial(lessonPlanId, activityId, materialVersionId, view.rowVersion)) }
    catch (requestError) { setError(message(requestError, 'Materijal nije uklonjen iz plana.')) }
    finally { setSavingKey(null) }
  }

  if (loading) return <section className="lesson-editor-state"><p>Učitavanje plana sata…</p></section>
  if (!view) return <section className="lesson-editor-state"><h1>Plan sata nije dostupan</h1>{error && <p role="alert">{error}</p>}<Link to="/lesson-plans">Povratak na pripremu sata</Link></section>

  return (
    <section className="lesson-editor-page" aria-labelledby="lesson-editor-title">
      <nav className="lesson-editor-breadcrumb" aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><span>Plan sata</span></nav>
      <header className="lesson-editor-hero">
        <div><p className="lesson-editor-kicker">5.3 Plan sata</p><h1 id="lesson-editor-title">Plan sata za {view.participantName}</h1><p>Pregledajte plan i povežite točne verzije materijala s aktivnostima.</p></div>
        <div className="lesson-editor-actions"><Link to={`/lesson-plans/${lessonPlanId}/structure`}>Natrag</Link><span>Nacrt se sprema nakon svake promjene</span></div>
      </header>

      {error && <div className="lesson-editor-alert" role="alert">{error}</div>}
      <PlanSummary view={view} />

      <div className="lesson-editor-layout">
        <main className="lesson-editor-activities">
          <div className="lesson-editor-section-heading"><div><h2>Planirane aktivnosti ({view.targetDurationMinutes} min)</h2><p>Phase 9.6 čuva postojeći redoslijed i sadržaj; uređivanje aktivnosti dolazi u sljedećim fazama.</p></div></div>
          {view.activities.map((activity) => <ActivityCard
            key={activity.id}
            activity={activity}
            pickerOpen={pickerActivityId === activity.id}
            candidates={candidates}
            candidateLoading={candidateLoading}
            savingKey={savingKey}
            search={search}
            onSearch={(value) => { setCandidateLoading(true); setSearch(value) }}
            onTogglePicker={() => {
              setSearch('')
              if (pickerActivityId === activity.id) setPickerActivityId(null)
              else { setCandidates([]); setCandidateLoading(true); setPickerActivityId(activity.id) }
            }}
            onAttach={(versionId) => void attach(activity.id, versionId)}
            onDetach={(versionId) => void detach(activity.id, versionId)}
          />)}
          <button className="lesson-editor-future-action" type="button" disabled>+ Dodaj aktivnost — Phase 9.7</button>
        </main>

        <section className="lesson-editor-side">
          <article className="lesson-editor-card lesson-editor-materials">
            <div className="lesson-editor-card-title"><h2>Materijali i resursi</h2><span>{view.attachedMaterials.length}</span></div>
            {view.attachedMaterials.length === 0
              ? <p className="lesson-editor-empty">Materijali još nisu povezani. Dodajte ih uz odgovarajuću aktivnost.</p>
              : <ul>{view.attachedMaterials.map((material) => <li key={material.materialVersionId}><div><strong>{material.title}</strong><small>{material.materialTypeCode} · v{material.versionNumber}{material.isCurrentVersion ? '' : ' · povijesna verzija'}</small></div><Link to={`/materials/${material.materialId}`}>Pregled</Link></li>)}</ul>}
          </article>
          <article className="lesson-editor-card"><h2>Bilješke za sat</h2><p className="lesson-editor-empty">Uređivanje bilješki pripada Phase 9.8 i nije simulirano.</p></article>
        </section>

        <aside className="lesson-editor-side">
          <article className="lesson-editor-card"><h2>Fokus sata</h2><p>{view.goal}</p><div className="lesson-editor-chips">{view.knowledgeComponents.map((item) => <span key={item.id}>{item.name}<small>{item.modelCode} · {item.modelVersion}</small></span>)}</div></article>
          <article className="lesson-editor-card"><h2>Provjera balansa</h2><div className="lesson-editor-balance">{view.balance.map((area) => <BalanceRow key={area.code} area={area} />)}</div></article>
          <article className="lesson-editor-card lesson-editor-next"><h2>Sljedeći korak</h2><p>Odabir novih aktivnosti i Knowledge Blockova dolazi u Phase 9.7.</p><button type="button" disabled>Spremi i nastavi</button></article>
        </aside>
      </div>
    </section>
  )
}

function PlanSummary({ view }: { readonly view: LessonPlanEditor }) {
  return <section className="lesson-editor-summary">
    <div><small>{view.participantType === 'student' ? 'Učenik' : 'Grupa'}</small><strong>{view.participantName}</strong></div>
    <div><small>Termin</small><strong>{view.session ? formatDate(view.session.startsAtUtc) : 'Nije povezan'}</strong>{view.session && <span>{minutes(view.session.startsAtUtc, view.session.endsAtUtc)} min</span>}</div>
    <div><small>Cilj sata</small><strong>{view.goal}</strong></div>
    <div><small>Knowledge Components</small><strong>{view.knowledgeComponents.length}</strong><span>exact verzioniranih ciljeva</span></div>
  </section>
}

function ActivityCard(props: {
  readonly activity: LessonPlanEditorActivity
  readonly pickerOpen: boolean
  readonly candidates: LessonMaterialCandidate[]
  readonly candidateLoading: boolean
  readonly savingKey: string | null
  readonly search: string
  readonly onSearch: (value: string) => void
  readonly onTogglePicker: () => void
  readonly onAttach: (versionId: string) => void
  readonly onDetach: (versionId: string) => void
}) {
  const { activity } = props
  return <article className="lesson-editor-activity">
    <div className="lesson-editor-order">{activity.sortOrder + 1}</div>
    <div className="lesson-editor-activity-body">
      <div className="lesson-editor-activity-title"><div><h3>{activity.name}</h3><p>{purposeLabel(activity.purpose)}{activity.knowledgeBlockCode ? ` · ${activity.knowledgeBlockCode}` : ''}</p></div><strong>{activity.durationMinutes} min</strong></div>
      <div className="lesson-editor-activity-materials">
        {activity.materials.map((material) => <span key={material.materialVersionId}><span><strong>{material.title}</strong><small>v{material.versionNumber} · {material.matchingKnowledgeComponentCount} KC podudaranja</small></span><button type="button" aria-label={`Ukloni ${material.title}`} disabled={props.savingKey === material.materialVersionId} onClick={() => props.onDetach(material.materialVersionId)}>×</button></span>)}
        <button type="button" onClick={props.onTogglePicker}>{props.pickerOpen ? 'Zatvori materijale' : '+ Dodaj materijal'}</button>
      </div>
      {props.pickerOpen && <section className="lesson-editor-picker" aria-label={`Materijali za ${activity.name}`}>
        <label><span>Pretraži biblioteku</span><input type="search" value={props.search} onChange={(event) => props.onSearch(event.target.value)} placeholder="Naziv, predmet ili vrsta" /></label>
        {props.candidateLoading ? <p>Učitavanje materijala…</p> : props.candidates.length === 0 ? <p>Nema dostupnih materijala.</p> : <ul>{props.candidates.map((candidate) => <li key={candidate.materialVersionId}><div><strong>{candidate.title}</strong><small>{candidate.materialTypeCode} · v{candidate.versionNumber} · {candidate.matchingKnowledgeComponentCount} KC podudaranja</small></div><button type="button" disabled={candidate.isAttached || props.savingKey === candidate.materialVersionId} onClick={() => props.onAttach(candidate.materialVersionId)}>{candidate.isAttached ? 'Povezano' : 'Dodaj'}</button></li>)}</ul>}
      </section>}
    </div>
  </article>
}

function BalanceRow({ area }: { readonly area: LessonBalanceArea }) {
  const normalized = typeof area.status === 'number' ? ['Unknown', 'Satisfied', 'Warning', 'NotEvaluated'][area.status] : area.status
  return <div className={`lesson-editor-balance-row lesson-editor-balance-row--${normalized.toLowerCase()}`}><span aria-hidden="true">{normalized === 'Satisfied' ? '✓' : normalized === 'Warning' ? '!' : '–'}</span><div><strong>{area.label}</strong><p>{area.message}</p></div></div>
}

function purposeLabel(value: string) {
  return ({ Engagement: 'Uključivanje', ActivatePriorKnowledge: 'Aktivacija predznanja', Instruction: 'Poučavanje', Practice: 'Vježba', CheckForUnderstanding: 'Provjera razumijevanja', Other: 'Ostalo' } as Record<string, string>)[value] ?? value
}

function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
function minutes(start: string, end: string) { return Math.round((new Date(end).getTime() - new Date(start).getTime()) / 60_000) }
function message(error: unknown, fallback: string) { return error instanceof ApiError ? error.message : fallback }
