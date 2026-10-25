import { Sparkles, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { ApiError } from '../api/apiClient.ts'
import {
  createPresentationAiSuggestion,
  getPresentationAiStatus,
  resolvePresentationAiSuggestion,
  type PresentationAiSuggestion,
  type PresentationAiSuggestionKind,
  type PresentationElement,
} from './presentationApi.ts'
import './PresentationAiPanel.css'

interface PresentationAiPanelProps {
  readonly materialId: string
  readonly documentRowVersion: string
  readonly selectedSlideId: string | null
  readonly selectedElement: PresentationElement | null
  readonly onClose: () => void
  readonly onApplied: () => void
}

export function PresentationAiPanel({
  materialId,
  documentRowVersion,
  selectedSlideId,
  selectedElement,
  onClose,
  onApplied,
}: PresentationAiPanelProps) {
  const [enabled, setEnabled] = useState<boolean | null>(null)
  const [kind, setKind] = useState<PresentationAiSuggestionKind>('ELEMENT_CONTENT')
  const [instruction, setInstruction] = useState('Predloži jasan i dobno primjeren sadržaj.')
  const [suggestion, setSuggestion] = useState<PresentationAiSuggestion | null>(null)
  const [editedJson, setEditedJson] = useState('')
  const [editing, setEditing] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    getPresentationAiStatus(materialId, controller.signal)
      .then(result => setEnabled(result.enabled))
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Status AI pomoći trenutačno nije moguće provjeriti.')
      })
    return () => controller.abort()
  }, [materialId])

  const canGenerate = useMemo(() => {
    if (!enabled || !selectedSlideId) return false
    if (kind === 'ELEMENT_CONTENT') {
      return !selectedElement || ['TEXT', 'RULE', 'EXAMPLE', 'VOCABULARY', 'QUESTION']
        .includes(selectedElement.elementTypeCode)
    }
    if (kind === 'KNOWLEDGE_MAPPING') return selectedElement?.elementTypeCode === 'QUICK_CHECK'
    return true
  }, [enabled, kind, selectedElement, selectedSlideId])

  async function generate() {
    if (!canGenerate || busy) return
    setBusy(true); setError(''); setSuggestion(null); setEditing(false)
    try {
      const result = await createPresentationAiSuggestion(materialId, {
        expectedDocumentRowVersion: documentRowVersion,
        suggestionKind: kind,
        instruction,
        targetSlideId: kind === 'PRESENTATION_DRAFT' ? null : selectedSlideId,
        targetElementId: kind === 'ELEMENT_CONTENT' || kind === 'KNOWLEDGE_MAPPING'
          ? selectedElement?.id ?? null
          : null,
      })
      setSuggestion(result)
      setEditedJson(result.contentJson ?? '')
    } catch (requestError) {
      setError(aiError(requestError))
    } finally {
      setBusy(false)
    }
  }

  async function resolve(decision: 'Accept' | 'Edit' | 'Reject') {
    if (!suggestion || busy) return
    setBusy(true); setError('')
    try {
      await resolvePresentationAiSuggestion(materialId, suggestion.id, {
        expectedDocumentRowVersion: documentRowVersion,
        decision,
        editedContentJson: decision === 'Edit' ? editedJson : null,
      })
      if (decision !== 'Reject') onApplied()
      onClose()
    } catch (requestError) {
      setError(aiError(requestError))
    } finally {
      setBusy(false)
    }
  }

  return <section className="presentation-ai-panel" aria-labelledby="presentation-ai-title">
    <header>
      <div><span><Sparkles size={18} /> AI prijedlog</span><h2 id="presentation-ai-title">Pomoć pri izradi</h2></div>
      <button aria-label="Zatvori AI pomoć bez prihvaćanja" onClick={onClose} type="button"><X /></button>
    </header>

    {enabled === false && <div className="presentation-ai-panel__disabled">
      <strong>AI pomoć je isključena</strong>
      <p>Nema automatskih OpenAI poziva ni dodatnog troška. Ručni editor ostaje potpuno dostupan.</p>
      <small>Samo vlasnik sustava može je izričito uključiti serverskom konfiguracijom i vlastitim API ključem. PLUS 5 nema naplatu unutar aplikacije.</small>
    </div>}

    {enabled === null && !error && <p role="status">Provjera dostupnosti…</p>}

    {enabled && !suggestion && <form onSubmit={event => { event.preventDefault(); void generate() }}>
      <label>Vrsta prijedloga<select value={kind} onChange={event => setKind(event.target.value as PresentationAiSuggestionKind)}>
        <option value="PRESENTATION_DRAFT">Početna struktura prezentacije</option>
        <option value="ELEMENT_CONTENT">Sadržaj tekstualnog elementa</option>
        <option value="QUICK_CHECK">Quick Check</option>
        <option value="KNOWLEDGE_MAPPING">Povezanost sa znanjem</option>
      </select></label>
      <label>Uputa učitelja<textarea maxLength={2000} value={instruction} onChange={event => setInstruction(event.target.value)} /></label>
      {!canGenerate && <small>Za ovu vrstu prijedloga odaberite odgovarajući slajd ili element.</small>}
      <p>AI se poziva samo klikom na gumb. Prijedlog se ne sprema bez vaše naknadne potvrde.</p>
      <button className="ds-action" disabled={!canGenerate || busy} type="submit"><Sparkles size={17} /> {busy ? 'Generiranje…' : 'Generiraj prijedlog'}</button>
    </form>}

    {suggestion?.contentJson && <div className="presentation-ai-panel__suggestion">
      <strong>AI prijedlog · nije spremljeno</strong>
      {editing
        ? <label>Uredite strukturirani prijedlog<textarea rows={12} value={editedJson} onChange={event => setEditedJson(event.target.value)} /></label>
        : <pre>{formatSuggestion(suggestion.contentJson)}</pre>}
      <div>
        <button disabled={busy} onClick={() => void resolve('Accept')} type="button">Prihvati</button>
        <button disabled={busy} onClick={() => setEditing(value => !value)} type="button">{editing ? 'Prikaži' : 'Uredi'}</button>
        {editing && <button disabled={busy} onClick={() => void resolve('Edit')} type="button">Spremi uređeno</button>}
        <button disabled={busy} onClick={() => void resolve('Reject')} type="button">Odbij</button>
      </div>
      <small>Prihvaćanje ponovno prolazi serversku i domensku validaciju. AI ne može objaviti prezentaciju.</small>
    </div>}

    {error && <p className="presentation-ai-panel__error" role="alert">{error} Ručni editor i dalje radi.</p>}
  </section>
}

function formatSuggestion(json: string): string {
  try { return JSON.stringify(JSON.parse(json), null, 2) }
  catch { return json }
}

function aiError(error: unknown): string {
  if (!(error instanceof ApiError)) return 'AI pomoć trenutačno nije dostupna.'
  if (error.code === 'presentation_ai_disabled') return 'AI pomoć je isključena i nije napravljen vanjski poziv.'
  if (error.code === 'concurrency_conflict') return 'Prezentacija je u međuvremenu promijenjena. Osvježite editor prije potvrde.'
  if (error.code === 'presentation_ai_invalid_output') return 'AI odgovor nije prošao sigurnu strukturnu validaciju.'
  return 'AI pomoć trenutačno nije dostupna.'
}
