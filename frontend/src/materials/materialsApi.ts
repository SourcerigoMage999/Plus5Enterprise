import { getJson } from '../api/apiClient.ts'

export type MaterialOwnership = 'mine' | 'shared'
export type MaterialSort = 'newest' | 'oldest' | 'title'
export type MaterialFileFormat = 'pdf' | 'docx' | 'pptx' | 'mp4' | 'zip'

export interface MaterialReference {
  readonly id: string
  readonly name: string
  readonly code: string | null
}

export interface MaterialLibraryItem {
  readonly id: string
  readonly versionId: string
  readonly title: string
  readonly description: string | null
  readonly materialTypeCode: string
  readonly subject: string | null
  readonly program: MaterialReference | null
  readonly schoolGrade: MaterialReference | null
  readonly proficiencyLevel: MaterialReference | null
  readonly fileFormat: MaterialFileFormat
  readonly addedAtUtc: string
  readonly isOwner: boolean
  readonly shareAccess: 'view' | 'use' | null
  readonly tags: readonly string[]
}

export interface PagedMaterials {
  readonly items: readonly MaterialLibraryItem[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
  readonly totalPages: number
}

export interface MaterialTypeCount {
  readonly code: string
  readonly count: number
}

export interface MaterialRecentItem {
  readonly id: string
  readonly title: string
  readonly addedAtUtc: string
}

export interface MaterialLibraryOverview {
  readonly subjects: readonly string[]
  readonly programs: readonly MaterialReference[]
  readonly schoolGrades: readonly MaterialReference[]
  readonly materialTypes: readonly string[]
  readonly tags: readonly string[]
  readonly materialTypeCounts: readonly MaterialTypeCount[]
  readonly recentlyAdded: readonly MaterialRecentItem[]
}

export interface MaterialLibraryFilters {
  readonly page: number
  readonly pageSize: number
  readonly ownership: MaterialOwnership
  readonly sort: MaterialSort
  readonly search: string
  readonly subject: string
  readonly programId: string
  readonly schoolGradeId: string
  readonly materialTypeCode: string
  readonly tag: string
}

export interface MaterialDetailFile {
  readonly format: MaterialFileFormat
  readonly originalFileName: string
  readonly mediaType: string
  readonly sizeBytes: number
}

export interface MaterialDetailKnowledgeComponent {
  readonly id: string
  readonly name: string
  readonly knowledgeAreaName: string
  readonly knowledgeModelCode: string
  readonly knowledgeModelVersion: string
  readonly knowledgeModelStatus: 'published' | 'retired'
}

export interface MaterialDetailCurriculumOutcome {
  readonly id: string
  readonly officialCode: string | null
  readonly title: string
  readonly curriculumCode: string
  readonly curriculumName: string
  readonly curriculumVersion: string
}

export interface MaterialDetail {
  readonly id: string
  readonly versionId: string
  readonly versionNumber: number
  readonly title: string
  readonly description: string | null
  readonly materialTypeCode: string
  readonly subject: string | null
  readonly languageCode: string | null
  readonly program: MaterialReference | null
  readonly schoolGrade: MaterialReference | null
  readonly proficiencyLevel: MaterialReference | null
  readonly learningGoal: string | null
  readonly file: MaterialDetailFile
  readonly addedAtUtc: string
  readonly isOwner: boolean
  readonly shareAccess: 'view' | 'use' | null
  readonly tags: readonly string[]
  readonly knowledgeComponents: readonly MaterialDetailKnowledgeComponent[]
  readonly curriculumOutcomes: readonly MaterialDetailCurriculumOutcome[]
}

export function getMaterials(filters: MaterialLibraryFilters, signal?: AbortSignal) {
  const query = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(filters.pageSize),
    ownership: filters.ownership === 'mine' ? '1' : '2',
    sort: filters.sort === 'newest' ? '1' : filters.sort === 'oldest' ? '2' : '3',
  })

  if (filters.search) query.set('search', filters.search)
  if (filters.subject) query.set('subject', filters.subject)
  if (filters.programId) query.set('programId', filters.programId)
  if (filters.schoolGradeId) query.set('schoolGradeId', filters.schoolGradeId)
  if (filters.materialTypeCode) query.set('materialTypeCode', filters.materialTypeCode)
  if (filters.tag) query.set('tag', filters.tag)

  return getJson<PagedMaterials>(`/materials?${query}`, signal)
}

export function getMaterialOverview(ownership: MaterialOwnership, signal?: AbortSignal) {
  const ownershipValue = ownership === 'mine' ? 1 : 2
  return getJson<MaterialLibraryOverview>(`/materials/overview?ownership=${ownershipValue}`, signal)
}

export function getMaterial(materialId: string, signal?: AbortSignal) {
  return getJson<MaterialDetail>(`/materials/${encodeURIComponent(materialId)}`, signal)
}
