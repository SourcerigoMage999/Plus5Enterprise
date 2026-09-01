import { getJson, postJson } from '../api/apiClient.ts'

export type StudentDeliveryMode = 'individual' | 'group'
export type StudentStatus = 'active' | 'on_hold' | 'inactive'

export interface StudentReference {
  readonly id: string
  readonly name: string
  readonly code: string | null
}

export interface StudentListItem {
  readonly id: string
  readonly firstName: string
  readonly lastName: string
  readonly nickname: string | null
  readonly schoolGrade: StudentReference
  readonly program: StudentReference | null
  readonly deliveryMode: StudentDeliveryMode | null
  readonly group: StudentReference | null
  readonly status: StudentStatus
  readonly lastSessionAtUtc: string | null
}

export interface PagedStudents {
  readonly items: readonly StudentListItem[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
  readonly totalPages: number
}

export interface StudentFilterOption {
  readonly id: string
  readonly name: string
  readonly code: string | null
}

export interface StudentProgramCount {
  readonly programId: string
  readonly name: string
  readonly studentCount: number
}

export interface StudentListOverview {
  readonly totalCount: number
  readonly activeCount: number
  readonly onHoldCount: number
  readonly inactiveCount: number
  readonly withoutProgramCount: number
  readonly programCounts: readonly StudentProgramCount[]
  readonly programs: readonly StudentFilterOption[]
  readonly schoolGrades: readonly StudentFilterOption[]
}

export interface StudentListFilters {
  readonly search: string
  readonly programId: string
  readonly deliveryMode: '' | '1' | '2'
  readonly status: '' | '1' | '2' | '3'
  readonly schoolGradeId: string
  readonly page: number
  readonly pageSize: number
}

export function getStudents(filters: StudentListFilters, signal?: AbortSignal) {
  const query = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(filters.pageSize),
  })

  if (filters.search) query.set('search', filters.search)
  if (filters.programId) query.set('programId', filters.programId)
  if (filters.deliveryMode) query.set('deliveryMode', filters.deliveryMode)
  if (filters.status) query.set('status', filters.status)
  if (filters.schoolGradeId) query.set('schoolGradeId', filters.schoolGradeId)

  return getJson<PagedStudents>(`/students?${query}`, signal)
}

export function getStudentOverview(signal?: AbortSignal) {
  return getJson<StudentListOverview>('/students/overview', signal)
}

export interface StudentCreateOptions {
  readonly schoolGrades: readonly StudentFilterOption[]
  readonly programs: readonly StudentFilterOption[]
  readonly groups: readonly StudentGroupCreateOption[]
}

export interface StudentGroupCreateOption {
  readonly id: string
  readonly name: string
  readonly programId: string
  readonly activeMemberCount: number
  readonly capacity: number
}

export interface StudentCreateInput {
  readonly firstName: string
  readonly lastName: string
  readonly schoolGradeId: string
  readonly schoolName: string | null
  readonly dateOfBirth: string | null
  readonly gender: string | null
  readonly email: string | null
  readonly phone: string | null
  readonly programId: string | null
  readonly deliveryMode: StudentDeliveryMode | null
  readonly groupId: string | null
  readonly status: StudentStatus
  readonly guardian: {
    readonly firstName: string
    readonly lastName: string
    readonly email: string | null
    readonly phone: string | null
  } | null
}

export function getStudentCreateOptions(programId?: string, signal?: AbortSignal) {
  const query = programId ? `?programId=${encodeURIComponent(programId)}` : ''
  return getJson<StudentCreateOptions>(`/students/create-options${query}`, signal)
}

export function createStudent(input: StudentCreateInput) {
  return postJson<{ readonly id: string }>('/students', input)
}
