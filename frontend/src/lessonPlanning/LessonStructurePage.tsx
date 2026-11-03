import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { applyLessonBaseline, changeLessonTargetDuration, getLessonStructure, type LessonBalanceArea, type LessonStructure } from './lessonPlanningApi.ts'
import './LessonStructurePage.css'

export function LessonStructurePage() {
  const { lessonPlanId = '' } = useParams()
  const [view, setView] = useState<LessonStructure | null>(null)
  const [duration, setDuration] = useState('60')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getLessonStructure(lessonPlanId, controller.signal)
      .then((result) => { setView(result); setDuration(String(result.targetDurationMinutes)) })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Prijedlog strukture trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [lessonPlanId])

  async function saveDuration() {
    if (!view || saving) return
    const parsed = Number(duration)
    if (!Number.isInteger(parsed) || parsed < 5 || parsed > 480) {
      setError('Ciljano trajanje mora biti između 5 i 480 minuta.')
      return
    }
    setSaving(true); setError(null)
    try { setView(await changeLessonTargetDuration(lessonPlanId, parsed, view.rowVersion)) }
    catch (requestError) { setError(message(requestError, 'Trajanje nije spremljeno.')) }
    finally { setSaving(false) }
  }

  async function applySuggestion() {
    if (!view || saving) return
    setSaving(true); setError(null)
    try { setView(await applyLessonBaseline(lessonPlanId, view.rowVersion)) }
    catch (requestError) { setError(message(requestError, 'Prijedlog nije primijenjen.')) }
    finally { setSaving(false) }
  }

  if (loading) return <section className="lesson-structure-state"><p>Učitavanje prijedloga strukture…</p></section>
  if (!view) return <section className="lesson-structure-state"><h1>Prijedlog strukture nije dostupan</h1>{error && <p role="alert">{error}</p>}<Link to="/lesson-plans">Povratak na cilj sata</Link></section>

  return (
    <section className="lesson-structure-page" aria-labelledby="lesson-structure-title">
      <nav className="lesson-structure-breadcrumb" aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><span>Prijedlog strukture i sadržaja</span></nav>
      <header className="lesson-structure-hero">
        <div><p className="lesson-structure-kicker">5.2 Prijedlog strukture i sadržaja sata</p><h1 id="lesson-structure-title">Plan sata za {view.participantName}</h1><p>{view.goal}</p></div>
        <div className="lesson-structure-actions"><Link to="/lesson-plans">Natrag</Link><button type="button" disabled={!view.canApply || saving} onClick={() => void applySuggestion()}>{view.isApplied ? 'Prijedlog je primijenjen' : saving ? 'Primjena…' : 'Primijeni prijedlog'}</button></div>
      </header>

      {error && <div className="lesson-structure-alert" role="alert">{error}</div>}
      <div className="lesson-structure-notice"><strong>Početni prijedlog</strong><span>Deterministička politika {view.policyCode}; AI i vanjski provider su isključeni. Nema dodatne naplate.</span></div>

      <div className="lesson-structure-tabs" aria-label="Koraci pripreme"><strong>Struktura sata</strong><span>Aktivnosti</span><span>Materijali</span><span>Balans sata</span></div>

      <div className="lesson-structure-grid">
        <main>
          <section className="lesson-structure-card lesson-structure-duration">
            <div><h2>Ciljano trajanje</h2><p>{view.isApplied ? 'Promjena cilja ne mijenja automatski trajanja primijenjenih aktivnosti.' : 'Promjena cilja ponovno izračunava samo preview.'}</p></div>
            <label><span>Minute</span><input type="number" min="5" max="480" value={duration} onChange={(event) => setDuration(event.target.value)} /></label>
            <button type="button" disabled={saving || Number(duration) === view.targetDurationMinutes} onClick={() => void saveDuration()}>Spremi trajanje</button>
          </section>

          <section className="lesson-structure-card">
            <div className="lesson-structure-section-heading"><div><h2>Vremenska crta sata</h2><p>{view.isApplied ? 'Canonical aktivnosti spremljene su u Draft plan.' : 'Preview možete prihvatiti ili odbaciti bez promjene plana.'}</p></div><strong>{view.activities.reduce((sum, item) => sum + item.durationMinutes, 0)} min</strong></div>
            <ol className="lesson-structure-timeline">{view.activities.map((activity) => <li key={`${activity.sortOrder}-${activity.name}`}>
              <span className="lesson-structure-index">{activity.sortOrder + 1}</span>
              <div><div className="lesson-structure-activity-title"><h3>{activity.name}</h3><strong>{activity.durationMinutes} min</strong></div><p>{purposeLabel(activity.purpose)}{activity.knowledgeBlockCode ? ` · ${activity.knowledgeBlockCode}` : ' · Bez KB klasifikacije'}</p>{activity.knowledgeComponentIds.length > 0 && <small>{activity.knowledgeComponentIds.length} odabranih komponenti znanja</small>}{activity.warningCodes.map((code) => <span className="lesson-structure-warning" key={code}>{warningLabel(code)}</span>)}</div>
            </li>)}</ol>
          </section>

          <section className="lesson-structure-card"><h2>Odabrano znanje</h2><div className="lesson-structure-chips">{view.knowledgeComponents.map((component) => <span key={component.id}>{component.name}<small>{component.modelCode} · {component.modelVersion}</small></span>)}</div></section>
        </main>

        <aside>
          <section className="lesson-structure-card"><h2>Balans sata</h2><div className="lesson-structure-balance">{view.balance.map((area) => <BalanceRow key={area.code} area={area} />)}</div></section>
          <section className="lesson-structure-card lesson-structure-materials"><div><h2>Materijali</h2><p>Stvarni kandidati s podudarnim Knowledge Components.</p></div><strong>{view.materialCandidateCount}</strong><small>Prikaz dostupnosti nije odabir ni povezivanje materijala. To dolazi u Phase 9.6.</small></section>
          <section className="lesson-structure-card lesson-structure-ai"><h2>AI pomoć</h2><p>Isključena je. Ovaj ekran ne poziva OpenAI ni drugi plaćeni provider i ne šalje podatke učenika izvan aplikacije.</p></section>
        </aside>
      </div>
    </section>
  )
}

function BalanceRow({ area }: { readonly area: LessonBalanceArea }) {
  const normalized = typeof area.status === 'number' ? ['Unknown', 'Satisfied', 'Warning', 'NotEvaluated'][area.status] : area.status
  return <div className={`lesson-structure-balance__row lesson-structure-balance__row--${normalized.toLowerCase()}`}><span aria-hidden="true">{normalized === 'Satisfied' ? '✓' : normalized === 'Warning' ? '!' : '–'}</span><div><strong>{area.label}</strong><p>{area.message}</p></div></div>
}

function purposeLabel(value: string) {
  return ({ Engagement: 'Uključivanje', ActivatePriorKnowledge: 'Aktivacija predznanja', Instruction: 'Poučavanje', Practice: 'Vježba', CheckForUnderstanding: 'Provjera razumijevanja', Other: 'Ostalo' } as Record<string, string>)[value] ?? value
}

function warningLabel(code: string) {
  return code === 'KB_DURATION_BELOW_RECOMMENDED' ? 'Kraće od preporučenog raspona' : 'Dulje od preporučenog raspona'
}

function message(error: unknown, fallback: string) { return error instanceof ApiError ? error.message : fallback }
