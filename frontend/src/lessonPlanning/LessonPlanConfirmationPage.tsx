import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  confirmLessonPlan,
  createEditableLessonPlanDraft,
  getLessonPlanConfirmation,
  type LessonBalanceArea,
  type LessonPlanConfirmation,
} from './lessonPlanningApi.ts'
import './LessonPlanConfirmationPage.css'

export function LessonPlanConfirmationPage() {
  const { lessonPlanId = '' } = useParams()
  const navigate = useNavigate()
  const [view, setView] = useState<LessonPlanConfirmation | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getLessonPlanConfirmation(lessonPlanId, controller.signal)
      .then(setView)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Potvrđenu pripremu trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [lessonPlanId])

  async function confirm() {
    if (!view || saving) return
    setSaving(true); setError(null)
    try { setView(await confirmLessonPlan(lessonPlanId, view.planRowVersion, view.versionRowVersion)) }
    catch (requestError) { setError(message(requestError, 'Pripremu nije moguće potvrditi. Osvježite stranicu i pokušajte ponovno.')) }
    finally { setSaving(false) }
  }

  async function edit() {
    if (!view || saving) return
    if (view.status === 'Draft') { navigate(`/lesson-plans/${lessonPlanId}/editor`); return }
    setSaving(true); setError(null)
    try {
      await createEditableLessonPlanDraft(lessonPlanId, view.planRowVersion)
      navigate(`/lesson-plans/${lessonPlanId}/editor`)
    } catch (requestError) { setError(message(requestError, 'Novi nacrt nije moguće otvoriti.')) }
    finally { setSaving(false) }
  }

  if (loading) return <section className="lesson-confirmation-state"><p>Učitavanje pripreme…</p></section>
  if (!view) return <section className="lesson-confirmation-state"><h1>Priprema nije dostupna</h1>{error && <p role="alert">{error}</p>}<Link to="/lesson-plans">Povratak na pripremu sata</Link></section>

  const ready = view.status === 'Ready'
  return <section className="lesson-confirmation-page" aria-labelledby="lesson-confirmation-title">
    <nav className="lesson-confirmation-breadcrumb" aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><Link to={`/lesson-plans/${lessonPlanId}/editor`}>Plan sata</Link><span>›</span><span>Potvrda</span></nav>
    <header className="lesson-confirmation-hero">
      <div><p className="lesson-confirmation-kicker">5.6 Potvrđena priprema</p><h1 id="lesson-confirmation-title">{ready ? 'Sat je spreman' : 'Provjerite pripremu sata'}</h1><p>{ready ? 'Plan je zaključan kao verzionirani Ready snapshot.' : 'Pregledajte sve dijelove plana prije potvrde.'}</p></div>
      <span className={`lesson-confirmation-status lesson-confirmation-status--${ready ? 'ready' : 'draft'}`}>{ready ? 'Spreman' : 'Nacrt'}</span>
    </header>

    {error && <div className="lesson-confirmation-alert" role="alert">{error}</div>}
    <section className="lesson-confirmation-summary">
      <div><small>{view.participantType === 'student' ? 'Učenik' : 'Grupa'}</small><strong>{view.participantName}</strong></div>
      <div><small>Termin</small><strong>{view.session ? formatDate(view.session.startsAtUtc) : 'Nije povezan'}</strong></div>
      <div><small>Cilj sata</small><strong>{view.goal}</strong></div>
      <div><small>Knowledge Components</small><strong>{view.knowledgeComponents.length}</strong><span>verzioniranih ciljeva</span></div>
    </section>

    <div className="lesson-confirmation-layout">
      <main>
        <section className="lesson-confirmation-card">
          <div className="lesson-confirmation-heading"><div><h2>Plan sata</h2><p>{view.plannedDurationMinutes} od {view.targetDurationMinutes} min · verzija {view.versionNumber}</p></div><span>{view.activities.length} aktivnosti</span></div>
          <ol className="lesson-confirmation-activities">{view.activities.map(activity => <li key={activity.id}>
            <span>{activity.sortOrder + 1}</span><div><strong>{activity.name}</strong><small>{purposeLabel(activity.purpose)} · {workModeLabel(activity.workMode)}</small>{activity.materials.length > 0 && <p>{activity.materials.map(item => `${item.title} v${item.versionNumber}`).join(' · ')}</p>}</div><b>{activity.durationMinutes} min</b>
          </li>)}</ol>
        </section>
        <section className="lesson-confirmation-card"><h2>Materijali i resursi</h2>{view.materials.length === 0 ? <p className="lesson-confirmation-empty">Plan nema povezanih materijala.</p> : <ul className="lesson-confirmation-materials">{view.materials.map(material => <li key={material.materialVersionId}><div><strong>{material.title}</strong><small>{material.materialTypeCode} · točna verzija v{material.versionNumber}{material.isCurrentVersion ? '' : ' · povijesna'}</small></div><Link to={`/materials/${material.materialId}`}>Pregled</Link></li>)}</ul>}</section>
      </main>
      <aside>
        <section className="lesson-confirmation-card"><h2>Komponente znanja</h2><div className="lesson-confirmation-chips">{view.knowledgeComponents.map(item => <span key={item.id}>{item.name}<small>{item.modelCode} · {item.modelVersion}</small></span>)}</div></section>
        <section className="lesson-confirmation-card"><h2>Provjera balansa</h2><div className="lesson-confirmation-balance">{view.balance.map(item => <BalanceRow key={item.code} item={item} />)}</div></section>
        <section className="lesson-confirmation-card"><h2>Procjena prije sata</h2><p className="lesson-confirmation-empty">Nema jedinstvene ukupne procjene. Detaljne verzionirane projekcije dostupne su u dosjeu učenika.</p></section>
      </aside>
    </div>

    {view.validationMessages.length > 0 && <section className="lesson-confirmation-validation" role="alert"><strong>Prije potvrde dovršite:</strong><ul>{view.validationMessages.map(item => <li key={item}>{item}</li>)}</ul></section>}
    <footer className="lesson-confirmation-actions ds-page-actions--below-global-profile">
      <button type="button" className="lesson-confirmation-secondary" onClick={() => void edit()} disabled={!view.canEdit || saving}>{ready ? 'Uredi pripremu' : 'Povratak na uređivanje'}</button>
      {ready ? <button type="button" className="lesson-confirmation-primary" disabled title="Pokretanje sata dolazi u Phase 10">Pokreni sat na PLUS 5 Ploči · Phase 10</button> : <button type="button" className="lesson-confirmation-primary" onClick={() => void confirm()} disabled={!view.canConfirm || saving}>{saving ? 'Potvrđivanje…' : 'Potvrdi pripremu'}</button>}
    </footer>
  </section>
}

function BalanceRow({ item }: { readonly item: LessonBalanceArea }) {
  const status = typeof item.status === 'number' ? ['Unknown', 'Satisfied', 'Warning', 'NotEvaluated'][item.status] : item.status
  return <div className={`lesson-confirmation-balance-row lesson-confirmation-balance-row--${status.toLowerCase()}`}><span>{status === 'Satisfied' ? '✓' : status === 'Warning' ? '!' : '–'}</span><div><strong>{item.label}</strong><p>{item.message}</p></div></div>
}
function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
function purposeLabel(value: string) { return ({ Engagement: 'Uključivanje', ActivatePriorKnowledge: 'Aktivacija predznanja', Instruction: 'Poučavanje', Practice: 'Vježba', CheckForUnderstanding: 'Provjera razumijevanja', Other: 'Ostalo' } as Record<string, string>)[value] ?? value }
function workModeLabel(value: string) { return ({ Individual: 'Samostalni rad', Pair: 'Rad u paru', Group: 'Grupni rad' } as Record<string, string>)[value] ?? value }
function message(error: unknown, fallback: string) { return error instanceof ApiError ? error.message : fallback }
