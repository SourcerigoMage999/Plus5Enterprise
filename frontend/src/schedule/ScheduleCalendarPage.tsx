import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { calendarRange, formatRange, todayInZone, validDateKey, addDays, type CalendarView } from './calendarDate.ts'
import { ScheduleCalendarGrid } from './ScheduleCalendarGrid.tsx'
import { ScheduleCalendarSidebar } from './ScheduleCalendarSidebar.tsx'
import { useScheduleCalendar } from './scheduleApi.ts'
import { programTone } from './scheduleTone.ts'
import './ScheduleCalendarPage.css'

const timeZone = 'Europe/Zagreb'

export function ScheduleCalendarPage() {
  const [params, setParams] = useSearchParams()
  const [revision, setRevision] = useState(0)
  const view: CalendarView = params.get('view') === 'day' ? 'day' : 'week'
  const requestedDate = params.get('date')
  const anchor = validDateKey(requestedDate) ? requestedDate : todayInZone(timeZone)
  const groupId = params.get('groupId') ?? ''
  const programId = params.get('programId') ?? ''
  const locationId = params.get('locationId') ?? ''
  const range = calendarRange(anchor, view)
  const apiPath = useMemo(() => {
    const query = new URLSearchParams({ from: range.from, to: range.to })
    if (groupId) query.set('groupId', groupId)
    if (programId) query.set('programId', programId)
    if (locationId) query.set('locationId', locationId)
    return `/schedule?${query.toString()}`
  }, [groupId, locationId, programId, range.from, range.to])
  const state = useScheduleCalendar(apiPath, revision)

  function update(values: Record<string, string>) {
    setParams(previous => {
      const next = new URLSearchParams(previous)
      Object.entries(values).forEach(([key, value]) => value ? next.set(key, value) : next.delete(key))
      return next
    }, { replace: true })
  }

  function setView(next: CalendarView) {
    update({ view: next === 'week' ? '' : next, date: anchor })
  }

  function move(amount: number) {
    update({ date: addDays(anchor, amount) })
  }

  return <div className="schedule-page">
    <header className="schedule-page-header">
      <div><h1>3.1 Raspored</h1><p>Pregledajte raspored svih grupa i individualnih sati.</p></div>
      <div className="schedule-page-actions">
        <div className="schedule-view-toggle" role="group" aria-label="Način prikaza">
          <button type="button" aria-pressed={view === 'week'} onClick={() => setView('week')}>Tjedan</button>
          <button type="button" aria-pressed={view === 'day'} onClick={() => setView('day')}>Dan</button>
        </div>
        <button type="button" className="schedule-today" onClick={() => update({ date: todayInZone(timeZone) })}>Danas</button>
        <button type="button" className="schedule-new" disabled title="Novi termin dolazi u Phase 4.3.">+ Novi termin</button>
      </div>
    </header>

    {!state && <ScheduleState message="Učitavanje rasporeda…" />}
    {state?.error && <ScheduleState error message="Raspored trenutno nije moguće učitati." action={() => setRevision(value => value + 1)} />}
    {state?.data && <>
      <div className="schedule-layout">
        <section className="ds-card schedule-board" aria-label="Raspored termina">
          <header className="schedule-board-header">
            <div className="schedule-period-navigation">
              <button type="button" aria-label={view === 'week' ? 'Prethodni tjedan' : 'Prethodni dan'} onClick={() => move(view === 'week' ? -7 : -1)}>‹</button>
              <button type="button" aria-label={view === 'week' ? 'Sljedeći tjedan' : 'Sljedeći dan'} onClick={() => move(view === 'week' ? 7 : 1)}>›</button>
            </div>
            <h2>{formatRange(range.from, range.to, view)}</h2>
            <span>{view === 'week' ? 'Tjedni prikaz' : 'Dnevni prikaz'}</span>
          </header>
          <ScheduleCalendarGrid from={range.from} to={range.to} view={view} items={state.data.items} timeZone={state.data.timeZoneId} />
          <footer className="schedule-legend" aria-label="Legenda programa">
            <strong>Legenda:</strong>
            {state.data.programs.map(program => <span key={program.id}><i className={`schedule-event--${programTone(program.id)}`} aria-hidden="true" />{program.name}</span>)}
            <span><i className="schedule-event--individual" aria-hidden="true" />Individualno</span>
          </footer>
        </section>
        <ScheduleCalendarSidebar
          key={anchor}
          calendar={state.data}
          anchor={anchor}
          groupId={groupId}
          programId={programId}
          locationId={locationId}
          onDate={date => update({ date })}
          onFilter={(key, value) => update({ [key]: value })}
        />
      </div>
      <p className="schedule-info"><span aria-hidden="true">ⓘ</span> Raspored se automatski ažurira nakon promjena termina ili rasporeda grupe.</p>
    </>}
  </div>
}

function ScheduleState({ message, error = false, action }: { readonly message: string; readonly error?: boolean; readonly action?: () => void }) {
  return <div className={`ds-card schedule-state${error ? ' schedule-state--error' : ''}`} role={error ? 'alert' : 'status'}><p>{message}</p>{action && <button type="button" onClick={action}>Pokušaj ponovno</button>}</div>
}
