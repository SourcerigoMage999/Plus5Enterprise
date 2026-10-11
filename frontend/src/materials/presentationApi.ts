import { deleteJson, getJson, postJson, putJson } from '../api/apiClient.ts'

export interface PresentationSlide {
  readonly id: string
  readonly sortOrder: number
}

export interface PresentationDocument {
  readonly documentSchemaVersion: number
  readonly canvasWidth: number
  readonly canvasHeight: number
  readonly rowVersion: string
  readonly updatedAtUtc: string
  readonly slides: readonly PresentationSlide[]
}

export interface PresentationEditorWorkspace {
  readonly materialId: string
  readonly materialVersionId: string
  readonly materialVersionNumber: number
  readonly title: string
  readonly materialRowVersion: string
  readonly document: PresentationDocument | null
}

interface PresentationMutationResponse {
  readonly slideId: string | null
}

const path = (materialId: string) => `/materials/${materialId}/presentation`

export function getPresentationEditor(
  materialId: string,
  signal?: AbortSignal,
): Promise<PresentationEditorWorkspace> {
  return getJson<PresentationEditorWorkspace>(path(materialId), signal)
}

export function initializePresentation(
  materialId: string,
  materialVersionId: string,
  expectedMaterialRowVersion: string,
): Promise<PresentationMutationResponse> {
  return postJson(path(materialId), { materialVersionId, expectedMaterialRowVersion })
}

export function addPresentationSlide(
  materialId: string,
  expectedDocumentRowVersion: string,
  afterSlideId: string | null,
): Promise<PresentationMutationResponse> {
  return postJson(`${path(materialId)}/slides`, {
    expectedDocumentRowVersion,
    afterSlideId,
  })
}

export function duplicatePresentationSlide(
  materialId: string,
  slideId: string,
  expectedDocumentRowVersion: string,
): Promise<PresentationMutationResponse> {
  return postJson(`${path(materialId)}/slides/${slideId}/duplicate`, {
    expectedDocumentRowVersion,
  })
}

export function deletePresentationSlide(
  materialId: string,
  slideId: string,
  expectedDocumentRowVersion: string,
): Promise<PresentationMutationResponse> {
  return deleteJson(`${path(materialId)}/slides/${slideId}`, {
    expectedDocumentRowVersion,
  })
}

export function reorderPresentationSlides(
  materialId: string,
  expectedDocumentRowVersion: string,
  orderedSlideIds: readonly string[],
): Promise<PresentationMutationResponse> {
  return putJson(`${path(materialId)}/slides/order`, {
    expectedDocumentRowVersion,
    orderedSlideIds,
  })
}
