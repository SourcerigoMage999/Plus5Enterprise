import { useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError, postJson } from '../api/apiClient.ts'
import { useGroupResource } from './groupsApi.ts'
import type { StudentCreateOptions } from '../students/studentsApi.ts'
import { GroupCandidatePicker, type CreationCandidate } from './GroupCandidatePicker.tsx'
import { GroupScheduleEditor, GroupLocationPicker } from './GroupScheduleEditor.tsx'
import { emptySchedule, dayNames } from './groupCreationModels.ts'
import { GroupUnsavedGuard } from './GroupUnsavedGuard.tsx'
import './GroupCreatePage.css'

export function GroupCreatePage() {
  const navigate = useNavigate()
  const saved = useRef(false)
  const inFlight = useRef(false)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [schedule, setSchedule] = useState(emptySchedule)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [programId, setProgramId] = useState('')
  const [gradeId, setGradeId] = useState('')
  const [capacity, setCapacity] = useState('')
  const [selected, setSelected] = useState<CreationCandidate[]>([])
  const [revision, setRevision] = useState(0)
  const [capacityError, setCapacityError] = useState('')
  const options = useGroupResource<StudentCreateOptions>('/students/create-options', revision)
  const dirty = Boolean(name || description || programId || gradeId || capacity || selected.length || schedule.slots.length)

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (inFlight.current) return
    if (!name.trim() || !Number.isInteger(Number(capacity)) || Number(capacity) < selected.length) { setSaveError('Provjerite naziv i kapacitet grupe.'); return }
    if (schedule.slots.some(s => !s.start || !s.end || s.end <= s.start)) { setSaveError('Završetak svakog termina mora biti nakon početka istoga dana.'); return }
    inFlight.current = true
    setSaving(true)
    setSaveError('')
    try {
      const result = await postJson<{ id: string; sessionCount: number }>('/groups', {
        name: name.trim(), description: description.trim() || null, programId, schoolGradeId: gradeId, capacity: Number(capacity),
        members: selected.map(s => ({ studentId: s.id, rowVersion: s.rowVersion })),
        slots: schedule.slots.map(s => ({ dayOfWeek: s.dayOfWeek, start: s.start, end: s.end })),
        startsOn: schedule.slots.length ? schedule.startsOn : null, endsOn: schedule.slots.length ? schedule.endsOn || null : null,
        locationId: schedule.slots.length ? schedule.locationId || null : null,
      })
      saved.current = true
      navigate(`/students/groups?group=${encodeURIComponent(result.id)}`, { replace: true, state: { createdGroupName: name.trim() } })
    } catch (error) {
      setSaveError(error instanceof ApiError ? error.message : 'Grupu nije moguće spremiti. Provjerite vezu i pokušajte ponovno.')
    } finally { inFlight.current = false; setSaving(false) }
  }

  const program = options?.data?.programs.find(item => item.id === programId)
  const grade = options?.data?.schoolGrades.find(item => item.id === gradeId)
  const limit = Number(capacity)
  function changeCapacity(value: string) {
    if (selected.length > 0 && (!Number.isInteger(Number(value)) || Number(value) < selected.length)) {
      setCapacityError('Kapacitet ne može biti manji od broja odabranih učenika.')
      return
    }
    setCapacity(value)
    setCapacityError('')
  }

  return <section className="group-create-page" aria-labelledby="group-create-title">
    <nav className="group-create-breadcrumb" aria-label="Putanja"><Link to="/students">Učenici</Link><span aria-hidden="true">›</span><Link to="/students/groups">Grupe</Link><span aria-hidden="true">›</span><span>Nova grupa</span></nav>
    <header className="group-create-hero"><div><h1 id="group-create-title">2.8 Nova grupa</h1><p>Kreiraj novu grupu i dodaj učenike.</p></div>
      <div className="group-create-actions"><Link className="ds-action ds-action--secondary" to="/students/groups">Odustani</Link><button className="ds-action group-create-save" type="submit" form="group-create-form" disabled={saving || !options?.data?.programs.length || !options?.data?.schoolGrades.length}>{saving ? 'Spremanje…' : 'Kreiraj grupu'}</button></div>
    </header>
    {saveError && <div className="group-create-notice" role="alert">{saveError}<button type="button" onClick={() => { setSelected([]); setRevision(v => v + 1); setSaveError('') }}>Osvježi podatke i očisti odabir učenika</button></div>}
    {!options && <p role="status">Učitavanje programa i razreda…</p>}
    {options?.error && <div role="alert">Podatke obrasca nije moguće učitati. <button onClick={() => setRevision(value => value + 1)}>Pokušaj ponovno</button></div>}
    {options?.data && (!options.data.programs.length || !options.data.schoolGrades.length) && <p role="status">Za pripremu grupe potreban je postojeći program i školski razred. Katalog još nije dostupan.</p>}
    <form id="group-create-form" onSubmit={event => void submit(event)}><fieldset className="group-create-form-body" disabled={saving}>
      <div className="group-create-top">
        <Card title="Osnovni podaci grupe">
          <Field label="Naziv grupe" required><input required maxLength={160} value={name} onChange={event => setName(event.target.value)} /></Field>
          <Field label="Program / fokus" required><select required disabled={!options?.data} value={programId} onChange={event => setProgramId(event.target.value)}><option value="">Odaberite program</option>{options?.data?.programs.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <Field label="Razred / razina" required><select required disabled={!options?.data} value={gradeId} onChange={event => setGradeId(event.target.value)}><option value="">Odaberite razred</option>{options?.data?.schoolGrades.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <Field label="Opis grupe (opcionalno)"><textarea rows={5} maxLength={1000} value={description} onChange={event => setDescription(event.target.value)} /></Field>
          <small className="group-create-counter">{description.length}/1000</small>
        </Card>
        <Card title="Način rada i kapacitet">
          <Field label="Način rada"><input readOnly value="Grupa" /></Field>
          <Field label="Maksimalan broj učenika" required><input required type="number" min={1} step={1} value={capacity} aria-describedby={capacityError ? 'capacity-error' : undefined} onChange={event => changeCapacity(event.target.value)} /></Field>
          {capacityError && <p id="capacity-error" role="alert">{capacityError}</p>}
          <p>Odabrano: <strong>{selected.length}</strong>{capacity && <> / {capacity}</>} učenika</p>
          <p className="group-create-muted">Grupa može biti aktivna i bez učenika. Broj učenika ne mijenja automatski status grupe.</p>
          {schedule.slots.length > 0 && <GroupLocationPicker value={schedule} onChange={setSchedule} />}
        </Card>
        <div className="group-create-right">
          <Card title="Raspored grupe"><GroupScheduleEditor value={schedule} onChange={setSchedule} /></Card>
        </div>
      </div>
      <div className="group-create-bottom">
        <Card title="Dodaj učenike u grupu">
          {programId && gradeId ? <GroupCandidatePicker key={revision} programId={programId} gradeId={gradeId} capacity={limit} selected={selected} onChange={setSelected} /> : <p>Odaberite program i razred za prikaz postojećih učenika.</p>}
          <Link to="/students">Pogledaj sve učenike</Link>
        </Card>
        <div className="group-create-right"><Card title="Sažetak grupe"><dl><dt>Naziv</dt><dd>{name.trim() || 'Nije unesen'}</dd><dt>Program</dt><dd>{program?.name ?? 'Nije odabran'}</dd><dt>Razred</dt><dd>{grade?.name ?? 'Nije odabran'}</dd><dt>Kapacitet</dt><dd>{capacity || 'Nije unesen'}</dd><dt>Odabrani učenici</dt><dd>{selected.length}</dd><dt>Status</dt><dd>Aktivna</dd><dt>Raspored</dt><dd>{schedule.slots.length ? schedule.slots.map(s => `${dayNames[s.dayOfWeek]} ${s.start || '…'}–${s.end || '…'}`).join(', ') : 'Bez rasporeda'}</dd>{schedule.slots.length > 0 && <><dt>Razdoblje</dt><dd>{schedule.startsOn || 'Odaberite početak'} — {schedule.endsOn || 'Bez završnog datuma'}</dd><dt>Lokacija</dt><dd>{schedule.locationName || 'Bez lokacije'}</dd></>}</dl></Card><Card title="Napomene i ciljevi grupe"><p className="group-create-muted">Zasebne bilješke i ciljevi nisu dostupni u postojećem Group foundationu. Cilj grupe ne mijenja rezultate učenika.</p></Card><Card title="Materijali i plan rada"><p className="group-create-muted">Materijali su opcionalni i još nisu dostupni. Procjena znanja, fotografije i dijeljenje s drugim učiteljima ovdje se ne unose.</p></Card></div>
      </div>
    </fieldset></form>
    <p className="group-create-notice">Nakon spremanja grupa će biti aktivna. Odabrani učenici preuzimaju program grupe. Uneseni raspored i početni termini spremaju se zajedno s grupom.</p>
    <GroupUnsavedGuard dirty={dirty} saving={saving} saved={saved} />
  </section>
}

function Card({ title, children }: { readonly title: string; readonly children: ReactNode }) {
  return <section className="ds-card group-create-card"><h2>{title}</h2>{children}</section>
}
function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: ReactNode }) {
  return <label className="group-create-field"><span>{label}{required && <span aria-hidden="true"> *</span>}</span>{children}</label>
}
