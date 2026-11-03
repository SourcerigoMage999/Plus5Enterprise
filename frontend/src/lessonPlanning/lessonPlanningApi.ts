import { getJson, postJson, putJson } from '../api/apiClient.ts'

export type ParticipantType = 'student' | 'group'

export interface Page<T> {
  readonly items: T[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
  readonly totalPages: number
}

export interface LessonParticipant {
  readonly id: string
  readonly name: string
  readonly schoolGrade: string
  readonly programName: string | null
  readonly memberCount: number | null
}

export interface LessonSession {
  readonly id: string
  readonly startsAtUtc: string
  readonly endsAtUtc: string
  readonly timeZoneId: string
  readonly title: string | null
}

export interface LessonKnowledgeComponent {
  readonly id: string
  readonly name: string
  readonly areaName: string
  readonly modelCode: string
  readonly modelVersion: string
}

export interface CreatedLessonGoal {
  readonly lessonPlanId: string
  readonly lessonPlanVersionId: string
}

export function getLessonParticipants(type: ParticipantType, search: string, signal?: AbortSignal) {
  const query = new URLSearchParams({ type, page: '1', pageSize: '100' })
  if (search.trim()) query.set('search', search.trim())
  return getJson<Page<LessonParticipant>>(`/lesson-plans/participants?${query}`, signal)
}

export function getLessonSessions(type: ParticipantType, participantId: string, signal?: AbortSignal) {
  const query = new URLSearchParams({ type, participantId })
  return getJson<LessonSession[]>(`/lesson-plans/sessions?${query}`, signal)
}

export function getLessonKnowledgeComponents(search: string, signal?: AbortSignal) {
  const query = new URLSearchParams({ page: '1', pageSize: '100' })
  if (search.trim()) query.set('search', search.trim())
  return getJson<Page<LessonKnowledgeComponent>>(`/lesson-plans/knowledge-components?${query}`, signal)
}

export function createLessonGoal(input: {
  readonly participantType: ParticipantType
  readonly participantId: string
  readonly sessionId: string | null
  readonly goal: string
  readonly knowledgeComponentIds: string[]
}) {
  return postJson<CreatedLessonGoal>('/lesson-plans/', input)
}

export type LessonBalanceStatus = 'Satisfied' | 'Warning' | 'NotEvaluated' | 1 | 2 | 3

export interface LessonStructureKnowledge {
  readonly id: string
  readonly name: string
  readonly modelCode: string
  readonly modelVersion: string
}

export interface LessonStructureActivity {
  readonly id: string
  readonly sortOrder: number
  readonly name: string
  readonly purpose: string
  readonly durationMinutes: number
  readonly knowledgeBlockCode: string | null
  readonly knowledgeBlockName: string | null
  readonly knowledgeComponentIds: string[]
  readonly warningCodes: string[]
}

export interface LessonBalanceArea {
  readonly code: string
  readonly label: string
  readonly status: LessonBalanceStatus
  readonly message: string
}

export interface LessonStructure {
  readonly lessonPlanId: string
  readonly lessonPlanVersionId: string
  readonly rowVersion: string
  readonly participantName: string
  readonly participantType: ParticipantType
  readonly goal: string
  readonly targetDurationMinutes: number
  readonly knowledgeComponents: LessonStructureKnowledge[]
  readonly activities: LessonStructureActivity[]
  readonly balance: LessonBalanceArea[]
  readonly materialCandidateCount: number
  readonly isApplied: boolean
  readonly canApply: boolean
  readonly policyCode: string
}

export function getLessonStructure(lessonPlanId: string, signal?: AbortSignal) {
  return getJson<LessonStructure>(`/lesson-plans/${lessonPlanId}/structure/`, signal)
}

export function changeLessonTargetDuration(lessonPlanId: string, targetDurationMinutes: number, rowVersion: string) {
  return putJson<LessonStructure>(`/lesson-plans/${lessonPlanId}/structure/target-duration`, { targetDurationMinutes, rowVersion })
}

export function applyLessonBaseline(lessonPlanId: string, rowVersion: string) {
  return postJson<LessonStructure>(`/lesson-plans/${lessonPlanId}/structure/apply`, { rowVersion })
}
