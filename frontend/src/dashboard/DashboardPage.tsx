import {
  ArrowRight,
  BookOpenCheck,
  CalendarDays,
  ClipboardCheck,
  FileCheck2,
  FolderOpen,
  MapPin,
  MessageCircle,
  UserRoundPlus,
  UsersRound,
  Video,
  WalletCards,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { useDashboard, type DashboardData, type DashboardSession } from './dashboardApi.ts'
import './DashboardPage.css'

const sessionStatus: Record<DashboardSession['status'], string> = {
  1: 'Zakazano',
  2: 'U tijeku',
  3: 'Održano',
}

export function DashboardPage() {
  const [revision, setRevision] = useState(0)
  const state = useDashboard(revision)

  return (
    <div className="dashboard-page">
      <header className="dashboard-heading">
        <h1>Dobro došli! <span aria-hidden="true">👋</span></h1>
        <p>Ovdje je pregled vaših današnjih aktivnosti.</p>
      </header>

      {!state && <DashboardState message="Učitavanje Radnog stola…" />}
      {state?.error && (
        <DashboardState
          error
          message="Radni stol trenutačno nije moguće učitati."
          action={() => setRevision((value) => value + 1)}
        />
      )}
      {state?.data && <DashboardContent data={state.data} />}
    </div>
  )
}

function DashboardContent({ data }: { readonly data: DashboardData }) {
  return (
    <>
      <section className="dashboard-quick-actions" aria-label="Brze akcije">
        <QuickAction icon={<UserRoundPlus />} label="Dodaj učenika" detail="Novi učenik" to="/students/new" />
        <QuickAction icon={<MessageCircle />} label="Odgovori roditelju" detail="Poruke dolaze u kasnijoj fazi" />
        <QuickAction icon={<FileCheck2 />} label="Pošalji domaću" detail="Domaće zadaće još nisu dostupne" />
        <QuickAction icon={<ClipboardCheck />} label="Kreiraj test/ispit" detail="Priprema sata još nije dostupna" />
        <QuickAction icon={<FolderOpen />} label="Materijali" detail="Biblioteka materijala" to="/materials" />
        <QuickAction icon={<WalletCards />} label="Financije" detail="Financije dolaze u kasnijoj fazi" />
      </section>

      <div className="dashboard-primary-grid">
        <TodaySchedule sessions={data.todaySessions} today={data.today} timeZone={data.timeZoneId} />
        <NextSession session={data.nextSession} timeZone={data.timeZoneId} today={data.today} />
      </div>

      <StudentOverview students={data.students} />
    </>
  )
}

function QuickAction({ icon, label, detail, to }: {
  readonly icon: ReactNode
  readonly label: string
  readonly detail: string
  readonly to?: string
}) {
  const content = <><span className="dashboard-action-icon" aria-hidden="true">{icon}</span><span><strong>{label}</strong><small>{detail}</small></span></>
  return to
    ? <Link className="dashboard-quick-action" to={to}>{content}</Link>
    : <button className="dashboard-quick-action dashboard-quick-action--disabled" type="button" disabled title={detail}>{content}</button>
}

function TodaySchedule({ sessions, today, timeZone }: {
  readonly sessions: DashboardSession[]
  readonly today: string
  readonly timeZone: string
}) {
  return (
    <section className="dashboard-card dashboard-schedule" aria-labelledby="dashboard-schedule-title">
      <header className="dashboard-card-heading">
        <span className="dashboard-card-icon" aria-hidden="true"><CalendarDays /></span>
        <div><h2 id="dashboard-schedule-title">Današnji raspored</h2><p>{formatDate(today)}</p></div>
        <Link to={`/schedule?date=${today}`}>Pogledaj cijeli raspored <ArrowRight aria-hidden="true" /></Link>
      </header>

      {sessions.length === 0 ? (
        <div className="dashboard-empty"><strong>Danas nema zakazanih termina</strong><p>Cijeli raspored možete otvoriti iz poveznice iznad.</p></div>
      ) : (
        <ol className="dashboard-session-list">
          {sessions.map((session) => <DashboardSessionRow key={session.id} session={session} timeZone={timeZone} />)}
        </ol>
      )}
    </section>
  )
}

function DashboardSessionRow({ session, timeZone }: {
  readonly session: DashboardSession
  readonly timeZone: string
}) {
  const contextRoute = session.studentId
    ? `/students/${session.studentId}`
    : `/students/groups?group=${session.groupId}`

  return (
    <li className={session.status === 2 ? 'dashboard-session dashboard-session--current' : 'dashboard-session'}>
      <time dateTime={session.startsAtUtc}><strong>{formatTime(session.startsAtUtc, timeZone)}</strong><span>{formatTime(session.endsAtUtc, timeZone)}</span></time>
      <span className="dashboard-avatar" aria-hidden="true">{initials(session.contextName)}</span>
      <span className="dashboard-session-person"><Link to={contextRoute}>{session.contextName}</Link><small>{session.schoolGradeName}</small></span>
      <span className="dashboard-session-program"><b>{session.deliveryMode === 2 ? `Grupa (${session.participantCount})` : 'Individualno'}</b><small>{session.programName ?? 'Program nije postavljen'}</small></span>
      <span className={`dashboard-session-status dashboard-session-status--${session.status}`}>{sessionStatus[session.status]}</span>
      <Link className="dashboard-session-detail" to={`/schedule/${session.id}`} aria-label={`Otvori termin ${session.contextName}`}><ArrowRight aria-hidden="true" /></Link>
    </li>
  )
}

function NextSession({ session, timeZone, today }: {
  readonly session: DashboardSession | null
  readonly timeZone: string
  readonly today: string
}) {
  return (
    <section className="dashboard-card dashboard-next" aria-labelledby="dashboard-next-title">
      <header className="dashboard-card-heading dashboard-card-heading--compact">
        <span className="dashboard-card-icon" aria-hidden="true"><BookOpenCheck /></span>
        <div><h2 id="dashboard-next-title">Sljedeći sat</h2>{session && <p>{session.contextName} · {relativeDate(session.startsAtUtc, timeZone, today)} u {formatTime(session.startsAtUtc, timeZone)}</p>}</div>
      </header>

      {session ? (
        <div className="dashboard-next-content">
          <span className="dashboard-next-visual" aria-hidden="true"><BookOpenCheck /></span>
          <div className="dashboard-next-copy">
            <span className="dashboard-next-status">{sessionStatus[session.status]}</span>
            <h3>{session.title ?? session.programName ?? 'Termin bez upisane teme'}</h3>
            <p>{session.deliveryMode === 2 ? `${session.contextName} · ${session.participantCount} učenika` : session.contextName}</p>
            <p className="dashboard-next-location">{session.online ? <Video aria-hidden="true" /> : <MapPin aria-hidden="true" />}{session.online ? 'Online' : session.locationName ?? 'Lokacija nije postavljena'}</p>
            <small>Plan i aktivnosti bit će dostupni nakon implementacije Pripreme sata.</small>
          </div>
        </div>
      ) : (
        <div className="dashboard-empty"><strong>Nema budućeg zakazanog termina</strong><p>Novi termin možete dodati kroz Raspored.</p></div>
      )}

      <footer className="dashboard-next-actions">
        {session ? <Link to={`/schedule/${session.id}`}>Otvori termin</Link> : <Link to="/schedule">Otvori raspored</Link>}
        <button type="button" disabled title="Plan sata dolazi u Phase 9.">Otvori plan sata</button>
        <button type="button" disabled title="Pokretanje sata dolazi u Phase 10.">Pokreni sat</button>
      </footer>
    </section>
  )
}

function StudentOverview({ students }: { readonly students: { readonly totalCount: number; readonly activeCount: number; readonly onHoldCount: number; readonly inactiveCount: number } }) {
  return (
    <section className="dashboard-card dashboard-students" aria-labelledby="dashboard-students-title">
      <header className="dashboard-card-heading">
        <span className="dashboard-card-icon" aria-hidden="true"><UsersRound /></span>
        <div><h2 id="dashboard-students-title">Učenici — pregled</h2><p>Ukupno: {students.totalCount}</p></div>
        <Link to="/students">Pogledaj sve učenike <ArrowRight aria-hidden="true" /></Link>
      </header>
      <div className="dashboard-student-metrics">
        <Metric label="Aktivni" value={students.activeCount} total={students.totalCount} tone="active" />
        <Metric label="Na čekanju" value={students.onHoldCount} total={students.totalCount} tone="hold" />
        <Metric label="Neaktivni" value={students.inactiveCount} total={students.totalCount} tone="inactive" />
      </div>
      <p className="dashboard-students-note">Statusi su administrativni i ne predstavljaju procjenu znanja ili spremnosti učenika.</p>
    </section>
  )
}

function Metric({ label, value, total, tone }: {
  readonly label: string
  readonly value: number
  readonly total: number
  readonly tone: string
}) {
  return <div className={`dashboard-metric dashboard-metric--${tone}`}><span>{label}</span><progress max={Math.max(total, 1)} value={value}>{value}</progress><strong>{value}</strong></div>
}

function DashboardState({ message, error = false, action }: {
  readonly message: string
  readonly error?: boolean
  readonly action?: () => void
}) {
  return <div className={`dashboard-state${error ? ' dashboard-state--error' : ''}`} role={error ? 'alert' : 'status'}><p>{message}</p>{action && <button type="button" onClick={action}>Pokušaj ponovno</button>}</div>
}

function formatDate(value: string) {
  const [year, month, day] = value.split('-').map(Number)
  return new Intl.DateTimeFormat('hr-HR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date(Date.UTC(year, month - 1, day)))
}

function formatTime(value: string, timeZone: string) {
  return new Intl.DateTimeFormat('hr-HR', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone }).format(new Date(value))
}

function relativeDate(value: string, timeZone: string, today: string) {
  const date = new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone }).format(new Date(value))
  return date === today ? 'danas' : new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'short', timeZone }).format(new Date(value))
}

function initials(value: string) {
  return value.split(/\s+/).filter(Boolean).map((part) => part[0]).join('').slice(0, 2).toLocaleUpperCase('hr')
}
