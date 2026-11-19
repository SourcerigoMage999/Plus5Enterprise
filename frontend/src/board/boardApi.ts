import { getJson, postJson } from '../api/apiClient.ts'

export interface LessonSessionActivity {
  readonly id: string
  readonly sourceLessonActivityId: string | null
  readonly sortOrder: number
  readonly origin: 'Planned' | 'AdHoc'
  readonly status: 'Pending' | 'InProgress' | 'Paused' | 'Completed' | 'Partial' | 'Skipped'
  readonly name: string
  readonly purpose: string
  readonly durationMinutes: number
  readonly workMode: string
  readonly description: string | null
  readonly studentInstructions: string | null
  readonly showStudentInstructionsOnBoard: boolean
  readonly showProgressOnBoard: boolean
  readonly materialVersionIds: string[]
  readonly assessableTaskVersionIds: string[]
}

export interface LessonSession {
  readonly id: string
  readonly lessonPlanId: string
  readonly lessonPlanVersionId: string
  readonly lessonPlanVersionNumber: number
  readonly calendarSessionId: string | null
  readonly participantType: 'student' | 'group'
  readonly participantName: string
  readonly goal: string
  readonly status: 'InProgress' | 'Completed' | 'Voided'
  readonly startedAtUtc: string
  readonly scheduledStartsAtUtc: string | null
  readonly scheduledEndsAtUtc: string | null
  readonly scheduledTimeZoneId: string | null
  readonly lastCommittedAtUtc: string
  readonly revision: number
  readonly rowVersion: string
  readonly activities: LessonSessionActivity[]
}

export interface ActiveLessonSession {
  readonly id: string
  readonly lessonPlanId: string
  readonly lessonPlanVersionId: string
  readonly calendarSessionId: string | null
  readonly participantType: 'student' | 'group'
  readonly participantName: string
  readonly goal: string
  readonly startedAtUtc: string
  readonly lastCommittedAtUtc: string
  readonly revision: number
  readonly activityCount: number
}

export function startLessonSession(lessonPlanId: string, lessonPlanVersionId: string,
  calendarSessionId: string | null, startOperationId: string) {
  return postJson<LessonSession>(`/lesson-plans/${lessonPlanId}/lesson-sessions/start`, {
    lessonPlanVersionId, calendarSessionId, startOperationId,
  })
}

export function getActiveLessonSessions(signal?: AbortSignal) {
  return getJson<ActiveLessonSession[]>('/lesson-sessions/active', signal)
}

export function getLessonSession(id: string, signal?: AbortSignal) {
  return getJson<LessonSession>(`/lesson-sessions/${id}`, signal)
}

export function reorderLessonSessionTimeline(id: string, expectedRevision: number,
  activityIds: string[], operationId: string) {
  return postJson<LessonSession>(`/lesson-sessions/${id}/mutations`, {
    type: 'ReorderTimeline', operationId, expectedRevision, activityIds,
  })
}
