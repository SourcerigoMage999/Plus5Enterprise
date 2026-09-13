import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { sessionLocal } from './calendarDate.ts'
import { useScheduleSessionDetail, type ScheduleSessionDetail } from './scheduleApi.ts'
import './ScheduleSessionDetailPage.css'

const statusLabels = { 1: 'Zakazan', 2: 'U tijeku', 3: 'Održan', 4: 'Otkazan' } as const
const studentStatusLabels = { 1: 'Aktivan', 2: 'Na čekanju', 3: 'Neaktivan' } as const

export function ScheduleSessionDetailPage() {
  const { sessionId = '' } = useParams()
  const [revision, setRevision] = useState(0)
  const state = useScheduleSessionDetail(sessionId, revision)

  if (!state) return <DetailState message="Učitavanje detalja termina…" />
  if (state.error) return <DetailState error message={state.status === 404 ? 'Termin nije pronađen ili mu nemate pristup.' : 'Detalj termina trenutno nije moguće učitati.'} action={() => setRevision(value => value + 1)} />
  if (!state.data) return null
  return <DetailContent session={state.data} />
}

function DetailContent({ session }: { readonly session: ScheduleSessionDetail }) {
  const start = sessionLocal(session.startsAtUtc, session.timeZoneId)
  const end = sessionLocal(session.endsAtUtc, session.timeZoneId)
  const localDate = new Intl.DateTimeFormat('hr-HR', { timeZone: session.timeZoneId, weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(session.startsAtUtc))
  const duration = Math.round((new Date(session.endsAtUtc).getTime() - new Date(session.startsAtUtc).getTime()) / 60000)
  const contextLink = session.groupId ? `/students/groups?group=${session.groupId}` : `/students/${session.studentId}`
  return <section className="session-detail-page">
    <nav className="session-detail-breadcrumb" aria-label="Putanja"><Link to={`/schedule?date=${start.date}`}>Raspored</Link><span>›</span><Link to={contextLink}>{session.contextName}</Link><span>›</span><strong>{dateShort(session.startsAtUtc, session.timeZoneId)}</strong></nav>
    <header className="session-detail-header">
      <div><h1>3.2 Detalj termina</h1><p>Pregled i upravljanje detaljima ovog termina.</p></div>
      <div className="session-detail-header-actions">{session.status === 1 ? <Link to={`/schedule/${session.id}/edit`}>✎ Uredi termin</Link> : <button disabled title="Mijenjati se može samo zakazani termin.">✎ Uredi termin</button>}<button className="session-detail-primary" disabled title="Pokretanje sata ovisi o pripremi sata i delivery modelu.">▷ Pokreni sat</button></div>
    </header>

    <section className="session-identity ds-card" aria-label="Sažetak termina">
      <span className="session-context-avatar" aria-hidden="true">{initials(session.contextName)}</span>
      <div className="session-context-copy"><h2>{session.contextName}</h2><span className={`session-kind session-kind--${session.deliveryMode === 2 ? 'group' : 'individual'}`}>{session.deliveryMode === 2 ? 'Grupni sat' : 'Individualni sat'}</span>{session.title && <p>{session.title}</p>}</div>
      <dl className="session-facts">
        <Fact icon="▣" label={capitalize(localDate)} />
        <Fact icon="◷" label={`${start.time} – ${end.time} (${duration} min)`} />
        <Fact icon="⌖" label={session.locationName ?? (session.online ? 'Online' : 'Lokacija nije postavljena')} />
        <Fact icon="▤" label={`Program: ${session.programName ?? 'Nije postavljen'}`} />
      </dl>
      <div className="session-status"><small>Status termina</small><strong className={`session-status-badge session-status-badge--${session.status}`}>{statusLabels[session.status]}</strong></div>
    </section>

    <div className="session-detail-columns">
      <section className="session-card session-students" aria-labelledby="session-students-title">
        <h2 id="session-students-title">Učenici <span>({session.participants.length})</span></h2>
        {session.participants.length ? <div className="session-student-table" role="region" aria-label="Učenici na terminu — vodoravno pomična tablica" tabIndex={0}><table><thead><tr><th>Učenik</th><th>Razred</th><th>Status dolaska</th><th aria-label="Dosje" /></tr></thead><tbody>{session.participants.map(student => <tr key={student.id}>
          <td><Link to={`/students/${student.id}`}><span className="session-student-avatar" aria-hidden="true">{initials(`${student.firstName} ${student.lastName}`)}</span><span><strong>{student.firstName} {student.lastName}</strong><small>{studentStatusLabels[student.status]}</small></span></Link></td>
          <td>{student.schoolGrade}</td><td><button disabled title="Evidencija dolaska pripada delivery/evidence fazi.">Nije dostupno⌄</button></td><td><Link aria-label={`Otvori dosje: ${student.firstName} ${student.lastName}`} to={`/students/${student.id}`}>•••</Link></td>
        </tr>)}</tbody></table></div> : <div className="session-empty"><strong>Nema učenika za ovaj termin</strong><p>Prikaz koristi članstva koja vrijede u vrijeme početka termina.</p></div>}
        <button className="session-outline-action" disabled title="Promjena sastava konkretnog termina nema zaključan write contract.">＋ Dodaj učenika u termin</button>
        <p className="session-notice">ⓘ Označavanje dolaska bit će dostupno nakon početka ili završetka sata, u delivery/evidence fazi.</p>
      </section>

      <div className="session-middle-column">
        <section className="session-card"><h2>◎ Tema i cilj sata</h2><div className="session-empty session-empty--compact"><strong>Plan sata još nije pripremljen</strong><p>Tema, cilj i komponente znanja dolaze iz pripreme sata u Phase 9.</p><button disabled>Pripremi sat</button></div></section>
        <section className="session-card"><h2>▱ Materijali za ovaj sat</h2><div className="session-empty session-empty--compact"><strong>Nema povezanih materijala</strong><p>Materijali se povezuju kroz plan sata nakon definiranja Knowledge Modela.</p></div><button className="session-outline-action" disabled>＋ Dodaj materijal</button></section>
      </div>

      <div className="session-right-column">
        <section className="session-card"><h2>⌂ Domaća zadaća</h2><div className="session-empty session-empty--compact"><strong>Nema domaće zadaće</strong><p>Homework workflow dolazi u Phase 12.</p></div><button className="session-outline-action" disabled>＋ Dodaj domaću zadaću</button></section>
        <section className="session-card session-notes"><h2>▱ Napomene učitelja</h2><div>{session.notes ?? 'Nema napomene za ovaj termin.'}</div><small>{session.notes?.length ?? 0} / 2000</small></section>
        <section className="session-card session-actions"><h2>ϟ Akcije termina</h2>{session.status === 1 ? <Link className="session-action-danger" to={`/schedule/${session.id}/edit`}>⊘ Otkaži termin</Link> : <button disabled>⊘ Otkaži termin</button>}<Link to={`/schedule/new?duplicate=${session.id}`}>▣ Dupliciraj termin</Link><button disabled title="Slanje ovisi o budućem notification contractu.">➤ Pošalji podsjetnik učenicima</button></section>
      </div>
    </div>

    <section className="session-card session-related-history">
      <div><h2>⌘ Povezano</h2><dl><dt>{session.groupId ? 'Grupa' : 'Učenik'}</dt><dd><Link to={contextLink}>{session.contextName}</Link></dd><dt>Program</dt><dd>{session.programName ?? 'Nije postavljen'}</dd><dt>Razred</dt><dd>{session.schoolGrade}</dd><dt>Lokacija</dt><dd>{session.locationName ?? (session.online ? 'Online' : 'Nije postavljena')}</dd></dl></div>
      <div><h2>◷ Povijest termina</h2><ul><History title={session.isSeriesOccurrence ? 'Termin je kreiran iz rasporeda.' : 'Termin je kreiran.'} date={session.createdAtUtc} zone={session.timeZoneId} />{session.isSeriesException && <History title="Termin je izdvojen iz redovitog rasporeda." date={session.updatedAtUtc} zone={session.timeZoneId} />}{session.cancelledAtUtc && <History title="Termin je otkazan." date={session.cancelledAtUtc} zone={session.timeZoneId} />}</ul></div>
    </section>
    <footer className="session-detail-footer"><span>ⓘ Session je planirani kalendarski termin; prisutnost, provedene aktivnosti i rezultati pripadaju zasebnom delivery/evidence modelu.</span><span>Vrijeme prikaza: {session.timeZoneId}</span></footer>
  </section>
}

function Fact({ icon, label }: { readonly icon: string; readonly label: string }) { return <div><dt aria-hidden="true">{icon}</dt><dd>{label}</dd></div> }
function History({ title, date, zone }: { readonly title: string; readonly date: string; readonly zone: string }) { return <li><i aria-hidden="true" /><div><strong>{title}</strong><small>{new Intl.DateTimeFormat('hr-HR', { timeZone: zone, dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date))}</small></div></li> }
function DetailState({ message, error = false, action }: { readonly message: string; readonly error?: boolean; readonly action?: () => void }) { return <div className={`session-detail-state${error ? ' session-detail-state--error' : ''}`} role={error ? 'alert' : 'status'}><p>{message}</p>{action && <button onClick={action}>Pokušaj ponovno</button>}<Link to="/schedule">Povratak na raspored</Link></div> }
function initials(name: string) { return name.split(/\s+/).filter(Boolean).map(part => part[0]).join('').slice(0, 3).toUpperCase() }
function capitalize(value: string) { return value.charAt(0).toUpperCase() + value.slice(1) }
function dateShort(value: string, zone: string) { return new Intl.DateTimeFormat('hr-HR', { timeZone: zone, day: 'numeric', month: 'numeric', year: 'numeric' }).format(new Date(value)) }
