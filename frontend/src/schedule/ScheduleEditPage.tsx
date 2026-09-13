import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { GroupUnsavedGuard } from '../groups/GroupUnsavedGuard.tsx'
import {
  cancelScheduleSession,
  previewScheduleSession,
  updateScheduleSession,
  useScheduleEdit,
  useScheduleLocations,
  type ScheduleEditInput,
  type ScheduleEditItem,
} from './scheduleApi.ts'
import './ScheduleEditPage.css'

export function ScheduleEditPage() {
  const { sessionId = '' } = useParams()
  const [revision, setRevision] = useState(0)
  const state = useScheduleEdit(sessionId, revision)
  if (!state) return <EditState message="Učitavanje podataka termina…" />
  if (state.error) return <EditState error message={state.status === 404 ? 'Termin nije pronađen ili mu nemate pristup.' : 'Podatke termina nije moguće učitati.'} action={() => setRevision(value => value + 1)} />
  if (!state.data) return null
  if (state.data.detail.status !== 1) return <EditState error message="Mijenjati se može samo zakazani termin." />
  return <ScheduleEditForm key={`${state.data.detail.id}:${state.data.rowVersion}`} item={state.data} />
}

function ScheduleEditForm({ item }: { readonly item: ScheduleEditItem }) {
  const navigate = useNavigate()
  const session = item.detail
  const originalStart = localParts(session.startsAtUtc, session.timeZoneId)
  const originalEnd = localParts(session.endsAtUtc, session.timeZoneId)
  const [title, setTitle] = useState(session.title ?? '')
  const [notes, setNotes] = useState(session.notes ?? '')
  const [date, setDate] = useState(originalStart.date)
  const [startsAt, setStartsAt] = useState(originalStart.time)
  const [endsAt, setEndsAt] = useState(originalEnd.time)
  const [scope, setScope] = useState<1 | 2>(1)
  const initialLocationMode = session.locationId ? 'physical' : item.onlineMeetingUrl ? 'online' : 'none'
  const [locationMode, setLocationMode] = useState<'none' | 'physical' | 'online'>(initialLocationMode)
  const [locationSearch, setLocationSearch] = useState('')
  const [locationId, setLocationId] = useState(session.locationId ?? '')
  const [onlineUrl, setOnlineUrl] = useState(item.onlineMeetingUrl ?? '')
  const [locationRevision, setLocationRevision] = useState(0)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [conflictResult, setConflictResult] = useState<{ key: string; state: 'clear' | 'conflict' | 'error' }>()
  const saved = useRef(false)
  const inFlight = useRef(false)
  const cancelDialog = useRef<HTMLDialogElement>(null)
  const locations = useScheduleLocations(locationSearch, locationRevision)
  const locationChoices = [...(session.locationId && session.locationName
    && !locations?.data?.items.some(location => location.id === session.locationId)
    ? [{ id: session.locationId, name: session.locationName }] : []), ...(locations?.data?.items ?? [])]

  const currentLocation = session.locationName ?? (session.online ? 'Online' : 'Bez lokacije')
  const currentDuration = minutes(originalEnd.time) - minutes(originalStart.time)
  const duration = startsAt && endsAt && endsAt > startsAt ? minutes(endsAt) - minutes(startsAt) : 0
  const dirty = title !== (session.title ?? '') || notes !== (session.notes ?? '')
    || date !== originalStart.date || startsAt !== originalStart.time || endsAt !== originalEnd.time
    || scope !== 1 || locationMode !== initialLocationMode || locationId !== (session.locationId ?? '')
    || onlineUrl !== (item.onlineMeetingUrl ?? '')
  const input = useMemo<ScheduleEditInput>(() => ({
    title: title.trim() || null,
    notes: notes.trim() || null,
    date,
    startsAt,
    endsAt,
    locationId: locationMode === 'physical' ? locationId : null,
    onlineMeetingUrl: locationMode === 'online' ? onlineUrl.trim() : null,
    scope,
    rowVersion: item.rowVersion,
  }), [date, endsAt, item.rowVersion, locationId, locationMode, notes, onlineUrl, scope, startsAt, title])
  const previewKey = JSON.stringify(input)
  const clientValid = Boolean(date && startsAt && endsAt && endsAt > startsAt
    && (locationMode !== 'physical' || locationId)
    && (locationMode !== 'online' || /^https:\/\//i.test(onlineUrl.trim())))
  const conflictState = !clientValid ? 'error'
    : conflictResult?.key === previewKey ? conflictResult.state : 'checking'

  useEffect(() => {
    if (!clientValid) return
    let active = true
    const timer = window.setTimeout(() => {
      previewScheduleSession(session.id, input).then(result => {
        if (active) setConflictResult({ key: previewKey, state: result.hasConflict ? 'conflict' : 'clear' })
      }).catch(() => { if (active) setConflictResult({ key: previewKey, state: 'error' }) })
    }, 350)
    return () => { active = false; window.clearTimeout(timer) }
  }, [clientValid, input, previewKey, session.id])

  function chooseScope(next: 1 | 2) {
    setScope(next)
    if (next === 2) {
      setDate(originalStart.date)
      setTitle(session.title ?? '')
      setNotes(session.notes ?? '')
    }
  }

  function chooseLocationMode(next: 'none' | 'physical' | 'online') {
    setLocationMode(next)
    if (next !== 'physical') setLocationId('')
    if (next !== 'online') setOnlineUrl('')
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (inFlight.current) return
    if (!clientValid) {
      setSaveError('Unesite ispravan datum, vrijeme i lokaciju termina.')
      return
    }
    if (conflictState === 'conflict') {
      setSaveError('Raspored se preklapa s drugim terminom. Promijenite vrijeme ili lokaciju.')
      return
    }

    inFlight.current = true
    setSaving(true)
    setSaveError('')
    try {
      const result = await updateScheduleSession(session.id, input)
      saved.current = true
      navigate(`/schedule/${encodeURIComponent(result.id)}`, { replace: true, state: { editedSessionCount: result.sessionCount } })
    } catch (error) {
      setSaveError(error instanceof ApiError ? error.message : 'Promjene nije moguće spremiti. Pokušajte ponovno.')
    } finally {
      inFlight.current = false
      setSaving(false)
    }
  }

  async function confirmCancel() {
    if (inFlight.current) return
    inFlight.current = true
    setSaving(true)
    setSaveError('')
    try {
      await cancelScheduleSession(session.id, item.rowVersion)
      saved.current = true
      cancelDialog.current?.close()
      navigate(`/schedule/${encodeURIComponent(session.id)}`, { replace: true })
    } catch (error) {
      cancelDialog.current?.close()
      setSaveError(error instanceof ApiError ? error.message : 'Termin nije moguće otkazati. Pokušajte ponovno.')
    } finally {
      inFlight.current = false
      setSaving(false)
    }
  }

  return <section className="schedule-edit-page" aria-labelledby="schedule-edit-title">
    <nav className="schedule-edit-breadcrumb" aria-label="Putanja"><Link to={`/schedule?date=${originalStart.date}`}>Raspored</Link><span>›</span><Link to={`/schedule/${session.id}`}>Detalj termina</Link><span>›</span><strong>Uredi termin</strong></nav>
    <header className="schedule-edit-header"><div><h1 id="schedule-edit-title">3.4 Uredi termin</h1><p>Promijenite podatke ovog termina. Odaberite odnosi li se promjena samo na ovaj termin ili na buduću seriju.</p></div><div><Link to={`/schedule/${session.id}`}>Odustani</Link><button type="submit" form="schedule-edit-form" disabled={saving || conflictState === 'conflict'}>{saving ? 'Spremanje…' : 'Spremi promjene'}</button></div></header>
    {saveError && <div className="schedule-edit-error" role="alert">{saveError}<button type="button" onClick={() => { setLocationRevision(value => value + 1); setSaveError('') }}>Osvježi podatke</button></div>}

    <form id="schedule-edit-form" onSubmit={event => void submit(event)}>
      <fieldset disabled={saving}>
        <div className="schedule-edit-layout">
          <main>
            <EditCard step="1" title="Osnovni podaci" className="schedule-edit-basic">
              <span className="schedule-edit-label">Način rada</span>
              <div className="schedule-edit-mode"><label><input type="radio" checked={session.deliveryMode === 2} disabled readOnly /> Grupni sat</label><label><input type="radio" checked={session.deliveryMode === 1} disabled readOnly /> Individualni sat</label></div>
              <label className="schedule-edit-field"><span>{session.deliveryMode === 2 ? 'Grupa' : 'Učenik'}</span><select disabled value={session.groupId ?? session.studentId ?? ''}><option value={session.groupId ?? session.studentId ?? ''}>{session.contextName}</option></select></label>
              <p className="schedule-edit-context">Program: <strong>{session.programName ?? 'Nije postavljen'}</strong> · {session.schoolGrade}{session.deliveryMode === 2 ? ` · ${session.participants.length} učenika` : ''}</p>
              <label className="schedule-edit-field"><span>Naziv termina (opcionalno)</span><input maxLength={200} disabled={scope === 2} value={title} onChange={event => setTitle(event.target.value)} /><small>{title.length}/200{scope === 2 ? ' · naziv ostaje vezan uz ovaj termin' : ''}</small></label>
              <label className="schedule-edit-field"><span>Opis / napomena (opcionalno)</span><textarea rows={5} maxLength={2000} disabled={scope === 2} value={notes} onChange={event => setNotes(event.target.value)} /><small>{notes.length}/2000</small></label>
            </EditCard>

            <div className="schedule-edit-center-top">
              <EditCard step="2" title="Datum i vrijeme">
                <label className="schedule-edit-field"><span>Datum *</span><input type="date" required disabled={scope === 2} value={date} onChange={event => setDate(event.target.value)} /></label>
                <div className="schedule-edit-time-row"><label className="schedule-edit-field"><span>Vrijeme početka *</span><input type="time" required step={60} value={startsAt} onChange={event => setStartsAt(event.target.value)} /></label><label className="schedule-edit-field"><span>Vrijeme završetka *</span><input type="time" required step={60} value={endsAt} onChange={event => setEndsAt(event.target.value)} /></label></div>
                <p>Trajanje <strong>{duration > 0 ? `${duration} minuta` : '—'}</strong> · {session.timeZoneId}</p>
              </EditCard>
              <EditCard step="3" title="Lokacija">
                <div className="schedule-edit-location-types">{([['none', 'Bez lokacije'], ['physical', 'Učionica'], ['online', 'Online sat']] as const).map(([value, label]) => <label key={value}><input type="radio" name="edit-location" checked={locationMode === value} onChange={() => chooseLocationMode(value)} />{label}</label>)}</div>
                {locationMode === 'physical' && <><label className="schedule-edit-field"><span>Pretražite lokacije</span><input type="search" value={locationSearch} onChange={event => setLocationSearch(event.target.value)} /></label><label className="schedule-edit-field"><span>Učionica *</span><select required value={locationId} onChange={event => setLocationId(event.target.value)}><option value="">Odaberite lokaciju</option>{locationChoices.map(location => <option key={location.id} value={location.id}>{location.name}</option>)}</select></label></>}
                {locationMode === 'online' && <label className="schedule-edit-field"><span>HTTPS poveznica *</span><input type="url" required maxLength={2048} value={onlineUrl} onChange={event => setOnlineUrl(event.target.value)} /></label>}
                {locations?.error && locationMode === 'physical' && <p className="schedule-edit-inline-error">Lokacije nije moguće učitati. <button type="button" onClick={() => setLocationRevision(value => value + 1)}>Pokušaj ponovno</button></p>}
              </EditCard>
            </div>

            <EditCard step="4" title="Ponavljanje termina" className="schedule-edit-scope">
              {item.canEditFutureSeries ? <><p className="schedule-edit-info">Ovo je termin iz ponavljajućeg rasporeda. Odaberite opseg promjene vremena i lokacije.</p><label><input type="radio" name="scope" checked={scope === 1} onChange={() => chooseScope(1)} /><span><strong>Samo ovaj termin (jednokratna promjena)</strong><small>Promjena se odnosi samo na {formatDate(originalStart.date)}.</small></span></label><label><input type="radio" name="scope" checked={scope === 2} onChange={() => chooseScope(2)} /><span><strong>Svi budući termini ove serije</strong><small>Jednostavna promjena vremena/lokacije od ovog termina nadalje.</small></span></label></> : <p className="schedule-edit-info">Ovaj termin nije dio aktivne serije. Promjena se odnosi samo na njega.</p>}
              {session.groupId && <div className="schedule-edit-group-route"><strong>Promijenite strukturu redovitog rasporeda grupe</strong><small>Za promjenu dana ili dodavanje/uklanjanje tjednog termina koristite 2.9.</small><Link to={`/students/groups/${session.groupId}/edit`}>Otvori 2.9 Uredi grupu ↗</Link></div>}
            </EditCard>

            <EditCard step="5" title="Dodatne postavke" className="schedule-edit-future"><div><label>Boja termina<select disabled><option>Prema programu</option></select></label><label>Podsjetnik za učitelja<select disabled><option>Nije dostupno</option></select></label><label>Podsjetnik za učenike<select disabled><option>Nije dostupno</option></select></label></div><p>Boje i podsjetnici čekaju zaseban contract i ne spremaju lažno stanje.</p></EditCard>
          </main>

          <aside>
            <section className="schedule-edit-current ds-card"><h2>Trenutni termin</h2><div><span className="schedule-edit-current-avatar">▣</span><h3>{session.contextName}</h3><b>{session.deliveryMode === 2 ? 'Grupni sat' : 'Individualni sat'}</b></div><dl><Summary label="Datum" value={formatDate(originalStart.date)} /><Summary label="Vrijeme" value={`${originalStart.time}–${originalEnd.time} (${currentDuration} min)`} /><Summary label="Lokacija" value={currentLocation} /><Summary label="Program" value={session.programName ?? 'Nije postavljen'} /><Summary label="Razred" value={session.schoolGrade} />{session.deliveryMode === 2 && <Summary label="Učenika" value={`${session.participants.length} / ${session.capacity ?? '—'}`} />}</dl></section>
            <section className={`schedule-edit-conflict ds-card is-${conflictState}`}><h2>Mogući konflikti</h2>{conflictState === 'checking' && <p>◷ Provjera rasporeda…</p>}{conflictState === 'clear' && <p><strong>✓ Nema konflikata</strong><small>Termin se ne preklapa s drugim vašim terminima i lokacija je slobodna.</small></p>}{conflictState === 'conflict' && <p><strong>⚠ Konflikt termina</strong><small>Odabrano vrijeme ili lokacija preklapa se s drugim terminom.</small></p>}{conflictState === 'error' && <p><strong>Provjera nije dostupna</strong><small>Ispravite unos ili pokušajte ponovno; spremanje će uvijek ponoviti provjeru.</small></p>}</section>
            <section className="schedule-edit-actions ds-card"><h2>Akcije</h2><button type="button" onClick={() => cancelDialog.current?.showModal()}>⊘ Otkaži termin</button><Link to={`/schedule/new?duplicate=${session.id}`}>▣ Dupliciraj termin</Link></section>
            <section className="schedule-edit-note">ⓘ Za promjenu dana ili strukture redovitog rasporeda grupe koristite Uredi grupu (2.9).</section>
          </aside>
        </div>
      </fieldset>
    </form>

    <dialog ref={cancelDialog} className="schedule-edit-dialog"><h2>Otkazati termin?</h2><p>Termin se neće izbrisati. Dobit će status Otkazan i ostati u povijesti.</p><p>Slanje obavijesti nije dostupno dok notification contract nije zaključan.</p><div><button type="button" onClick={() => cancelDialog.current?.close()}>Zadrži termin</button><button type="button" className="is-danger" onClick={() => void confirmCancel()}>Otkaži termin</button></div></dialog>
    <GroupUnsavedGuard dirty={dirty} saving={saving} saved={saved} />
  </section>
}

function EditCard({ step, title, className = '', children }: { readonly step: string; readonly title: string; readonly className?: string; readonly children: ReactNode }) { return <section className={`schedule-edit-card ds-card ${className}`}><h2><span>{step}</span>{title}</h2>{children}</section> }
function Summary({ label, value }: { readonly label: string; readonly value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function EditState({ message, error = false, action }: { readonly message: string; readonly error?: boolean; readonly action?: () => void }) { return <div className={`schedule-edit-state${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'}><p>{message}</p>{action && <button onClick={action}>Pokušaj ponovno</button>}<Link to="/schedule">Povratak na raspored</Link></div> }
function minutes(value: string) { const [hours, minute] = value.split(':').map(Number); return hours * 60 + minute }
function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium' }).format(new Date(`${value}T12:00:00`)) }
function localParts(value: string, zone: string) { const date = new Date(value); const parts = new Intl.DateTimeFormat('en-CA', { timeZone: zone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(date); const get = (type: Intl.DateTimeFormatPartTypes) => parts.find(part => part.type === type)?.value ?? ''; return { date: `${get('year')}-${get('month')}-${get('day')}`, time: `${get('hour')}:${get('minute')}` } }
