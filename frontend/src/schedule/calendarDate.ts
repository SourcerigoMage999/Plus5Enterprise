export type CalendarView = 'week' | 'day'

const isoDate = /^\d{4}-\d{2}-\d{2}$/

export function validDateKey(value: string | null): value is string {
  if (!value || !isoDate.test(value)) return false
  const parsed = new Date(`${value}T12:00:00Z`)
  return !Number.isNaN(parsed.valueOf()) && dateKey(parsed) === value
}

export function todayInZone(timeZone = 'Europe/Zagreb') {
  return new Intl.DateTimeFormat('sv-SE', { timeZone }).format(new Date())
}

export function addDays(value: string, days: number) {
  const date = new Date(`${value}T12:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return dateKey(date)
}

export function startOfWeek(value: string) {
  const date = new Date(`${value}T12:00:00Z`)
  const day = date.getUTCDay()
  return addDays(value, -(day === 0 ? 6 : day - 1))
}

export function calendarRange(value: string, view: CalendarView) {
  const from = view === 'week' ? startOfWeek(value) : value
  return { from, to: addDays(from, view === 'week' ? 7 : 1) }
}

export function rangeDays(from: string, to: string) {
  const days: string[] = []
  for (let current = from; current < to; current = addDays(current, 1)) days.push(current)
  return days
}

export function formatRange(from: string, to: string, view: CalendarView) {
  if (view === 'day') return formatLongDate(from)
  const last = addDays(to, -1)
  const startDate = asDate(from)
  const endDate = asDate(last)
  const startMonth = startDate.getUTCMonth()
  const endMonth = endDate.getUTCMonth()
  if (startMonth === endMonth) {
    return `${startDate.getUTCDate()}. – ${new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }).format(endDate)}`
  }
  return `${new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'short', timeZone: 'UTC' }).format(startDate)} – ${new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(endDate)}`
}

export function formatLongDate(value: string) {
  return new Intl.DateTimeFormat('hr-HR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }).format(asDate(value))
}

export function formatDayHeader(value: string) {
  const date = asDate(value)
  return {
    day: new Intl.DateTimeFormat('hr-HR', { weekday: 'short', timeZone: 'UTC' }).format(date).replace('.', '').toUpperCase(),
    date: new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'numeric', timeZone: 'UTC' }).format(date),
  }
}

export function monthLabel(value: string) {
  const label = new Intl.DateTimeFormat('hr-HR', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(asDate(value))
  return `${label[0]?.toLocaleUpperCase('hr') ?? ''}${label.slice(1)}`
}

export function monthGrid(value: string) {
  const date = asDate(value)
  const first = `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}-01`
  return rangeDays(startOfWeek(first), addDays(startOfWeek(first), 42))
}

export function shiftMonth(value: string, amount: number) {
  const date = asDate(value)
  date.setUTCDate(1)
  date.setUTCMonth(date.getUTCMonth() + amount)
  return dateKey(date)
}

export function sessionLocal(iso: string, timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(new Date(iso))
  const read = (type: Intl.DateTimeFormatPartTypes) => parts.find(part => part.type === type)?.value ?? ''
  const hour = Number(read('hour'))
  const minute = Number(read('minute'))
  return {
    date: `${read('year')}-${read('month')}-${read('day')}`,
    time: `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`,
    minutes: hour * 60 + minute,
  }
}

function asDate(value: string) { return new Date(`${value}T12:00:00Z`) }
function dateKey(value: Date) { return value.toISOString().slice(0, 10) }
