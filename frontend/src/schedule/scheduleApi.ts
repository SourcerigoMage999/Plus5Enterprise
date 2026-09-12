import { useEffect, useState } from 'react'
import { getJson, postJson } from '../api/apiClient.ts'
import type { Group, Page } from '../groups/groupsApi.ts'
import type { PagedStudents } from '../students/studentsApi.ts'

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

export interface ScheduleParticipant {
  id: string
  firstName: string
  lastName: string
  schoolGradeId: string
  schoolGrade: string
  status: 1 | 2 | 3
}

export interface ScheduleSessionDetail {
  id: string
  deliveryMode: 1 | 2
  groupId: string | null
  studentId: string | null
  contextName: string
  programId: string | null
  programName: string | null
  schoolGradeId: string
  schoolGrade: string
  groupStatus: 1 | 2 | 3 | null
  capacity: number | null
  title: string | null
  notes: string | null
  startsAtUtc: string
  endsAtUtc: string
  timeZoneId: string
  locationId: string | null
  locationName: string | null
  online: boolean
  status: 1 | 2 | 3 | 4
  isSeriesOccurrence: boolean
  isSeriesException: boolean
  createdAtUtc: string
  updatedAtUtc: string
  cancelledAtUtc: string | null
  participants: ScheduleParticipant[]
}

export interface ScheduleLocation { id: string; name: string }

export interface ScheduleCreateInput {
  deliveryMode: 1 | 2
  contextId: string
  title: string | null
  notes: string | null
  date: string
  startsAt: string
  endsAt: string
  repeatWeekly: boolean
  endsOn: string | null
  locationId: string | null
  onlineMeetingUrl: string | null
}

export interface ScheduleCreateResult { id: string; sessionCount: number }

export function createScheduleSession(input: ScheduleCreateInput) {
  return postJson<ScheduleCreateResult>('/schedule', input)
}

export function useScheduleCreateContexts(mode: 1 | 2, search: string, revision: number) {
  const query = new URLSearchParams({ page: '1', pageSize: '25', status: '1' })
  if (search) query.set('search', search)
  const path = mode === 2 ? `/groups?${query}` : `/students?${query}`
  const [state, setState] = useState<{ path: string; revision: number; data?: Page<Group> | PagedStudents; error?: string }>()

  useEffect(() => {
    const controller = new AbortController()
    getJson<Page<Group> | PagedStudents>(path, controller.signal).then(data => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setState({ path, revision, error: error instanceof Error ? error.message : 'Grupe ili učenike nije moguće učitati.' })
    })
    return () => controller.abort()
  }, [path, revision])

  return state?.path === path && state.revision === revision ? state : undefined
}

export function useScheduleLocations(search: string, revision: number) {
  const query = new URLSearchParams({ page: '1' })
  if (search) query.set('search', search)
  const path = `/groups/create-locations?${query}`
  const [state, setState] = useState<{ path: string; revision: number; data?: Page<ScheduleLocation>; error?: string }>()

  useEffect(() => {
    const controller = new AbortController()
    getJson<Page<ScheduleLocation>>(path, controller.signal).then(data => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setState({ path, revision, error: error instanceof Error ? error.message : 'Lokacije nije moguće učitati.' })
    })
    return () => controller.abort()
  }, [path, revision])

  return state?.path === path && state.revision === revision ? state : undefined
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

export function useScheduleSessionDetail(sessionId: string, revision: number, enabled = true) {
  const path = `/schedule/${encodeURIComponent(sessionId)}`
  const [state, setState] = useState<{ path: string; revision: number; data?: ScheduleSessionDetail; error?: string; status?: number }>()

  useEffect(() => {
    if (!enabled) return
    const controller = new AbortController()
    getJson<ScheduleSessionDetail>(path, controller.signal).then((data) => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setState({
        path,
        revision,
        error: error instanceof Error ? error.message : 'Detalj termina nije moguće učitati.',
        status: typeof error === 'object' && error !== null && 'status' in error ? Number(error.status) : undefined,
      })
    })
    return () => controller.abort()
  }, [enabled, path, revision])

  return enabled && state?.path === path && state.revision === revision ? state : undefined
}
