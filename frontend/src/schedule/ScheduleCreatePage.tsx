import { useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { GroupUnsavedGuard } from '../groups/GroupUnsavedGuard.tsx'
import type { Group } from '../groups/groupsApi.ts'
import type { StudentListItem } from '../students/studentsApi.ts'
import { todayInZone } from './calendarDate.ts'
import {
  createScheduleSession,
  type ScheduleSessionDetail,
  useScheduleCreateContexts,
  useScheduleLocations,
  useScheduleSessionDetail,
} from './scheduleApi.ts'
import './ScheduleCreatePage.css'

const timeZone = 'Europe/Zagreb'

type ContextChoice = {
  id: string
  name: string
  program: string
  grade: string
  memberCount?: number
  hasRegularSchedule?: boolean
}

export function ScheduleCreatePage() {
  const [params] = useSearchParams()
  const duplicateId = params.get('duplicate') ?? ''
  const duplicate = useScheduleSessionDetail(duplicateId, 0, Boolean(duplicateId))
  if (duplicateId && !duplicate) return <section className="schedule-create-page"><Notice status>Učitavanje podataka termina za dupliciranje…</Notice></section>
  return <ScheduleCreateForm initialDate={params.get('date')} duplicate={duplicate?.data} duplicateError={duplicate?.error} />
}

function ScheduleCreateForm({ initialDate, duplicate, duplicateError }: {
  readonly initialDate: string | null
  readonly duplicate?: ScheduleSessionDetail
  readonly duplicateError?: string
}) {
  const navigate = useNavigate()
  const duplicateStart = duplicate ? localParts(duplicate.startsAtUtc, duplicate.timeZoneId) : undefined
  const duplicateEnd = duplicate ? localParts(duplicate.endsAtUtc, duplicate.timeZoneId) : undefined
  const saved = useRef(false)
  const inFlight = useRef(false)
  const [mode, setMode] = useState<1 | 2>(duplicate?.deliveryMode ?? 2)
  const [contextSearch, setContextSearch] = useState('')
  const [context, setContext] = useState<ContextChoice | undefined>(duplicate ? {
    id: duplicate.groupId ?? duplicate.studentId!, name: duplicate.contextName,
    program: duplicate.programName ?? 'Program nije postavljen', grade: duplicate.schoolGrade,
    memberCount: duplicate.deliveryMode === 2 ? duplicate.participants.length : undefined,
    hasRegularSchedule: duplicate.deliveryMode === 2 && duplicate.isSeriesOccurrence,
  } : undefined)
  const [title, setTitle] = useState(duplicate?.title ?? '')
  const [notes, setNotes] = useState(duplicate?.notes ?? '')
  const [date, setDate] = useState(duplicateStart?.date ?? (validDate(initialDate) ? initialDate : todayInZone(timeZone)))
  const [startsAt, setStartsAt] = useState(duplicateStart?.time ?? '')
  const [endsAt, setEndsAt] = useState(duplicateEnd?.time ?? '')
  const [repeatWeekly, setRepeatWeekly] = useState(false)
  const [endsOn, setEndsOn] = useState('')
  const [locationMode, setLocationMode] = useState<'none' | 'physical' | 'online'>(duplicate?.locationId ? 'physical' : duplicate?.online ? 'online' : 'none')
  const [locationSearch, setLocationSearch] = useState('')
  const [locationId, setLocationId] = useState(duplicate?.locationId ?? '')
  const [onlineUrl, setOnlineUrl] = useState('')
  const [revision, setRevision] = useState(0)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const contexts = useScheduleCreateContexts(mode, contextSearch, revision)
  const locations = useScheduleLocations(locationSearch, revision)

  const choices: ContextChoice[] = (() => {
    if (!contexts?.data) return []
    if (mode === 2) return contexts.data.items.map(item => {
      const group = item as Group
      return { id: group.id, name: group.name, program: group.programName, grade: group.schoolGrade, memberCount: group.memberCount, hasRegularSchedule: group.slots.length > 0 }
    })
    return contexts.data.items.map(item => {
      const student = item as StudentListItem
      return { id: student.id, name: `${student.firstName} ${student.lastName}`, program: student.program?.name ?? 'Program nije postavljen', grade: student.schoolGrade.name }
    })
  })()

  const locationChoices = [...(duplicate?.locationId && duplicate.locationName
    && !locations?.data?.items.some(item => item.id === duplicate.locationId)
    ? [{ id: duplicate.locationId, name: duplicate.locationName }] : []), ...(locations?.data?.items ?? [])]
  const selectedLocation = locationChoices.find(item => item.id === locationId)
  const dirty = Boolean(context || title || notes || startsAt || endsAt || repeatWeekly || endsOn || locationMode !== 'none' || onlineUrl)
  const duration = startsAt && endsAt && endsAt > startsAt ? minutes(endsAt) - minutes(startsAt) : 0

  function changeMode(next: 1 | 2) {
    setMode(next)
    setContext(undefined)
    setContextSearch('')
    if (next === 2) { setRepeatWeekly(false); setEndsOn('') }
  }

  function chooseContext(id: string) {
    setContext(choices.find(item => item.id === id))
  }

  function chooseLocationMode(next: 'none' | 'physical' | 'online') {
    setLocationMode(next)
    if (next !== 'physical') setLocationId('')
    if (next !== 'online') setOnlineUrl('')
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (inFlight.current) return
    if (!context || !date || !startsAt || !endsAt || endsAt <= startsAt) {
      setSaveError('Odaberite grupu ili učenika te unesite ispravan datum i vrijeme termina.')
      return
    }
    if (repeatWeekly && endsOn && endsOn < date) {
      setSaveError('Završni datum ponavljanja ne može biti prije prvog termina.')
      return
    }
    if (locationMode === 'physical' && !locationId) {
      setSaveError('Odaberite fizičku lokaciju ili promijenite vrstu lokacije.')
      return
    }
    if (locationMode === 'online' && !/^https:\/\//i.test(onlineUrl.trim())) {
      setSaveError('Poveznica za online sat mora biti potpuna HTTPS adresa.')
      return
    }

    inFlight.current = true
    setSaving(true)
    setSaveError('')
    try {
      const result = await createScheduleSession({
        deliveryMode: mode,
        contextId: context.id,
        title: title.trim() || null,
        notes: notes.trim() || null,
        date,
        startsAt,
        endsAt,
        repeatWeekly,
        endsOn: repeatWeekly && endsOn ? endsOn : null,
        locationId: locationMode === 'physical' ? locationId : null,
        onlineMeetingUrl: locationMode === 'online' ? onlineUrl.trim() : null,
      })
      saved.current = true
      navigate(`/schedule/${encodeURIComponent(result.id)}`, { replace: true, state: { createdSessionCount: result.sessionCount } })
    } catch (error) {
      setSaveError(error instanceof ApiError ? error.message : 'Termin nije moguće spremiti. Provjerite vezu i pokušajte ponovno.')
    } finally {
      inFlight.current = false
      setSaving(false)
    }
  }

  return <section className="schedule-create-page" aria-labelledby="schedule-create-title">
    <nav className="schedule-create-breadcrumb" aria-label="Putanja"><Link to="/schedule">Raspored</Link><span>›</span><strong>Novi termin</strong></nav>
    <header className="schedule-create-header">
      <div><h1 id="schedule-create-title">3.3 Novi termin</h1><p>Zakažite novi grupni ili individualni termin.</p></div>
      <div><Link className="schedule-create-cancel" to={`/schedule?date=${date}`}>Odustani</Link><button type="submit" form="schedule-create-form" disabled={saving}>{saving ? 'Spremanje…' : 'Spremi termin'}</button></div>
    </header>

    {duplicateError && <Notice error>Izvorni termin nije moguće učitati. Možete nastaviti s praznim obrascem.</Notice>}
    {saveError && <Notice error>{saveError}<button type="button" onClick={() => { setRevision(value => value + 1); setSaveError('') }}>Osvježi podatke</button></Notice>}

    <form id="schedule-create-form" onSubmit={event => void submit(event)}>
      <fieldset disabled={saving}>
        <div className="schedule-create-layout">
          <main>
            <Card step="1" title="Osnovni podaci">
              <div className="schedule-create-segmented" role="radiogroup" aria-label="Način rada">
                <label><input type="radio" name="mode" checked={mode === 2} onChange={() => changeMode(2)} /><span><b>♟</b><strong>Grupni sat</strong><small>Odaberite postojeću grupu</small></span></label>
                <label><input type="radio" name="mode" checked={mode === 1} onChange={() => changeMode(1)} /><span><b>♙</b><strong>Individualni sat</strong><small>Odaberite jednog učenika</small></span></label>
              </div>
              <Field label={mode === 2 ? 'Pretražite i odaberite grupu' : 'Pretražite i odaberite učenika'} required>
                <input type="search" maxLength={100} placeholder={mode === 2 ? 'Upišite naziv grupe…' : 'Upišite ime učenika…'} value={contextSearch} onChange={event => { setContextSearch(event.target.value); setContext(undefined) }} />
                <select required aria-label={mode === 2 ? 'Grupa' : 'Učenik'} value={context?.id ?? ''} onChange={event => chooseContext(event.target.value)}>
                  <option value="">{!contexts ? 'Učitavanje…' : `Odaberite ${mode === 2 ? 'grupu' : 'učenika'}`}</option>
                  {context && !choices.some(item => item.id === context.id) && <option value={context.id}>{context.name}</option>}
                  {choices.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </Field>
              {contexts?.error && <InlineError retry={() => setRevision(value => value + 1)}>Popis nije moguće učitati.</InlineError>}
              {contexts?.data && contexts.data.items.length === 0 && <p className="schedule-create-muted">Nema aktivnih rezultata za ovu pretragu.</p>}
              {context && <div className="schedule-create-context"><span className="schedule-create-avatar" aria-hidden="true">{initials(context.name)}</span><div><strong>{context.name}</strong><small>{context.program} · {context.grade}{context.memberCount !== undefined ? ` · ${context.memberCount} učenika` : ''}</small></div></div>}
              {mode === 2 && context?.hasRegularSchedule && <p className="schedule-create-warning">Ova grupa već ima redoviti raspored. Ovdje dodajete dodatni konkretni termin. <Link to={`/students/groups/${context.id}/edit`}>Promijeni redoviti raspored grupe</Link>.</p>}
              <Field label="Naziv termina (opcionalno)"><input maxLength={200} value={title} placeholder={context ? `${context.name} – tema sata` : 'Npr. priprema za ispit'} onChange={event => setTitle(event.target.value)} /><small>{title.length}/200 · Ako ostane prazno, prikazat će se naziv grupe ili učenika.</small></Field>
              <Field label="Opis / napomena (opcionalno)"><textarea rows={4} maxLength={2000} value={notes} placeholder="Kratka napomena za ovaj termin" onChange={event => setNotes(event.target.value)} /><small>{notes.length}/2000</small></Field>
            </Card>

            <Card step="2" title="Datum i vrijeme">
              <div className="schedule-create-row schedule-create-row--three">
                <Field label="Datum" required><input type="date" required value={date} onChange={event => setDate(event.target.value)} /></Field>
                <Field label="Početak" required><input type="time" required step={60} value={startsAt} onChange={event => setStartsAt(event.target.value)} /></Field>
                <Field label="Završetak" required><input type="time" required step={60} value={endsAt} onChange={event => setEndsAt(event.target.value)} /></Field>
              </div>
              <p className="schedule-create-duration">◷ Trajanje: <strong>{duration > 0 ? `${duration} minuta` : 'odredit će se iz vremena'}</strong> · Vrijeme: {timeZone}</p>
              <h3 className="schedule-create-subtitle">Ponavljanje</h3>
              <div className="schedule-create-repeat">
                <label><input type="radio" name="repeat" checked={!repeatWeekly} onChange={() => { setRepeatWeekly(false); setEndsOn('') }} /><span><strong>Ne ponavlja se</strong><small>Stvara se jedan konkretni termin.</small></span></label>
                <label className={mode === 2 ? 'is-disabled' : ''}><input type="radio" name="repeat" checked={repeatWeekly} disabled={mode === 2} onChange={() => setRepeatWeekly(true)} /><span><strong>Redovno – svaki tjedan</strong><small>{mode === 2 ? 'Redoviti raspored grupe uređuje se na ekranu grupe.' : 'Početni horizont termina je 12 tjedana.'}</small></span></label>
              </div>
              {repeatWeekly && <><p className="schedule-create-weekday">Dan u tjednu: <strong>{weekday(date)}</strong></p><Field label="Ponavlja se do (opcionalno)"><input type="date" min={date} value={endsOn} onChange={event => setEndsOn(event.target.value)} /><small>Bez završnog datuma sprema se otvoreno tjedno pravilo i početnih 12 tjedana termina.</small></Field></>}
            </Card>

            <Card step="3" title="Lokacija">
              <div className="schedule-create-location-types" role="radiogroup" aria-label="Vrsta lokacije">
                {([['none', 'Bez lokacije'], ['physical', 'Učionica / lokacija'], ['online', 'Online sat']] as const).map(([value, label]) => <label key={value}><input type="radio" name="location" checked={locationMode === value} onChange={() => chooseLocationMode(value)} />{label}</label>)}
              </div>
              {locationMode === 'physical' && <div className="schedule-create-row"><Field label="Pretražite lokacije"><input type="search" maxLength={100} value={locationSearch} onChange={event => setLocationSearch(event.target.value)} /></Field><Field label="Lokacija" required><select required value={locationId} onChange={event => setLocationId(event.target.value)}><option value="">Odaberite lokaciju</option>{locationChoices.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field></div>}
              {locationMode === 'online' && <Field label="HTTPS poveznica za videopoziv" required><input type="url" required maxLength={2048} placeholder="https://…" value={onlineUrl} onChange={event => setOnlineUrl(event.target.value)} /></Field>}
              {locations?.error && locationMode === 'physical' && <InlineError retry={() => setRevision(value => value + 1)}>Lokacije nije moguće učitati.</InlineError>}
            </Card>

            <Card step="4" title="Dodatne postavke">
              <div className="schedule-create-future"><label>Boja termina<select disabled><option>Prema programu</option></select></label><label>Podsjetnik sebi<select disabled><option>Nije dostupno</option></select></label><label>Podsjetnik učenicima<select disabled><option>Nije dostupno</option></select></label></div>
              <p className="schedule-create-muted">Boje i podsjetnici čekaju zaseban notification contract i ne spremaju lažno stanje.</p>
            </Card>

            <Card step="5" title="Obavijesti (opcionalno)">
              <label className="schedule-create-checkbox"><input type="checkbox" disabled /> Automatski obavijesti učenike / roditelje o novom terminu</label>
              <Field label="Poruka"><textarea rows={4} disabled value="Slanje poruke nije dostupno dok notification contract nije zaključan." readOnly /></Field>
              <p className="schedule-create-muted">Obavijesti nisu dio Session writea u ovoj fazi.</p>
            </Card>
          </main>

          <aside>
            <section className="schedule-create-summary ds-card"><h2>Sažetak termina</h2><span className="schedule-create-summary-kind">{mode === 2 ? 'Grupni sat' : 'Individualni sat'}</span><h3>{context?.name ?? 'Nije odabrano'}</h3><p>{context ? `${context.program} · ${context.grade}` : 'Odaberite grupu ili učenika'}</p><dl><Summary label="Datum" value={date ? formatDate(date) : 'Nije odabran'} /><Summary label="Vrijeme" value={startsAt && endsAt ? `${startsAt}–${endsAt}` : 'Nije uneseno'} /><Summary label="Trajanje" value={duration > 0 ? `${duration} min` : '—'} /><Summary label="Ponavljanje" value={repeatWeekly ? `Svaki tjedan${endsOn ? ` do ${formatDate(endsOn)}` : ', bez završnog datuma'}` : 'Ne ponavlja se'} /><Summary label="Lokacija" value={locationMode === 'physical' ? selectedLocation?.name ?? 'Nije odabrana' : locationMode === 'online' ? 'Online' : 'Bez lokacije'} />{context?.memberCount !== undefined && <Summary label="Učenici" value={String(context.memberCount)} />}</dl><p>Prije spremanja sustav ponovno provjerava Teacher i lokacijske konflikte.</p></section>
          </aside>
        </div>
      </fieldset>
    </form>
    <footer className="schedule-create-footer"><span>ⓘ 3.3 stvara konkretni dodatni ili individualni termin.</span><span>Redoviti raspored grupe uređuje se kroz 2.8/2.9.</span></footer>
    <GroupUnsavedGuard dirty={dirty} saving={saving} saved={saved} />
  </section>
}

function Card({ step, title, children }: { readonly step: string; readonly title: string; readonly children: ReactNode }) { return <section className="schedule-create-card ds-card"><h2><span>{step}</span>{title}</h2>{children}</section> }
function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: ReactNode }) { return <label className="schedule-create-field"><span>{label}{required && <b aria-hidden="true"> *</b>}</span>{children}</label> }
function Notice({ children, error = false, status = false }: { readonly children: ReactNode; readonly error?: boolean; readonly status?: boolean }) { return <div className={`schedule-create-notice${error ? ' is-error' : ''}`} role={error ? 'alert' : status ? 'status' : undefined}>{children}</div> }
function InlineError({ children, retry }: { readonly children: ReactNode; readonly retry: () => void }) { return <div className="schedule-create-inline-error" role="alert">{children} <button type="button" onClick={retry}>Pokušaj ponovno</button></div> }
function Summary({ label, value }: { readonly label: string; readonly value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function minutes(value: string) { const [hours, minute] = value.split(':').map(Number); return hours * 60 + minute }
function initials(value: string) { return value.split(/\s+/).filter(Boolean).map(item => item[0]).join('').slice(0, 3).toUpperCase() }
function validDate(value: string | null): value is string { return Boolean(value && /^\d{4}-\d{2}-\d{2}$/.test(value)) }
function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium' }).format(new Date(`${value}T12:00:00`)) }
function weekday(value: string) { return value ? new Intl.DateTimeFormat('hr-HR', { weekday: 'long' }).format(new Date(`${value}T12:00:00`)) : 'nije odabran' }
function localParts(value: string, zone: string) { const date = new Date(value); const parts = new Intl.DateTimeFormat('en-CA', { timeZone: zone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(date); const get = (type: Intl.DateTimeFormatPartTypes) => parts.find(part => part.type === type)?.value ?? ''; return { date: `${get('year')}-${get('month')}-${get('day')}`, time: `${get('hour')}:${get('minute')}` } }
