import { useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, putJson } from '../api/apiClient.ts'
import type { StudentCreateOptions } from '../students/studentsApi.ts'
import { GroupUnsavedGuard } from './GroupUnsavedGuard.tsx'
import { GroupLocationPicker, GroupScheduleEditor } from './GroupScheduleEditor.tsx'
import { changeMembership, useGroupResource, type Group, type GroupEdit, type GroupStudent, type Page } from './groupsApi.ts'
import { dayNames, emptySchedule, type GroupSchedule } from './groupCreationModels.ts'
import './GroupCreatePage.css'
import './GroupEditPage.css'

const statusNames = { 1: 'Aktivna', 2: 'Na čekanju', 3: 'Neaktivna' } as const

export function GroupEditPage() {
  const { groupId = '' } = useParams()
  const [revision, setRevision] = useState(0)
  const group = useGroupResource<GroupEdit>(`/groups/${encodeURIComponent(groupId)}/edit`, revision)
  const options = useGroupResource<StudentCreateOptions>('/students/create-options', revision)
  if (!group || !options) return <p role="status">Učitavanje grupe…</p>
  if (group.error || options.error) return <div role="alert"><p>Podatke grupe nije moguće učitati.</p><button onClick={() => setRevision(value => value + 1)}>Pokušaj ponovno</button></div>
  if (!group.data || !options.data) return null
  return <GroupEditForm key={`${group.data.rowVersion}:${revision}`} group={group.data} options={options.data}
    reload={() => setRevision(value => value + 1)} />
}

function GroupEditForm({ group, options, reload }: { readonly group: GroupEdit; readonly options: StudentCreateOptions; readonly reload: () => void }) {
  const navigate = useNavigate()
  const saved = useRef(false)
  const inFlight = useRef(false)
  const initialSchedule = scheduleFrom(group)
  const [name, setName] = useState(group.name)
  const [description, setDescription] = useState(group.description ?? '')
  const [programId, setProgramId] = useState(group.programId)
  const [gradeId, setGradeId] = useState(group.schoolGradeId)
  const [capacity, setCapacity] = useState(String(group.capacity))
  const [status, setStatus] = useState(String(group.status))
  const [schedule, setSchedule] = useState(initialSchedule)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const dirty = name !== group.name || description !== (group.description ?? '') || programId !== group.programId
    || gradeId !== group.schoolGradeId || capacity !== String(group.capacity) || status !== String(group.status)
    || JSON.stringify(schedule) !== JSON.stringify(initialSchedule)
  const program = options.programs.find(item => item.id === programId)
  const grade = options.schoolGrades.find(item => item.id === gradeId)

  function changeSchedule(next: GroupSchedule) {
    if (group.slots.length && next.slots.length && JSON.stringify(next) !== JSON.stringify(initialSchedule)
      && (!next.startsOn || next.startsOn <= localDate(0))) next = { ...next, startsOn: localDate(1) }
    setSchedule(next)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (inFlight.current) return
    const parsedCapacity = Number(capacity)
    if (!name.trim() || !Number.isInteger(parsedCapacity) || parsedCapacity < group.memberCount) {
      setSaveError('Provjerite naziv i kapacitet grupe.'); return
    }
    if (schedule.slots.some(slot => !slot.start || !slot.end || slot.end <= slot.start)) {
      setSaveError('Završetak svakog termina mora biti nakon početka istoga dana.'); return
    }
    inFlight.current = true; setSaving(true); setSaveError('')
    try {
      await putJson<{ id: string; sessionCount: number }>(`/groups/${group.id}`, {
        name: name.trim(), description: description.trim() || null, programId, schoolGradeId: gradeId,
        status: Number(status), capacity: parsedCapacity, rowVersion: group.rowVersion,
        slots: schedule.slots.map(slot => ({ dayOfWeek: slot.dayOfWeek, start: slot.start, end: slot.end })),
        scheduleStartsOn: schedule.slots.length ? schedule.startsOn : null,
        scheduleEndsOn: schedule.slots.length ? schedule.endsOn || null : null,
        locationId: schedule.slots.length ? schedule.locationId || null : null,
      })
      saved.current = true
      navigate(`/students/groups?group=${encodeURIComponent(group.id)}`, { replace: true, state: { updatedGroupName: name.trim() } })
    } catch (error) {
      setSaveError(error instanceof ApiError ? error.message : 'Promjene grupe nije moguće spremiti.')
    } finally { inFlight.current = false; setSaving(false) }
  }

  return <section className="group-create-page group-edit-page" aria-labelledby="group-edit-title">
    <nav className="group-create-breadcrumb" aria-label="Putanja"><Link to="/students">Učenici</Link><span aria-hidden="true">›</span><Link to="/students/groups">Grupe</Link><span aria-hidden="true">›</span><span>Uredi grupu</span></nav>
    <header className="group-create-hero"><div><h1 id="group-edit-title">2.9 Uredi grupu</h1><p>Uredi podatke grupe, raspored i članove grupe.</p></div><div className="group-create-actions">
      <Link className="ds-action ds-action--secondary" to={`/students/groups?group=${group.id}`}>Odustani</Link>
      <button className="ds-action ds-action--danger" type="button" disabled title="Hard delete nije dopušten; archive lifecycle s rasporedom još nije zaključan.">Arhiviraj grupu</button>
      <button className="ds-action group-create-save" type="submit" form="group-edit-form" disabled={saving || !dirty}>{saving ? 'Spremanje…' : 'Spremi promjene'}</button>
    </div></header>
    {saveError && <div className="group-create-notice" role="alert">{saveError}<button type="button" onClick={reload}>Osvježi podatke</button></div>}
    <form id="group-edit-form" onSubmit={event => void submit(event)}><fieldset className="group-create-form-body" disabled={saving}>
      <div className="group-edit-grid">
        <Card title="Osnovni podaci grupe">
          <Field label="Naziv grupe" required><input required maxLength={160} value={name} onChange={event => setName(event.target.value)} /></Field>
          <Field label="Program / fokus" required><select required disabled={group.memberCount > 0} value={programId} onChange={event => setProgramId(event.target.value)}>{options.programs.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          {group.memberCount > 0 && <p className="group-edit-rule">Program nije moguće promijeniti dok grupa ima aktivne učenike. Najprije ih premjestite ili uklonite.</p>}
          <Field label="Razred / razina" required><select required value={gradeId} onChange={event => setGradeId(event.target.value)}>{options.schoolGrades.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <Field label="Opis grupe (opcionalno)"><textarea rows={5} maxLength={1000} value={description} onChange={event => setDescription(event.target.value)} /></Field><small className="group-create-counter">{description.length}/1000</small>
        </Card>
        <Card title="Način rada i kapacitet">
          <Field label="Način rada"><input readOnly value="Grupa" /></Field>
          <Field label="Maksimalan broj učenika" required><input required type="number" min={Math.max(1, group.memberCount)} step={1} value={capacity} onChange={event => setCapacity(event.target.value)} /></Field>
          <p>Trenutno: <strong>{group.memberCount}</strong> učenika · {Math.max(0, Number(capacity) - group.memberCount)} slobodnih mjesta</p>
          {schedule.slots.length > 0 && <GroupLocationPicker value={schedule} onChange={changeSchedule} />}
          <Field label="Status grupe" required><select value={status} onChange={event => setStatus(event.target.value)}><option value="1">Aktivna</option><option value="2">Na čekanju</option><option value="3">Neaktivna</option></select></Field>
        </Card>
        <Card title="Raspored grupe"><div className="group-edit-info">Promjene rasporeda automatski se odražavaju u kalendaru. Postojeći raspored mijenja se od odabranog budućeg datuma.</div><GroupScheduleEditor value={schedule} onChange={changeSchedule} /></Card>
        <Card title="Sažetak grupe"><div className="group-edit-identity"><span>{initials(name)}</span></div><dl><dt>Naziv</dt><dd>{name || '—'}</dd><dt>Status</dt><dd>{statusNames[Number(status) as 1 | 2 | 3]}</dd><dt>Program</dt><dd>{program?.name ?? '—'}</dd><dt>Razred</dt><dd>{grade?.name ?? '—'}</dd><dt>Kapacitet</dt><dd>{group.memberCount} / {capacity || '—'}</dd><dt>Termini</dt><dd>{schedule.slots.length ? schedule.slots.map(slot => `${dayNames[slot.dayOfWeek]} ${slot.start}–${slot.end}`).join(', ') : 'Bez rasporeda'}</dd><dt>Lokacija</dt><dd>{schedule.locationName || 'Bez lokacije'}</dd></dl></Card>
      </div>
      <div className="group-edit-lower">
        <Card title="Učenici u grupi"><EditMembers group={group} dirty={dirty} reload={reload} /></Card>
        <Card title="Materijali i ciljevi grupe"><p>Materijali, ciljna razina i ciljevi ostaju nedostupni do svojih zaključanih faza. Ne stvaraju se paralelni podaci grupe.</p></Card>
        <Card title="Bilješke o grupi"><p>Bilješke i postavke privatnosti nisu dio zaključanog 3.7 podatkovnog contracta i ovdje se ne spremaju.</p></Card>
      </div>
      <aside className="group-create-notice">Spremanje ažurira isti Group zapis i canonical raspored. Učenikovi rezultati znanja ne mijenjaju se ovom radnjom.</aside>
    </fieldset></form>
    <GroupUnsavedGuard dirty={dirty} saving={saving} saved={saved} />
  </section>
}

function EditMembers({ group, dirty, reload }: { readonly group: GroupEdit; readonly dirty: boolean; readonly reload: () => void }) {
  const [adding, setAdding] = useState(false)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const path = `/groups/${group.id}/${adding ? 'candidates' : 'students'}?page=${page}&pageSize=8&search=${encodeURIComponent(search)}`
  const people = useGroupResource<Page<GroupStudent>>(path)
  const groupContract: Group = { id: group.id, name: group.name, programId: group.programId,
    programName: group.programName, schoolGradeId: group.schoolGradeId, schoolGrade: group.schoolGrade,
    status: group.status === 1 ? 'active' : group.status === 2 ? 'on_hold' : 'inactive', capacity: group.capacity,
    memberCount: group.memberCount, rowVersion: group.rowVersion, slots: group.slots }
  async function change(student: GroupStudent) {
    if (dirty || saving) return
    setSaving(true); setError('')
    try { await changeMembership(groupContract, student, adding); reload() }
    catch (failure) { setError(failure instanceof Error ? failure.message : 'Članstvo nije promijenjeno.'); setSaving(false) }
  }
  return <div className="group-edit-members">
    <div className="group-edit-member-tabs"><button type="button" className={!adding ? 'active' : ''} onClick={() => { setAdding(false); setPage(1) }}>Članovi ({group.memberCount})</button><button type="button" className={adding ? 'active' : ''} onClick={() => { setAdding(true); setPage(1) }}>Dostupni učenici</button></div>
    <label className="group-create-field"><span>Pretraži učenike</span><input type="search" maxLength={100} value={search} onChange={event => { setSearch(event.target.value); setPage(1) }} /></label>
    {dirty && <p className="group-edit-rule">Najprije spremite ili odbacite promjene obrasca prije zasebne promjene članstva.</p>}
    {error && <p role="alert">{error}</p>}
    {!people ? <p role="status">Učitavanje učenika…</p> : people.error ? <p role="alert">Učenike nije moguće učitati.</p> : <><ul className="group-edit-people">{people.data?.items.map(student => <li key={student.id}><Link to={`/students/${student.id}`}>{student.firstName} {student.lastName}</Link><span>{student.schoolGrade}</span><button type="button" disabled={dirty || saving || adding && (group.status !== 1 || group.memberCount >= group.capacity)} onClick={() => void change(student)}>{adding ? 'Dodaj' : 'Ukloni'}</button></li>)}</ul>{!people.data?.items.length && <p>Nema učenika za ovaj prikaz.</p>}{people.data && <EditPager page={people.data} onPage={setPage} />}</>}
  </div>
}

function EditPager({ page, onPage }: { readonly page: Page<unknown>; readonly onPage: (page: number) => void }) { return <nav className="group-create-paging" aria-label="Stranice učenika"><button type="button" disabled={page.page <= 1} onClick={() => onPage(page.page - 1)}>Prethodna</button><span>{page.page} / {page.totalPages}</span><button type="button" disabled={page.page >= page.totalPages} onClick={() => onPage(page.page + 1)}>Sljedeća</button></nav> }
function scheduleFrom(group: GroupEdit): GroupSchedule { const first = group.slots[0]; return first ? { startsOn: first.startsOn, endsOn: first.endsOn ?? '', locationId: first.locationId ?? '', locationName: first.locationName ?? '', slots: group.slots.map(slot => ({ key: slot.seriesId, dayOfWeek: slot.dayOfWeek, start: slot.start.slice(0, 5), end: slot.end.slice(0, 5) })) } : emptySchedule }
function localDate(days: number) { const date = new Date(Date.now() + days * 86_400_000); return new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Zagreb' }).format(date) }
function initials(value: string) { return value.trim().split(/\s+/).map(part => part[0]).join('').slice(0, 3).toUpperCase() || 'G' }
function Card({ title, children }: { readonly title: string; readonly children: ReactNode }) { return <section className="ds-card group-create-card"><h2>{title}</h2>{children}</section> }
function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: ReactNode }) { return <label className="group-create-field"><span>{label}{required && <span aria-hidden="true"> *</span>}</span>{children}</label> }
