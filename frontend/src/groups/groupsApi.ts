import { getJson, postJson } from '../api/apiClient.ts'
import { useEffect, useState } from 'react'

export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export interface Slot { dayOfWeek: number; start: string; end: string; timeZoneId: string; location: string | null; online: boolean }
export interface Group {
  id: string; name: string; programId: string; programName: string; schoolGradeId: string; schoolGrade: string
  status: 'active' | 'on_hold' | 'inactive'; capacity: number; memberCount: number; rowVersion: string; slots: Slot[]
}
export interface Overview { totalGroups: number; activeGroups: number; students: number; availableSeats: number; sessionsThisWeek: number; weekStartsOn: string }
export interface GroupStudent { id: string; firstName: string; lastName: string; schoolGrade: string; recommended: boolean; rowVersion: string }
export interface GroupSession { id: string; startsAtUtc: string; endsAtUtc: string; timeZoneId: string; location: string | null; online: boolean; status: number }

export function changeMembership(group: Group, student: GroupStudent, join: boolean) {
  return postJson<void>(`/groups/${group.id}/members/${student.id}`, {
    join, groupRowVersion: group.rowVersion, studentRowVersion: student.rowVersion,
  })
}

export function useGroupResource<T>(path: string, revision = 0) {
  const [state, setState] = useState<{ path: string; revision: number; data?: T; error?: string }>()
  useEffect(() => {
    const controller = new AbortController()
    getJson<T>(path, controller.signal).then((data) => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setState({ path, revision, error: error instanceof Error ? error.message : 'Podatke nije moguće učitati.' })
    })
    return () => controller.abort()
  }, [path, revision])
  return state?.path === path && state.revision === revision ? state : undefined
}
