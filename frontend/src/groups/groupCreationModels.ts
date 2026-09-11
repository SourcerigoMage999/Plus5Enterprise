export interface ScheduleSlot { key: string; dayOfWeek: number; start: string; end: string }
export interface GroupSchedule { startsOn: string; endsOn: string; locationId: string; locationName: string; slots: ScheduleSlot[] }
export const emptySchedule: GroupSchedule = { startsOn: '', endsOn: '', locationId: '', locationName: '', slots: [] }
export const dayNames = ['Nedjelja', 'Ponedjeljak', 'Utorak', 'Srijeda', 'Četvrtak', 'Petak', 'Subota']
