import { useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { getStudentDossier, type StudentDossier, type StudentDossierSession, type StudentStatus } from './studentsApi.ts'
import './StudentDossierPage.css'

const learningAreas = ['Gramatika', 'Vokabular', 'Čitanje', 'Slušanje', 'Govor', 'Pisanje']

export function StudentDossierPage() {
  const { studentId = '' } = useParams()
  const [dossier, setDossier] = useState<StudentDossier | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<{ message: string; notFound: boolean } | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getStudentDossier(studentId, controller.signal)
      .then(setDossier)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError({
          message: requestError instanceof ApiError ? requestError.message : 'Dosje učenika trenutačno nije moguće učitati.',
          notFound: requestError instanceof ApiError && requestError.status === 404,
        })
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [studentId, version])

  if (loading) return <DossierState title="Učitavanje dosjea" message="Pripremamo podatke učenika…" />
  if (error) return (
    <DossierState
      title={error.notFound ? 'Učenik nije pronađen' : 'Dosje nije dostupan'}
      message={error.notFound ? 'Učenik ne postoji, arhiviran je ili nije dio vašeg računa.' : error.message}
    >
      {!error.notFound && <button onClick={() => { setLoading(true); setError(null); setVersion((value) => value + 1) }} type="button">Pokušaj ponovno</button>}
      <Link to="/students">Povratak na popis</Link>
    </DossierState>
  )
  if (!dossier) return null

  const name = `${dossier.firstName} ${dossier.lastName}`
  const initials = `${dossier.firstName[0] ?? ''}${dossier.lastName[0] ?? ''}`.toLocaleUpperCase('hr')
  const guardian = dossier.primaryGuardian

  return (
    <section className="dossier-page" aria-labelledby="dossier-title">
      <nav className="dossier-breadcrumb" aria-label="Putanja"><Link to="/students">Učenici</Link><span>›</span><span>{name}</span></nav>
      <header className="dossier-hero">
        <div><span className="dossier-avatar" aria-hidden="true">{initials}</span><div><div className="dossier-title-row"><h1 id="dossier-title">{name}</h1><StatusBadge status={dossier.status} /></div><p>Digitalni dosje učenika i pregled ključnih informacija.</p></div></div>
        <div className="dossier-actions" aria-label="Akcije dosjea">
          <button disabled title="Komunikacija s roditeljima dolazi u kasnijoj fazi" type="button">✉ Poruka roditelju</button>
          <button disabled title="Zakazivanje termina dolazi u Phase 5" type="button">▣ Zakaži termin</button>
          <Link to={`/students/${studentId}/edit`}>✎ Uredi učenika</Link>
        </div>
      </header>

      <div className="dossier-top-grid">
        <Card title="Profil učenika" className="dossier-profile">
          <div className="dossier-profile-grid">
            <Detail label="Datum rođenja" value={formatDate(dossier.dateOfBirth)} />
            <Detail label="Spol" value={dossier.gender ?? 'Nije navedeno'} />
            <Detail label="E-pošta" value={dossier.email ?? 'Nije navedena'} />
            <Detail label="Telefon" value={dossier.phone ?? 'Nije naveden'} />
            <Detail label="Škola" value={dossier.schoolName ?? 'Nije navedena'} />
            <Detail label="Razred" value={dossier.schoolGrade.code ?? dossier.schoolGrade.name} />
            <Detail label="Program" value={dossier.program?.name ?? 'Nije dodijeljen'} />
            <Detail label="Način rada" value={deliveryLabel(dossier.deliveryMode)} />
            <Detail label="Grupa" value={dossier.group?.name ?? 'Nije dodijeljena'} />
          </div>
        </Card>
        <Card title="Spremnost učenika" className="dossier-readiness">
          <BoundaryEmpty icon="◎" title="Procjena još nije dostupna">Spremnost će se prikazati nakon uvođenja pedagoških procjena i kriterija.</BoundaryEmpty>
        </Card>
      </div>

      <div className="dossier-main-grid">
        <Card title="Plan rada">
          <h3 className="dossier-section-label">Sljedeći termin</h3>
          {dossier.nextSession ? <Session session={dossier.nextSession} /> : <BoundaryEmpty icon="▣" title="Nema zakazanog termina">Novi termini bit će dostupni u modulu rasporeda.</BoundaryEmpty>}
        </Card>
        <Card title="Napredak po područjima">
          <div className="dossier-progress-list">{learningAreas.map((area) => <div key={area}><span>{area}</span><i aria-hidden="true" /><small>Nije procijenjeno</small></div>)}</div>
        </Card>
        <Card title="Korišteni materijali">
          <BoundaryEmpty icon="▤" title="Nema evidentiranih materijala">Materijali će se prikazati nakon aktivacije sadržaja i evidencije rada.</BoundaryEmpty>
        </Card>
        <Card title="Zadnji održani sat">
          {dossier.lastHeldSession ? <Session session={dossier.lastHeldSession} /> : <BoundaryEmpty icon="◷" title="Nema održanih sati">Ovdje će biti prikazan posljednji završeni termin.</BoundaryEmpty>}
        </Card>
      </div>

      <div className="dossier-bottom-grid">
        <Card title="Nedavne aktivnosti"><BoundaryEmpty icon="↗" title="Aktivnosti još nisu dostupne">Povijest aktivnosti zahtijeva zaključan model evidencije i audit događaja.</BoundaryEmpty></Card>
        <Card title="Komunikacija s roditeljem">
          {guardian ? <div className="dossier-guardian"><span aria-hidden="true">{guardian.firstName[0]}{guardian.lastName[0]}</span><div><strong>{guardian.firstName} {guardian.lastName}</strong><small>Primarni kontakt{guardian.relationship ? ` · ${guardian.relationship}` : ''}</small><p>{guardian.email ?? 'E-pošta nije navedena'}<br />{guardian.phone ?? 'Telefon nije naveden'}</p></div></div> : <BoundaryEmpty icon="♙" title="Primarni kontakt nije dodan">Kontakt možete dodati kroz akciju Uredi učenika.</BoundaryEmpty>}
          <p className="dossier-boundary-note">Povijest poruka dolazi u fazi komunikacije.</p>
        </Card>
        <Card title="Bilješke nastavnika"><BoundaryEmpty icon="✎" title="Bilješke još nisu dostupne">Bilješke se neće spremati dok model privatnosti i ovlasti ne bude zaključan.</BoundaryEmpty></Card>
      </div>
      <p className="dossier-guidance"><span aria-hidden="true">ⓘ</span> Prikazani su samo stvarno spremljeni administrativni podaci. Neutralna stanja označavaju funkcije budućih faza.</p>
    </section>
  )
}

function Card({ title, className = '', children }: { readonly title: string; readonly className?: string; readonly children: ReactNode }) {
  return <section className={`dossier-card ${className}`}><header><h2>{title}</h2><span aria-hidden="true">•••</span></header>{children}</section>
}

function Detail({ label, value }: { readonly label: string; readonly value: string }) { return <div className="dossier-detail"><span>{label}</span><strong>{value}</strong></div> }

function BoundaryEmpty({ icon, title, children }: { readonly icon: string; readonly title: string; readonly children: ReactNode }) {
  return <div className="dossier-empty"><span aria-hidden="true">{icon}</span><strong>{title}</strong><p>{children}</p></div>
}

function Session({ session }: { readonly session: StudentDossierSession }) {
  return <article className="dossier-session"><span className="dossier-session__date">{formatSessionDay(session.startsAtUtc)}<b>{new Date(session.startsAtUtc).getDate()}</b></span><div><strong>{session.title ?? 'Termin nastave'}</strong><p>{formatSessionTime(session.startsAtUtc, session.endsAtUtc)}</p><small>{session.group?.name ?? deliveryLabel(session.deliveryMode)}</small></div></article>
}

function StatusBadge({ status }: { readonly status: StudentStatus }) { return <span className={`dossier-status dossier-status--${status}`}>{status === 'active' ? 'Aktivan' : status === 'on_hold' ? 'Na čekanju' : 'Neaktivan'}</span> }

function DossierState({ title, message, children }: { readonly title: string; readonly message: string; readonly children?: ReactNode }) { return <section className="dossier-state"><span aria-hidden="true">5</span><h1>{title}</h1><p>{message}</p><div>{children}</div></section> }

function deliveryLabel(value: StudentDossier['deliveryMode']) { return value === 'individual' ? 'Individualno' : value === 'group' ? 'Grupno' : 'Nije odabrano' }
function formatDate(value: string | null) { if (!value) return 'Nije naveden'; const [year, month, day] = value.split('-'); return `${day}. ${month}. ${year}.` }
function formatSessionDay(value: string) { return new Intl.DateTimeFormat('hr-HR', { weekday: 'short', month: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
function formatSessionTime(start: string, end: string) { const formatter = new Intl.DateTimeFormat('hr-HR', { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Zagreb' }); return `${formatter.format(new Date(start))} – ${formatter.format(new Date(end))}` }
