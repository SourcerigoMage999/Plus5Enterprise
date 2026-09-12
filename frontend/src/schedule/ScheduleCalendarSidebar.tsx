import { useState } from 'react'
import { addDays, formatDayHeader, monthGrid, monthLabel, sessionLocal, shiftMonth } from './calendarDate.ts'
import type { ScheduleCalendar } from './scheduleApi.ts'
import { scheduleTone } from './scheduleTone.ts'

export function ScheduleCalendarSidebar({
  calendar,
  anchor,
  groupId,
  programId,
  locationId,
  onDate,
  onFilter,
}: {
  readonly calendar: ScheduleCalendar
  readonly anchor: string
  readonly groupId: string
  readonly programId: string
  readonly locationId: string
  readonly onDate: (date: string) => void
  readonly onFilter: (key: 'groupId' | 'programId' | 'locationId', value: string) => void
}) {
  const [month, setMonth] = useState(anchor)
  const monthNumber = Number(month.slice(5, 7))
  const today = new Intl.DateTimeFormat('sv-SE', { timeZone: calendar.timeZoneId }).format(new Date())
  return <aside className="schedule-side" aria-label="Navigacija i sažetak rasporeda">
    <section className="ds-card schedule-mini"><header><h2>{monthLabel(month)}</h2><div><button type="button" aria-label="Prethodni mjesec" onClick={() => setMonth(value => shiftMonth(value, -1))}>‹</button><button type="button" aria-label="Sljedeći mjesec" onClick={() => setMonth(value => shiftMonth(value, 1))}>›</button></div></header>
      <div className="schedule-mini-weekdays" aria-hidden="true">{['PON', 'UTO', 'SRI', 'ČET', 'PET', 'SUB', 'NED'].map(day => <span key={day}>{day}</span>)}</div>
      <div className="schedule-mini-days">{monthGrid(month).map(day => {
        const number = formatDayHeader(day).date.split('.')[0]
        const outside = Number(day.slice(5, 7)) !== monthNumber
        return <button key={day} type="button" className={`${outside ? 'outside ' : ''}${day === anchor ? 'selected ' : ''}${day === today ? 'today' : ''}`} aria-label={day} aria-current={day === anchor ? 'date' : undefined} onClick={() => { setMonth(day); onDate(day) }}>{number}</button>
      })}</div>
    </section>

    <section className="ds-card schedule-filters"><h2>Filteri</h2>
      <Filter label="Grupa" value={groupId} options={calendar.groups} all="Sve grupe" onChange={value => onFilter('groupId', value)} />
      <Filter label="Program" value={programId} options={calendar.programs} all="Svi programi" onChange={value => onFilter('programId', value)} />
      <Filter label="Učionica" value={locationId} options={calendar.locations} all="Sve učionice" onChange={value => onFilter('locationId', value)} />
      <label className="schedule-mine"><input type="checkbox" checked disabled readOnly /><span>Prikaži samo moje termine</span></label>
    </section>

    <section className="ds-card schedule-summary"><h2>Sažetak razdoblja</h2><dl>
      <Row tone="group" label="Grupni termini" value={calendar.summary.groupSessions} />
      <Row tone="individual" label="Individualni termini" value={calendar.summary.individualSessions} />
      <Row tone="total" label="Ukupno termina" value={calendar.summary.totalSessions} />
      <Row tone="students" label="Jedinstvenih učenika" value={calendar.summary.uniqueStudents} />
      <Row tone="visits" label="Planiranih dolazaka" value={calendar.summary.plannedAttendances} />
      <Row tone="seats" label="Slobodnih mjesta" value={calendar.summary.availableSeats} />
    </dl><button type="button" disabled title="Detaljni izvještaji dolaze u Phase 14.">Pogledaj detaljan izvještaj <span aria-hidden="true">→</span></button></section>

    <section className="ds-card schedule-reminders"><h2>Podsjetnici</h2>{calendar.reminders.length ? <ul>{calendar.reminders.map(item => {
      const local = sessionLocal(item.startsAtUtc, calendar.timeZoneId)
      const when = local.date === today ? `Danas u ${local.time}` : local.date === addDays(today, 1) ? `Sutra u ${local.time}` : `${formatDayHeader(local.date).date} u ${local.time}`
      return <li key={item.id}><span className={`schedule-reminder-icon schedule-event--${scheduleTone(item)}`} aria-hidden="true">▣</span><button type="button" disabled title="Detalj termina dolazi u Phase 4.2."><small>{when}</small><strong>{item.contextName}</strong></button></li>
    })}</ul> : <p>Nema nadolazećih termina.</p>}<button type="button" disabled title="Centar podsjetnika ovisi o budućem notification contractu.">Pogledaj sve podsjetnike <span aria-hidden="true">→</span></button></section>
    <p className="schedule-zone">◷ Vrijeme prikaza: {calendar.timeZoneId}</p>
  </aside>
}

function Filter({ label, value, options, all, onChange }: { readonly label: string; readonly value: string; readonly options: ScheduleCalendar['groups']; readonly all: string; readonly onChange: (value: string) => void }) {
  return <label><span className="sr-only">{label}</span><select aria-label={label} value={value} onChange={event => onChange(event.target.value)}><option value="">{all}</option>{options.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}</select></label>
}

function Row({ tone, label, value }: { readonly tone: string; readonly label: string; readonly value: number }) {
  return <div><dt><span className={`schedule-dot schedule-dot--${tone}`} aria-hidden="true" />{label}</dt><dd>{value}</dd></div>
}
