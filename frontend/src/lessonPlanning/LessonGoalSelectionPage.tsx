import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  createLessonGoal,
  getLessonKnowledgeComponents,
  getLessonParticipants,
  getLessonSessions,
  type LessonKnowledgeComponent,
  type LessonParticipant,
  type LessonSession,
  type ParticipantType,
} from './lessonPlanningApi.ts'
import './LessonGoalSelectionPage.css'

export function LessonGoalSelectionPage() {
  const [participantType, setParticipantType] = useState<ParticipantType>('student')
  const [participantSearch, setParticipantSearch] = useState('')
  const [participants, setParticipants] = useState<LessonParticipant[]>([])
  const [participantId, setParticipantId] = useState('')
  const [sessions, setSessions] = useState<LessonSession[]>([])
  const [sessionId, setSessionId] = useState('')
  const [goal, setGoal] = useState('')
  const [knowledgeSearch, setKnowledgeSearch] = useState('')
  const [components, setComponents] = useState<LessonKnowledgeComponent[]>([])
  const [selectedComponents, setSelectedComponents] = useState<LessonKnowledgeComponent[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [savedPlanId, setSavedPlanId] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getLessonParticipants(participantType, participantSearch, controller.signal)
      .then((page) => setParticipants(page.items))
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Učenike i grupe trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [participantType, participantSearch])

  useEffect(() => {
    const controller = new AbortController()
    if (!participantId) return () => controller.abort()
    getLessonSessions(participantType, participantId, controller.signal)
      .then(setSessions)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Termine trenutačno nije moguće učitati.')
      })
    return () => controller.abort()
  }, [participantId, participantType])

  useEffect(() => {
    const controller = new AbortController()
    getLessonKnowledgeComponents(knowledgeSearch, controller.signal)
      .then((page) => setComponents(page.items))
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Knowledge Components trenutačno nije moguće učitati.')
      })
    return () => controller.abort()
  }, [knowledgeSearch])

  const selectedParticipant = participants.find((item) => item.id === participantId)
  const visibleComponents = useMemo(() => {
    const selectedIds = new Set(selectedComponents.map((item) => item.id))
    return [...selectedComponents, ...components.filter((item) => !selectedIds.has(item.id))]
  }, [components, selectedComponents])

  function changeParticipantType(value: ParticipantType) {
    setParticipantType(value)
    setLoading(true)
    setParticipantId('')
    setSessionId('')
    setParticipants([])
    setSavedPlanId(null)
  }

  function toggleComponent(component: LessonKnowledgeComponent) {
    setSelectedComponents((current) => current.some((item) => item.id === component.id)
      ? current.filter((item) => item.id !== component.id)
      : current.length >= 50 ? current : [...current, component])
    setSavedPlanId(null)
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    if (!participantId || !goal.trim() || selectedComponents.length === 0) {
      setError('Odaberite učenika ili grupu, unesite cilj sata i odaberite barem jednu komponentu znanja.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      const result = await createLessonGoal({
        participantType,
        participantId,
        sessionId: sessionId || null,
        goal: goal.trim(),
        knowledgeComponentIds: selectedComponents.map((item) => item.id),
      })
      setSavedPlanId(result.lessonPlanId)
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Cilj sata trenutačno nije moguće spremiti.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="lesson-goal-page" aria-labelledby="lesson-goal-title">
      <nav className="lesson-goal-breadcrumb" aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><span>Početak pripreme sata</span></nav>
      <header className="lesson-goal-hero">
        <div><h1 id="lesson-goal-title">5.1 Početak pripreme sata — Cilj sata</h1><p>Definirajte za koga pripremate sat, glavni cilj i Knowledge Components koje ćete obrađivati.</p></div>
        <div className="lesson-goal-hero__actions"><Link to="/">Odustani i izađi</Link><button type="submit" form="lesson-goal-form" disabled={saving}>{saving ? 'Spremanje…' : 'Spremi cilj sata'}</button></div>
      </header>

      {error && <div className="lesson-goal-alert" role="alert">{error}</div>}
      {savedPlanId && <div className="lesson-goal-success" role="status"><strong>Cilj sata je spremljen kao Draft.</strong><span>Sljedeći korak, prijedlog strukture i balansa, dolazi u Phase 9.5.</span></div>}

      <form id="lesson-goal-form" onSubmit={(event) => void submit(event)}>
        <section className="lesson-goal-context" aria-labelledby="participant-heading">
          <div><h2 id="participant-heading">Kome pripremate sat?</h2><div className="lesson-goal-segmented">
            <button type="button" aria-pressed={participantType === 'student'} onClick={() => changeParticipantType('student')}>♙ Pojedini učenik</button>
            <button type="button" aria-pressed={participantType === 'group'} onClick={() => changeParticipantType('group')}>♟ Grupa učenika</button>
          </div></div>
          <div className="lesson-goal-participant-field"><label htmlFor="lesson-participant">{participantType === 'student' ? 'Odaberite učenika' : 'Odaberite grupu'}</label><input aria-label={participantType === 'student' ? 'Pretražite učenike' : 'Pretražite grupe'} value={participantSearch} maxLength={100} placeholder="Pretražite…" onChange={(event) => { setLoading(true); setParticipantSearch(event.target.value) }} /><select id="lesson-participant" required disabled={loading} value={participantId} onChange={(event) => { setParticipantId(event.target.value); setSessions([]); setSessionId(''); setSavedPlanId(null) }}><option value="">Odaberite</option>{participants.map((item) => <option key={item.id} value={item.id}>{item.name} · {item.schoolGrade}</option>)}</select></div>
          <label><span>Termin (opcionalno)</span><select disabled={!participantId} value={sessionId} onChange={(event) => { setSessionId(event.target.value); setSavedPlanId(null) }}><option value="">Pripremi bez termina</option>{sessions.map((item) => <option key={item.id} value={item.id}>{formatSession(item)}</option>)}</select></label>
        </section>

        <div className="lesson-goal-layout">
          <div className="lesson-goal-main">
            <section className="lesson-goal-card">
              <h2><span>1</span> Cilj sata</h2>
              <div className="lesson-goal-ai"><strong>✦ AI prijedlog cilja sata nije uključen</strong><p>Phase 9.2 zaključava AI kao default-disabled. Ručno uneseni cilj ostaje jedini canonical sadržaj i ne stvara dodatni trošak.</p></div>
              <label className="lesson-goal-text"><span>Unesite vlastiti cilj sata</span><textarea required rows={4} maxLength={300} value={goal} onChange={(event) => { setGoal(event.target.value); setSavedPlanId(null) }} placeholder="Primjer: Učenik će pravilno razlikovati i koristiti…" /><small>{goal.length} / 300</small></label>
            </section>

            <section className="lesson-goal-card">
              <h2><span>2</span> Knowledge Components <small>(što će se ovim satom obrađivati?)</small></h2>
              <label className="lesson-goal-search"><span>Pretražite komponente znanja</span><input maxLength={100} value={knowledgeSearch} onChange={(event) => setKnowledgeSearch(event.target.value)} placeholder="Naziv komponente ili područja" /></label>
              {visibleComponents.length === 0 ? <p className="lesson-goal-empty">Nema dostupnih aktivnih komponenti iz objavljenog Knowledge Modela.</p> : <div className="lesson-goal-components">{visibleComponents.map((component) => {
                const selected = selectedComponents.some((item) => item.id === component.id)
                return <label key={component.id} className={selected ? 'lesson-goal-component lesson-goal-component--selected' : 'lesson-goal-component'}><input type="checkbox" checked={selected} onChange={() => toggleComponent(component)} /><strong>{component.name}</strong><span>{component.areaName}</span><small>{component.modelCode} · {component.modelVersion}</small></label>
              })}</div>}
            </section>
          </div>

          <aside className="lesson-goal-aside" aria-label="Sažetak pripreme">
            <section><h2>Kontekst {participantType === 'student' ? 'učenika' : 'grupe'}</h2><dl><dt>Odabir</dt><dd>{selectedParticipant?.name ?? 'Nije odabran'}</dd><dt>Program</dt><dd>{selectedParticipant?.programName ?? 'Nije postavljen'}</dd><dt>Razred</dt><dd>{selectedParticipant?.schoolGrade ?? '—'}</dd>{participantType === 'group' && <><dt>Učenici</dt><dd>{selectedParticipant?.memberCount ?? 0}</dd></>}</dl></section>
            <section><h2>Odabrano znanje</h2>{selectedComponents.length ? <ul>{selectedComponents.map((item) => <li key={item.id}>{item.name}<small>{item.modelCode} · {item.modelVersion}</small></li>)}</ul> : <p>Odaberite najmanje jednu komponentu znanja.</p>}</section>
            <section><h2>Dostupni kontekst</h2><p>Knowledge Model je dostupan. Domaće zadaće, ispiti i AI obrazloženje nisu prikazani bez implementiranog izvornog contracta.</p></section>
          </aside>
        </div>
      </form>
      <footer className="lesson-goal-note">ⓘ Cilj sata možete mijenjati u kasnijoj authoring fazi dok je plan Draft. Planiranje ne stvara Attempt, EvidenceEvent ni promjenu Mastery/Readiness.</footer>
    </section>
  )
}

function formatSession(session: LessonSession) {
  const start = new Date(session.startsAtUtc)
  const date = new Intl.DateTimeFormat('hr-HR', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric', timeZone: session.timeZoneId }).format(start)
  const time = new Intl.DateTimeFormat('hr-HR', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: session.timeZoneId }).format(start)
  return `${date} u ${time}${session.title ? ` · ${session.title}` : ''}`
}
