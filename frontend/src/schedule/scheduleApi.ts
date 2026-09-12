import { useEffect, useState } from 'react'
import { getJson } from '../api/apiClient.ts'

export interface ScheduleOption { id: string; name: string }

export interface ScheduleItem {
  id: string
  deliveryMode: 1 | 2
  groupId: string | null
  studentId: string | null
  contextName: string
  programId: string | null
  programName: string | null
  startsAtUtc: string
  endsAtUtc: string
  timeZoneId: string
  locationId: string | null
  locationName: string | null
  online: boolean
  status: 1 | 2 | 3
  memberCount: number
  capacity: number | null
}

export interface ScheduleSummary {
  groupSessions: number
  individualSessions: number
  totalSessions: number
  uniqueStudents: number
  plannedAttendances: number
  availableSeats: number
}

export interface ScheduleCalendar {
  timeZoneId: string
  from: string
  to: string
  items: ScheduleItem[]
  reminders: ScheduleItem[]
  summary: ScheduleSummary
  groups: ScheduleOption[]
  programs: ScheduleOption[]
  locations: ScheduleOption[]
}

export function useScheduleCalendar(path: string, revision: number) {
  const [state, setState] = useState<{ path: string; revision: number; data?: ScheduleCalendar; error?: string }>()

  useEffect(() => {
    const controller = new AbortController()
    getJson<ScheduleCalendar>(path, controller.signal).then((data) => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setState({
        path,
        revision,
        error: error instanceof Error ? error.message : 'Raspored nije moguće učitati.',
      })
    })
    return () => controller.abort()
  }, [path, revision])

  return state?.path === path && state.revision === revision ? state : undefined
}
