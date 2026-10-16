import { deleteJson, getJson, postFormData, postJson, putJson } from '../api/apiClient.ts'
import { environment } from '../config/environment.ts'

export type PresentationElementType = 'TEXT' | 'IMAGE' | 'SHAPE' | 'TABLE'

export interface PresentationAsset {
  readonly id: string
  readonly originalFileName: string
  readonly mediaType: string
  readonly sizeBytes: number
  readonly readPath: string
}

export interface PresentationElement {
  readonly id: string
  readonly elementTypeCode: PresentationElementType
  readonly elementSchemaVersion: number
  readonly zIndex: number
  readonly x: number
  readonly y: number
  readonly width: number
  readonly height: number
  readonly payloadJson: string
  readonly asset: PresentationAsset | null
}

export interface PresentationSlide {
  readonly id: string
  readonly sortOrder: number
  readonly elements: readonly PresentationElement[]
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
  readonly elementId: string | null
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

export function createPresentationElement(
  materialId: string,
  slideId: string,
  body: {
    readonly expectedDocumentRowVersion: string
    readonly elementTypeCode: Exclude<PresentationElementType, 'IMAGE'>
    readonly x: number
    readonly y: number
    readonly width: number
    readonly height: number
    readonly payloadJson: string
  },
): Promise<PresentationMutationResponse> {
  return postJson(`${path(materialId)}/slides/${slideId}/elements`, body)
}

export function updatePresentationElement(
  materialId: string,
  slideId: string,
  elementId: string,
  body: {
    readonly expectedDocumentRowVersion: string
    readonly zIndex: number
    readonly x: number
    readonly y: number
    readonly width: number
    readonly height: number
    readonly payloadJson: string
  },
): Promise<PresentationMutationResponse> {
  return putJson(`${path(materialId)}/slides/${slideId}/elements/${elementId}`, body)
}

export function deletePresentationElement(
  materialId: string,
  slideId: string,
  elementId: string,
  expectedDocumentRowVersion: string,
): Promise<PresentationMutationResponse> {
  return deleteJson(`${path(materialId)}/slides/${slideId}/elements/${elementId}`, {
    expectedDocumentRowVersion,
  })
}

export function uploadPresentationImage(
  materialId: string,
  slideId: string,
  file: File,
  expectedDocumentRowVersion: string,
): Promise<PresentationMutationResponse> {
  const form = new FormData()
  form.append('file', file)
  form.append('expectedDocumentRowVersion', expectedDocumentRowVersion)
  form.append('x', '570')
  form.append('y', '260')
  form.append('width', '460')
  form.append('height', '380')
  form.append('altText', file.name.replace(/\.[^.]+$/, ''))
  form.append('fit', 'contain')
  return postFormData(`${path(materialId)}/slides/${slideId}/images`, form)
}

export function presentationAssetUrl(asset: PresentationAsset): string {
  return `${environment.apiBaseUrl}${asset.readPath}`
}
