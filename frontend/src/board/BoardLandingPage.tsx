import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { getActiveLessonSessions, type ActiveLessonSession } from './boardApi.ts'
import './TeacherBoardPage.css'

export function BoardLandingPage() {
  const [sessions, setSessions] = useState<ActiveLessonSession[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    getActiveLessonSessions(controller.signal)
      .then(setSessions)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(true)
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [])

  return <section className="board-landing" aria-labelledby="board-landing-title">
    <header>
      <p>PLUS 5 Ploča</p>
      <h1 id="board-landing-title">Aktivni satovi</h1>
      <span>Odaberite sat koji želite nastaviti. Otvaranje ne mijenja runtime stanje.</span>
    </header>
    {loading && <div className="board-state">Učitavanje aktivnih sati…</div>}
    {error && <div className="board-state board-state--error" role="alert">Aktivne satove trenutačno nije moguće učitati.</div>}
    {!loading && !error && sessions.length === 0 && <div className="board-state">
      <h2>Nema aktivnih sati</h2><p>Pokrenite potvrđenu pripremu kada ste spremni za izvođenje.</p>
      <Link to="/lesson-plans">Otvori pripremu sata</Link>
    </div>}
    {!loading && !error && sessions.length > 0 && <div className="board-session-grid">
      {sessions.map(session => <article key={session.id}>
        <div><span className="board-live-dot" /> Sat u tijeku</div>
        <h2>{session.participantName}</h2>
        <p>{session.goal}</p>
        <dl><div><dt>Pokrenut</dt><dd>{formatDate(session.startedAtUtc)}</dd></div><div><dt>Aktivnosti</dt><dd>{session.activityCount}</dd></div><div><dt>Revizija</dt><dd>{session.revision}</dd></div></dl>
        <Link to={`/board/sessions/${session.id}`}>Nastavi sat</Link>
      </article>)}
    </div>}
  </section>
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value))
}
