import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  addLessonActivity,
  getLessonActivityCandidates,
  getLessonPlanEditor,
  type LessonActivityCandidate,
  type LessonPlanEditor,
} from './lessonPlanningApi.ts'
import './LessonActivitySelectionPage.css'

type Tab = 'blocks' | 'activities'

export function LessonActivitySelectionPage() {
  const { lessonPlanId = '' } = useParams()
  const navigate = useNavigate()
  const [view, setView] = useState<LessonPlanEditor | null>(null)
  const [candidates, setCandidates] = useState<LessonActivityCandidate[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>('blocks')
  const [search, setSearch] = useState('')
  const [maximumDuration, setMaximumDuration] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [candidateLoading, setCandidateLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
    if (tab !== 'blocks') return
    const controller = new AbortController()
    getLessonActivityCandidates(lessonPlanId, search, maximumDuration, controller.signal)
      .then((items) => {
        setCandidates(items)
        setSelectedId((current) => items.some((item) => item.knowledgeBlockVersionId === current)
          ? current
          : items[0]?.knowledgeBlockVersionId ?? null)
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Knowledge Block katalog trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setCandidateLoading(false) })
    return () => controller.abort()
  }, [lessonPlanId, search, maximumDuration, tab])

  const selected = useMemo(() => candidates.find((item) =>
    item.knowledgeBlockVersionId === selectedId) ?? null, [candidates, selectedId])

  async function addSelected() {
    if (!view || !selected || saving) return
    setSaving(true)
    setError(null)
    try {
      await addLessonActivity(lessonPlanId, selected.knowledgeBlockVersionId, view.rowVersion)
      navigate(`/lesson-plans/${lessonPlanId}/editor`)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Aktivnost nije dodana u plan.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <section className="activity-selection-state"><p>Učitavanje odabira aktivnosti…</p></section>
  if (!view) return <section className="activity-selection-state"><h1>Odabir aktivnosti nije dostupan</h1>{error && <p role="alert">{error}</p>}<Link to="/lesson-plans">Povratak na pripremu sata</Link></section>

  return <section className="activity-selection-page" aria-labelledby="activity-selection-title">
    <nav className="activity-selection-breadcrumb" aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><Link to={`/lesson-plans/${lessonPlanId}/editor`}>Plan sata</Link><span>›</span><span>Dodaj aktivnost</span></nav>
    <header className="activity-selection-hero">
      <div><p>5.4 Odabir aktivnosti / Knowledge Blocka</p><h1 id="activity-selection-title">Odabir aktivnosti / Knowledge Blocka</h1><span>Odaberite odobreni blok koji odgovara cilju i komponentama znanja plana.</span></div>
      <Link to={`/lesson-plans/${lessonPlanId}/editor`} aria-label="Zatvori odabir aktivnosti">×</Link>
    </header>
    {error && <div className="activity-selection-alert" role="alert">{error}</div>}
    <PlanContext view={view} />

    <div className="activity-selection-tabs" role="tablist" aria-label="Vrsta kandidata">
      <button type="button" role="tab" aria-selected={tab === 'blocks'} onClick={() => { setCandidateLoading(true); setTab('blocks') }}>Knowledge Blocks</button>
      <button type="button" role="tab" aria-selected={tab === 'activities'} onClick={() => setTab('activities')}>Aktivnosti</button>
    </div>

    {tab === 'activities' ? <section className="activity-selection-no-catalog">
      <h2>Nema objavljenih predložaka aktivnosti</h2>
      <p>PLUS 5 još nema odobreni Activity Template katalog. Nova prilagođena aktivnost pripada Phase 9.8 i nije simulirana.</p>
    </section> : <div className="activity-selection-workspace">
      <aside className="activity-selection-filters">
        <div><strong>1</strong><h2>Filtrirajte blokove</h2></div>
        <label><span>Pretraži katalog</span><input type="search" value={search} onChange={(event) => { setCandidateLoading(true); setSearch(event.target.value) }} placeholder="Naziv ili KB code" /></label>
        <label><span>Dostupno vrijeme</span><select value={maximumDuration ?? ''} onChange={(event) => { setCandidateLoading(true); setMaximumDuration(event.target.value ? Number(event.target.value) : null) }}><option value="">Sva trajanja</option><option value="10">Do 10 min</option><option value="15">Do 15 min</option><option value="30">Do 30 min</option><option value="60">Do 60 min</option></select></label>
        <p>Prikazuju se samo current Published blokovi koji su hard-eligible za ovaj plan.</p>
      </aside>

      <main className="activity-selection-results">
        <div className="activity-selection-heading"><div><strong>2</strong><h2>Odaberite Knowledge Block</h2></div><span>{candidates.length} dostupno</span></div>
        {candidateLoading ? <p className="activity-selection-empty">Učitavanje kataloga…</p>
          : candidates.length === 0 ? <p className="activity-selection-empty">Nema blokova koji odgovaraju filtrima.</p>
            : candidates.map((candidate) => <button className="activity-selection-candidate" data-selected={candidate.knowledgeBlockVersionId === selectedId} type="button" key={candidate.knowledgeBlockVersionId} onClick={() => setSelectedId(candidate.knowledgeBlockVersionId)}>
              <span className="activity-selection-code">{candidate.code}</span><span><strong>{candidate.name}</strong><small>Published v{candidate.versionNumber} · {candidate.minDurationMinutes}–{candidate.maxDurationMinutes} min</small><small>{participantLabel(candidate.participantApplicability)} · optimalno {candidate.optimalDurationMinutes} min</small></span><b aria-hidden="true">+</b>{candidate.existingActivityCount > 0 && <em>Već u planu: {candidate.existingActivityCount}</em>}
            </button>)}
      </main>

      <aside className="activity-selection-preview">
        <div className="activity-selection-heading"><div><strong>3</strong><h2>Pregled odabira</h2></div></div>
        {selected ? <>
          <span className="activity-selection-code">{selected.code}</span>
          <h3>{selected.name}</h3>
          <p>U plan će se dodati nova aktivnost na kraj trenutačnog redoslijeda.</p>
          <dl><div><dt>Trajanje</dt><dd>{selected.optimalDurationMinutes} min <small>({selected.minDurationMinutes}–{selected.maxDurationMinutes})</small></dd></div><div><dt>Način rada</dt><dd>{participantLabel(selected.participantApplicability)}</dd></div><div><dt>Knowledge Components</dt><dd>{view.knowledgeComponents.length} odabranih komponenti</dd></div><div><dt>Procjenjivanje</dt><dd>{assessmentLabel(selected.assessmentCapability)}</dd></div></dl>
          {selected.existingActivityCount > 0 && <p className="activity-selection-warning">Ovaj blok već postoji u planu. Ponovljeni blok je dopušten, a balans će ostati vidljiv u editoru.</p>}
        </> : <p className="activity-selection-empty">Odaberite blok za pregled.</p>}
      </aside>
    </div>}

    <footer className="activity-selection-footer"><Link to={`/lesson-plans/${lessonPlanId}/editor`}>Odustani</Link><span>Aktivnost se dodaje u Draft; ne nastaju Attempt ni Evidence.</span><button type="button" disabled={tab !== 'blocks' || !selected || saving} onClick={() => void addSelected()}>{saving ? 'Dodavanje…' : 'Dodaj u plan'}</button></footer>
  </section>
}

function PlanContext({ view }: { readonly view: LessonPlanEditor }) {
  return <section className="activity-selection-context"><div><small>{view.participantType === 'student' ? 'Učenik' : 'Grupa'}</small><strong>{view.participantName}</strong></div><div><small>Cilj sata</small><strong>{view.goal}</strong></div><div><small>Odabrani Knowledge Components</small><span>{view.knowledgeComponents.map((item) => <b key={item.id}>{item.name}</b>)}</span></div><div><small>Raspoloživo vrijeme</small><strong>{view.targetDurationMinutes} min</strong></div></section>
}

function participantLabel(value: string) {
  return value === 'Individual' ? 'Individualno' : value === 'Group' ? 'Grupno' : 'Individualno ili grupno'
}

function assessmentLabel(value: string) {
  return value === 'NotAssessment' ? 'Nije procjenjivanje' : value === 'AssessmentFocused' ? 'Usmjereno na procjenu' : 'Može sadržavati procjenu'
}
