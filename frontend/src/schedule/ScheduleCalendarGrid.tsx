import type { CSSProperties } from 'react'
import { formatDayHeader, rangeDays, sessionLocal, type CalendarView } from './calendarDate.ts'
import type { ScheduleItem } from './scheduleApi.ts'
import { scheduleTone } from './scheduleTone.ts'

export function ScheduleCalendarGrid({
  from,
  to,
  view,
  items,
  timeZone,
}: {
  readonly from: string
  readonly to: string
  readonly view: CalendarView
  readonly items: ScheduleItem[]
  readonly timeZone: string
}) {
  const days = rangeDays(from, to)
  const displayed = items.map(item => {
    const start = sessionLocal(item.startsAtUtc, timeZone)
    const end = sessionLocal(item.endsAtUtc, timeZone)
    return { ...item, localDate: start.date, localStart: start.time, localEnd: end.time, startMinutes: start.minutes, endMinutes: end.minutes }
  })
  const startHour = Math.max(0, Math.min(8, ...displayed.map(item => Math.floor(item.startMinutes / 60))))
  const endHour = Math.min(24, Math.max(20, ...displayed.map(item => Math.ceil(item.endMinutes / 60))))
  const hourHeight = 56
  const height = (endHour - startHour) * hourHeight
  const hours = Array.from({ length: endHour - startHour + 1 }, (_, index) => startHour + index)
  const columns = view === 'week' ? days.length : 1
  const gridStyle = { '--schedule-columns': columns, '--schedule-height': `${height}px`, '--schedule-hour-height': `${hourHeight}px` } as CSSProperties

  return <div className="schedule-calendar-scroll" role="region" aria-label="Kalendar termina — vodoravno pomičan" tabIndex={0}>
    <div className={`schedule-calendar schedule-calendar--${view}`} style={gridStyle}>
      <div className="schedule-calendar-head"><span aria-hidden="true" />{days.map(day => { const label = formatDayHeader(day); return <div key={day} className="schedule-day-heading"><strong>{label.day}</strong><span>{label.date}</span></div> })}</div>
      <div className="schedule-calendar-body" style={{ height }}>
        <div className="schedule-time-axis">{hours.map(hour => <span key={hour} style={{ top: (hour - startHour) * hourHeight }}>{String(hour).padStart(2, '0')}:00</span>)}</div>
        <div className="schedule-day-lanes">{days.map(day => <div className="schedule-day-lane" key={day}>
          {displayed.filter(item => item.localDate === day).map(item => {
            const top = Math.max(0, (item.startMinutes - startHour * 60) / 60 * hourHeight)
            const duration = Math.max(30, item.endMinutes - item.startMinutes)
            const style = { top, height: Math.max(34, duration / 60 * hourHeight - 3) }
            return <button key={item.id} type="button" className={`schedule-event schedule-event--${scheduleTone(item)}`} style={style} disabled title="Detalj termina dolazi u Phase 4.2.">
              <strong>{item.deliveryMode === 2 ? item.contextName : 'Individualno'}</strong>
              <span>{item.localStart} – {item.localEnd}</span>
              <small>{item.deliveryMode === 1 ? item.contextName : item.locationName ?? (item.online ? 'Online' : 'Bez lokacije')}</small>
              {item.deliveryMode === 2 && <small>{item.memberCount}/{item.capacity} učenika</small>}
            </button>
          })}
        </div>)}</div>
        {!items.length && <p className="schedule-empty">Nema termina za odabrano razdoblje i filtre.</p>}
      </div>
    </div>
  </div>
}
