import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { changeMembership, useGroupResource } from './groupsApi.ts'
import type { Group, GroupSession, GroupStudent, Overview, Page, Slot } from './groupsApi.ts'
import type { StudentListOverview } from '../students/studentsApi.ts'
import './GroupListPage.css'

const statuses = { active: 'Aktivna', on_hold: 'Na čekanju', inactive: 'Neaktivna' }
const days = ['Ned', 'Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub']
const pageNumber = (value: string | null) => value && /^\d+$/.test(value) && Number(value) > 0 && Number(value) <= 2147483647 ? Number(value) : 1
const shortTime = (value: string) => value.slice(0, 5)
const slotLabel = (slot: Slot) => `${days[slot.dayOfWeek]} ${shortTime(slot.start)}–${shortTime(slot.end)}`

export function GroupListPage() {
  const [params, setParams] = useSearchParams()
  const [revision, setRevision] = useState(0)
  const page = pageNumber(params.get('page'))
  const search = (params.get('search') ?? '').slice(0, 100)
  const program = params.get('programId') ?? ''
  const status = ['1', '2', '3'].includes(params.get('status') ?? '') ? params.get('status')! : ''
  const query = new URLSearchParams({ page: String(page), pageSize: '8' })
  if (search) query.set('search', search)
  if (program) query.set('programId', program)
  if (status) query.set('status', status)
  const groups = useGroupResource<Page<Group>>(`/groups?${query}`, revision)
  const overview = useGroupResource<Overview>('/groups/overview', revision)
  const options = useGroupResource<StudentListOverview>('/students/overview', revision)
  const selected = params.get('group') ?? groups?.data?.items[0]?.id
  function update(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value); else next.delete(key)
    if (key !== 'group') next.delete('group')
    if (key !== 'page' && key !== 'group') next.delete('page')
    setParams(next)
  }
  const reload = () => setRevision((value) => value + 1)
  return (
    <section className="groups-page">
      <nav className="groups-breadcrumb" aria-label="Putanja"><Link to="/students">Učenici</Link><span aria-hidden="true">›</span><span>Grupe</span></nav>
      <header className="groups-hero">
        <div><h1>2.7 Grupe</h1><p>Pregledaj i upravljaj grupama i učenicima po grupama.</p></div>
        <div className="groups-hero-actions">
          <button disabled title="Izvoz je planiran u fazi izvještaja.">Izvezi izvještaj (PDF)</button>
          <button className="groups-primary" disabled title="Nova grupa dolazi u Phase 3.6.">+ Nova grupa</button>
          <small>Nova grupa: sljedeća faza · Izvoz: faza izvještaja</small>
        </div>
      </header>
      {overview?.data ? <div className="groups-stats">
        <Stat icon="groups" label="Ukupno grupa" value={overview.data.totalGroups} note={`Aktivnih: ${overview.data.activeGroups}`} />
        <Stat icon="students" label="Ukupno učenika" value={overview.data.students} note={`Prosječno po grupi: ${overview.data.totalGroups ? (overview.data.students / overview.data.totalGroups).toLocaleString('hr-HR', { maximumFractionDigits: 1 }) : '0'}`} />
        <Stat icon="clock" label="Termini ovaj tjedan" value={overview.data.sessionsThisWeek} note={`Tjedan od ${new Date(`${overview.data.weekStartsOn}T12:00:00`).toLocaleDateString('hr-HR')}`} />
        <Stat icon="seats" label="Slobodna mjesta" value={overview.data.availableSeats} note="U aktivnim grupama" />
      </div> : <Message error={overview?.error} retry={reload} loading="Učitavanje pregleda…" />}
      <div className="groups-columns">
        <section className="groups-panel groups-master" aria-labelledby="group-list-title">
          <h2 id="group-list-title">Popis grupa</h2>
          <div className="groups-filters">
            <label className="groups-search"><span className="sr-only">Pretraži grupe</span><input type="search" placeholder="Pretraži grupe..." value={search} maxLength={100} onChange={(event) => update('search', event.target.value)} /></label>
            <label><span className="sr-only">Program</span><select aria-label="Program" value={program} onChange={(event) => update('programId', event.target.value)}><option value="">Svi programi</option>{options?.data?.programs.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            <label><span className="sr-only">Status grupe</span><select aria-label="Status grupe" value={status} onChange={(event) => update('status', event.target.value)}><option value="">Svi statusi</option><option value="1">Aktivna</option><option value="2">Na čekanju</option><option value="3">Neaktivna</option></select></label>
          </div>
          {options?.error && <Message error="Popis programa nije učitan." retry={reload} />}
          {groups?.data ? <>
            <ul className="groups-rows" aria-label="Grupe">{groups.data.items.map((group) => <li key={group.id}>
              <button className={`groups-row${selected === group.id ? ' groups-row--selected' : ''}`} onClick={() => update('group', group.id)} aria-pressed={selected === group.id}>
                <span className="groups-avatar" aria-hidden="true">{initials(group.name)}</span>
                <span className="groups-row-copy"><span className="groups-row-title"><strong>{group.name}</strong><Status group={group} /></span><span>{group.programName} · Grupa</span><small>{group.slots.length ? group.slots.slice(0, 2).map(slotLabel).join(' · ') : 'Raspored nije postavljen'}</small></span>
                <span className="groups-row-count"><strong>{group.memberCount} učenika</strong><small>{Math.max(0, group.capacity - group.memberCount)} slobodnih</small></span><span aria-hidden="true">›</span>
              </button>
            </li>)}</ul>
            {groups.data.items.length === 0 && <div className="groups-empty"><h3>Nema grupa za odabrane filtre</h3><p>Promijenite pretragu ili filtre. Kreiranje grupa dolazi u sljedećoj fazi.</p></div>}
            <Pager page={groups.data} label="grupa" onPage={(value) => update('page', String(value))} />
          </> : <Message error={groups?.error} retry={reload} loading="Učitavanje grupa…" />}
        </section>
        {selected ? <GroupDetail key={selected} id={selected} revision={revision} reload={reload} /> : <section className="groups-panel groups-empty"><h2>Detalji grupe</h2><p>Odaberite grupu za prikaz detalja.</p></section>}
      </div>
      <aside className="groups-info">Grupe služe organizaciji nastave i povezivanju učenika. Procjene znanja ostaju individualne i ovdje se ne uređuju.</aside>
    </section>
  )
}

function GroupDetail({ id, revision, reload }: { id: string; revision: number; reload: () => void }) {
  const detail = useGroupResource<Group>(`/groups/${encodeURIComponent(id)}`, revision)
  const [tab, setTab] = useState('students')
  const group = detail?.data
  return <div className="groups-detail-column">
    <section className="groups-panel groups-detail" aria-label="Detalji grupe">
      <div className="groups-panel-heading"><h2>Detalji grupe</h2><button disabled title="Uređivanje grupe dolazi u Phase 3.7.">Uredi <span aria-hidden="true">↗</span></button></div>
      {group ? <>
        <header className="groups-detail-identity"><span className="groups-avatar groups-avatar--large" aria-hidden="true">{initials(group.name)}</span><div><h3>{group.name} <Status group={group} /></h3><p>{group.programName} · {group.schoolGrade}</p></div></header>
        <div className="groups-metadata">
          <div><Icon kind="students" /><strong>{group.memberCount} / {group.capacity} učenika</strong><small>Kapacitet grupe</small></div>
          <div><Icon kind="clock" /><strong>{group.slots.length ? `${group.slots.length}${group.slots.length === 14 ? '+' : ''} redovitih termina` : 'Bez rasporeda'}</strong><small>{group.slots.length ? group.slots.slice(0, 2).map(slotLabel).join(' · ') : 'Nema važećih tjednih pravila'}</small></div>
          <div><Icon kind="clock" /><strong>{duration(group.slots)}</strong><small>Trajanje termina</small></div>
          <div><Icon kind="seats" /><strong>{locations(group.slots)}</strong><small>Lokacija</small></div>
        </div>
        <div className="groups-tabs" role="tablist" aria-label="Sadržaj grupe">{[['students', 'Učenici'], ['schedule', 'Raspored'], ['materials', 'Materijali'], ['notes', 'Bilješke']].map(([key, title], index, tabs) => <button key={key} id={`group-tab-${key}`} role="tab" aria-selected={tab === key} aria-controls="group-tabpanel" tabIndex={tab === key ? 0 : -1} onClick={() => setTab(key)} onKeyDown={(event) => {
          const next = event.key === 'ArrowRight' ? (index + 1) % tabs.length : event.key === 'ArrowLeft' ? (index + tabs.length - 1) % tabs.length : event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : -1
          if (next >= 0) { event.preventDefault(); setTab(tabs[next][0]); document.getElementById(`group-tab-${tabs[next][0]}`)?.focus() }
        }}>{title}{key === 'students' ? ` (${group.memberCount})` : ''}</button>)}</div>
        <div role="tabpanel" id="group-tabpanel" aria-labelledby={`group-tab-${tab}`} tabIndex={0}>
          {tab === 'students' && <Members key={`${id}:${revision}`} group={group} reload={reload} />}
          {tab === 'schedule' && <Schedule group={group} revision={revision} />}
          {tab === 'materials' && <div className="groups-empty"><h3>Materijali još nisu dostupni</h3><p>Povezivanje s bibliotekom dolazi u fazi materijala.</p></div>}
          {tab === 'notes' && <div className="groups-empty"><h3>Bilješke još nisu dostupne</h3><p>Unos čeka definiranje pravila privatnosti i ovlasti. Bilješke neće mijenjati procjenu znanja učenika.</p></div>}
        </div>
      </> : <Message error={detail?.error} retry={reload} loading="Učitavanje detalja…" />}
    </section>
    <aside className="groups-panel groups-notes"><h2>Bilješke grupe <small>(opcionalno)</small></h2><p>Bilješke će biti dostupne nakon zaključavanja pravila privatnosti. Trenutačno nema unosa.</p></aside>
  </div>
}

function Members({ group, reload }: { group: Group; reload: () => void }) {
  const [page, setPage] = useState(1)
  const [adding, setAdding] = useState(false)
  const [search, setSearch] = useState('')
  const [retry, setRetry] = useState(0)
  const [confirmation, setConfirmation] = useState<GroupStudent>()
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const members = useGroupResource<Page<GroupStudent>>(`/groups/${group.id}/${adding ? 'candidates' : 'students'}?page=${page}&pageSize=8&search=${encodeURIComponent(search)}`, retry)
  async function save() {
    if (!confirmation || saving) return
    setSaving(true); setError('')
    try { await changeMembership(group, confirmation, adding); reload() }
    catch (error) { setError(error instanceof Error ? error.message : 'Promjena članstva nije spremljena.'); setSaving(false) }
  }
  const unavailable = group.memberCount >= group.capacity || group.status !== 'active'
  return <div className="groups-members">
    <h3>{adding ? 'Dodaj postojećeg učenika' : 'Učenici u grupi'}</h3>
    {adding && <><p>Prvo su ponuđeni učenici istog programa i razreda. Ostale možete odabrati sami. Prikazani su učenici bez aktivne grupe; premještaj je dostupan kroz uređivanje učenika.</p><label className="groups-candidate-search">Pretraži učenike<input type="search" maxLength={100} value={search} disabled={saving} onChange={(event) => { setSearch(event.target.value); setPage(1); setConfirmation(undefined) }} /></label></>}
    {members?.data ? <>
      <div className="groups-table-scroll" role="region" aria-label={adding ? 'Dostupni učenici — vodoravno pomična tablica' : 'Članovi grupe — vodoravno pomična tablica'} tabIndex={0}><table><thead><tr><th>Učenik</th><th>Razred</th>{adding ? <th>Podudaranje</th> : <><th>Razina (procjena)</th><th>Prisutnost</th></>}<th>Akcije</th></tr></thead><tbody>
        {members.data.items.map((student) => <tr key={student.id}><td><Link className="groups-student-link" to={`/students/${student.id}`}><span className="groups-student-avatar" aria-hidden="true">{initials(`${student.firstName} ${student.lastName}`)}</span>{student.firstName} {student.lastName}</Link></td><td>{student.schoolGrade}</td>{adding ? <td>{student.recommended ? 'Program i razred' : 'Drugi program / razred'}</td> : <><td><span title="Čeka Knowledge Model">Nije dostupno</span></td><td><span title="Čeka evidenciju prisutnosti">Nije dostupno</span></td></>}<td><button disabled={saving || (adding && unavailable)} onClick={() => { setConfirmation(student); setError('') }} aria-label={`${adding ? 'Dodaj' : 'Ukloni'} ${student.firstName} ${student.lastName}`}>{adding ? 'Dodaj' : 'Ukloni'}</button>{!adding && <Link className="groups-transfer" to={`/students/${student.id}/edit`}>Premjesti</Link>}</td></tr>)}
      </tbody></table></div>
      {members.data.items.length === 0 && <p className="groups-empty">{adding ? 'Nema dostupnih učenika za ovu pretragu.' : 'Grupa još nema članova.'}</p>}
      <Pager page={members.data} label="učenika" onPage={(value) => { setPage(value); setConfirmation(undefined) }} disabled={saving} />
    </> : <Message error={members?.error} retry={() => setRetry((value) => value + 1)} loading="Učitavanje učenika…" />}
    {confirmation && <div className="groups-confirmation" role="group" aria-label="Potvrda promjene članstva">
      <p>{adding ? `Dodati ${confirmation.firstName} ${confirmation.lastName} u ${group.name}? Učenik preuzima program ${group.programName} i grupni način rada.` : `Ukloniti ${confirmation.firstName} ${confirmation.lastName} iz grupe? Učenik ostaje u aplikaciji i istom programu, s individualnim načinom rada.`}</p>
      <button className="groups-primary" disabled={saving} onClick={() => void save()}>{saving ? 'Spremanje…' : 'Potvrdi promjenu'}</button><button disabled={saving} onClick={() => { setConfirmation(undefined); setError('') }}>Odustani</button>
      {error && <div role="alert"><p>{error}</p><button onClick={reload}>Osvježi podatke</button></div>}
    </div>}
    {!adding && unavailable && <p role="status">{group.memberCount >= group.capacity ? 'Grupa je popunjena.' : 'Dodavanje je dostupno samo aktivnim grupama.'}</p>}
    <button className="groups-add-member" disabled={saving || (!adding && unavailable)} onClick={() => { setAdding(!adding); setPage(1); setSearch(''); setConfirmation(undefined); setError('') }}>{adding ? '← Povratak na članove' : '+ Dodaj učenika u grupu'}</button>
    {!adding && <small className="groups-boundary">Razina i prisutnost nisu dostupni u ovoj fazi. Premjesti otvara postojeći obrazac za uređivanje učenika.</small>}
  </div>
}

function Schedule({ group, revision }: { group: Group; revision: number }) {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const sessions = useGroupResource<Page<GroupSession>>(`/groups/${group.id}/sessions?page=${page}&pageSize=8`, revision + retry)
  return <section className="groups-schedule"><h3>Redoviti raspored</h3>
    {group.slots.length ? <ul>{group.slots.map((slot, index) => <li key={index}><strong>{slotLabel(slot)}</strong><span>{slot.location ?? (slot.online ? 'Online' : 'Lokacija nije postavljena')} · {slot.timeZoneId}</span></li>)}</ul> : <p>Nema trenutačno važećih redovitih termina.</p>}
    {group.slots.length === 14 && <p>Prikazano je prvih 14 redovitih pravila.</p>}
    <h3>Nadolazeći termini</h3><p>Samo spremljeni, neotkazani termini. Redovita pravila sama ne stvaraju termine.</p>
    {sessions?.data ? <><ul>{sessions.data.items.map((session) => <li key={session.id}><strong>{new Date(session.startsAtUtc).toLocaleString('hr-HR', { timeZone: session.timeZoneId, dateStyle: 'medium', timeStyle: 'short' })}–{new Date(session.endsAtUtc).toLocaleTimeString('hr-HR', { timeZone: session.timeZoneId, hour: '2-digit', minute: '2-digit' })}</strong><span>{session.location ?? (session.online ? 'Online' : 'Lokacija nije postavljena')} · {session.timeZoneId}</span></li>)}</ul>{!sessions.data.items.length && <p>Nema nadolazećih termina.</p>}<Pager page={sessions.data} label="termina" onPage={setPage} /></> : <Message error={sessions?.error} retry={() => setRetry((value) => value + 1)} loading="Učitavanje termina…" />}
    <button disabled>Pogledaj cijeli raspored</button><small>Otvaranje kalendara i detalja termina dolazi u fazi rasporeda.</small>
  </section>
}

function Stat({ icon, label, value, note }: { icon: string; label: string; value: number; note: string }) {
  return <article className={`groups-stat groups-stat--${icon}`}><span className="groups-stat-icon"><Icon kind={icon} /></span><div><h2>{label}</h2><strong>{value}</strong><p>{note}</p></div></article>
}
function Status({ group }: { group: Group }) { return <span className={`groups-status groups-status--${group.status}`}>{statuses[group.status]}</span> }
function Message({ error, retry, loading }: { error?: string; retry: () => void; loading?: string }) { return <div className="groups-message" role={error ? 'alert' : 'status'}>{error ? <><p>{error}</p><button onClick={retry}>Pokušaj ponovno</button></> : loading}</div> }
function Pager({ page, label, onPage, disabled = false }: { page: { page: number; pageSize: number; totalCount: number; totalPages: number }; label: string; onPage: (value: number) => void; disabled?: boolean }) {
  return <nav className="groups-pager" aria-label={`Stranice ${label}`}><small>Prikazano {page.totalCount && (page.page - 1) * page.pageSize < page.totalCount ? (page.page - 1) * page.pageSize + 1 : 0}–{Math.min(page.page * page.pageSize, page.totalCount)} od {page.totalCount} {label}</small><div><button aria-label={`Prethodna stranica ${label}`} disabled={disabled || page.page <= 1} onClick={() => onPage(page.page - 1)}>‹</button><span>{page.page}</span><button aria-label={`Sljedeća stranica ${label}`} disabled={disabled || page.page >= page.totalPages} onClick={() => onPage(page.page + 1)}>›</button></div></nav>
}
function initials(name: string) { return name.split(/\s+/).map((part) => part[0]).join('').slice(0, 2).toUpperCase() }
function duration(slots: Slot[]) { const values = [...new Set(slots.map((slot) => (Number(slot.end.slice(0, 2)) * 60 + Number(slot.end.slice(3, 5))) - (Number(slot.start.slice(0, 2)) * 60 + Number(slot.start.slice(3, 5)))))] ; return values.length === 1 ? `${values[0]} min` : values.length ? 'Različita trajanja' : 'Nije postavljeno' }
function locations(slots: Slot[]) { const values = [...new Set(slots.map((slot) => slot.location ?? (slot.online ? 'Online' : 'Nije postavljena')))]; return values.length === 1 ? values[0] : values.length ? 'Više lokacija' : 'Nije postavljena' }
function Icon({ kind }: { kind: string }) { return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">{kind === 'clock' ? <><circle cx="12" cy="12" r="9" /><path d="M12 6v6l4 2" /></> : kind === 'seats' ? <><path d="M5 13V5h14v8M3 12v6h18v-6M6 18v4m12-4v4M7 9h10" /></> : <><circle cx="9" cy="7" r="3" /><path d="M3 21v-4a6 6 0 0 1 12 0v4M16 4a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 4v4" /></>}</svg> }
