import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { getLessonSession, reorderLessonSessionTimeline, type LessonSession } from './boardApi.ts'
import './TeacherBoardPage.css'

type SaveState = 'saved' | 'saving' | 'offline' | 'conflict'

export function TeacherBoardPage() {
  const { lessonSessionId = '' } = useParams()
  const [view, setView] = useState<LessonSession | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saveState, setSaveState] = useState<SaveState>(navigator.onLine ? 'saved' : 'offline')
  const [clock, setClock] = useState(() => Date.now())

  useEffect(() => {
    const controller = new AbortController()
    getLessonSession(lessonSessionId, controller.signal).then(setView)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Sat trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [lessonSessionId])

  useEffect(() => {
    const interval = window.setInterval(() => setClock(Date.now()), 1_000)
    const online = () => setSaveState('saved')
    const offline = () => setSaveState('offline')
    window.addEventListener('online', online); window.addEventListener('offline', offline)
    return () => { window.clearInterval(interval); window.removeEventListener('online', online); window.removeEventListener('offline', offline) }
  }, [])

  const elapsed = useMemo(() => view ? formatElapsed(clock - new Date(view.startedAtUtc).getTime()) : '00:00:00', [view, clock])

  async function move(activityId: string, offset: -1 | 1) {
    if (!view || saveState === 'saving') return
    if (!navigator.onLine) { setSaveState('offline'); return }
    const current = view.activities.findIndex(item => item.id === activityId)
    const next = current + offset
    if (current < 0 || next < 0 || next >= view.activities.length) return
    const ids = view.activities.map(item => item.id)
    ;[ids[current], ids[next]] = [ids[next], ids[current]]
    setSaveState('saving'); setError(null)
    try {
      const updated = await reorderLessonSessionTimeline(view.id, view.revision, ids, crypto.randomUUID())
      setView(updated); setSaveState('saved')
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.code === 'lesson_session_concurrency_conflict') {
        setSaveState('conflict')
        try { setView(await getLessonSession(view.id)) } catch { /* canonical reload remains available through refresh */ }
      } else {
        setError('Promjenu redoslijeda nije moguće spremiti.'); setSaveState(navigator.onLine ? 'conflict' : 'offline')
      }
    }
  }

  if (loading) return <section className="board-state">Učitavanje PLUS 5 Ploče…</section>
  if (!view) return <section className="board-state"><h1>Sat nije dostupan</h1>{error && <p role="alert">{error}</p>}<Link to="/board">Povratak na aktivne satove</Link></section>

  return <section className="teacher-board" aria-labelledby="teacher-board-title">
    <nav aria-label="Putanja"><Link to="/board">PLUS 5 Ploča</Link><span>›</span><span>Aktivan sat</span></nav>
    <header className="teacher-board-hero">
      <div><p>6.1 Ploča učitelja</p><h1 id="teacher-board-title">Ploča učitelja</h1><span>Vodite sat i zadržite sigurnu verzioniranu runtime povijest.</span></div>
      <span className="teacher-board-live">SAT U TIJEKU</span>
      <div className="teacher-board-actions ds-page-actions--below-global-profile"><div><small>Vrijeme sata</small><strong>{elapsed}</strong></div><button disabled>Sažetak · Phase 10.4</button><button disabled>Završi sat · Phase 10.5</button></div>
    </header>
    <section className="teacher-board-context">
      <div><small>{view.participantType === 'student' ? 'Učenik' : 'Grupa'}</small><strong>{view.participantName}</strong></div>
      <div><small>Cilj sata</small><strong>{view.goal}</strong></div>
      <div><small>Ready snapshot</small><strong>Verzija {view.lessonPlanVersionNumber}</strong><span>{view.lessonPlanVersionId.slice(0, 8)}</span></div>
      <div><small>Planirano trajanje</small><strong>{view.activities.reduce((sum, item) => sum + item.durationMinutes, 0)} min</strong></div>
    </section>
    {error && <div className="board-inline-error" role="alert">{error}</div>}
    <div className="teacher-board-layout">
      <aside className="teacher-board-timeline"><div><h2>Plan sata</h2><span>{view.activities.length} aktivnosti</span></div><ol>{view.activities.map((activity, index) => <li key={activity.id}>
        <span>{index + 1}</span><div><strong>{activity.name}</strong><small>{activity.durationMinutes} min · {purpose(activity.purpose)}</small><em>{status(activity.status)}</em></div>
        <div className="board-order-actions"><button aria-label={`Pomakni ${activity.name} gore`} disabled={index === 0 || saveState === 'saving'} onClick={() => void move(activity.id, -1)}>↑</button><button aria-label={`Pomakni ${activity.name} dolje`} disabled={index === view.activities.length - 1 || saveState === 'saving'} onClick={() => void move(activity.id, 1)}>↓</button></div>
      </li>)}</ol><button disabled>+ Dodaj aktivnost · Phase 10.2</button></aside>
      <main className="teacher-board-workspace"><p>AKTIVNOST 1 / {view.activities.length}</p><h2>{view.activities[0]?.name}</h2><span>{view.activities[0]?.description ?? 'Aktivnost je spremna za izvođenje.'}</span><div><strong>Runtime aktivnosti dolazi u Phase 10.2</strong><p>Ovaj ekran sada pouzdano čuva exact Ready snapshot, timeline i recovery stanje bez stvaranja Attempta ili Evidencea.</p></div><button disabled>Pokreni aktivnost · Phase 10.2</button></main>
      <aside className="teacher-board-participants"><h2>{view.participantType === 'student' ? 'Učenik' : 'Grupa'}</h2><div><span className="teacher-board-avatar">{initials(view.participantName)}</span><strong>{view.participantName}</strong><small>Runtime rezultat nije dostupan prije Phase 10.2</small></div><section><h3>Spremanje</h3><strong className={`board-save board-save--${saveState}`}>{saveLabel(saveState, view.lastCommittedAtUtc)}</strong><small>Revizija {view.revision}</small></section></aside>
    </div>
  </section>
}

function purpose(value: string) { return ({ Engagement: 'Uključivanje', ActivatePriorKnowledge: 'Aktivacija predznanja', Instruction: 'Poučavanje', Practice: 'Vježba', CheckForUnderstanding: 'Provjera razumijevanja', Other: 'Ostalo' } as Record<string, string>)[value] ?? value }
function status(value: string) { return ({ Pending: 'Čeka', InProgress: 'U tijeku', Paused: 'Pauzirano', Completed: 'Završeno', Partial: 'Djelomično', Skipped: 'Preskočeno' } as Record<string, string>)[value] ?? value }
function initials(value: string) { return value.split(/\s+/).map(part => part[0]).join('').slice(0, 2).toLocaleUpperCase('hr') }
function formatElapsed(milliseconds: number) { const seconds = Math.max(0, Math.floor(milliseconds / 1_000)); const hours = Math.floor(seconds / 3_600); const minutes = Math.floor(seconds % 3_600 / 60); return [hours, minutes, seconds % 60].map(value => String(value).padStart(2, '0')).join(':') }
function saveLabel(state: SaveState, value: string) { if (state === 'saving') return 'Spremanje…'; if (state === 'offline') return 'Offline — nije spremljeno'; if (state === 'conflict') return 'Konflikt — pregledajte promjene'; return `Spremljeno u ${new Intl.DateTimeFormat('hr-HR', { timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value))}` }
