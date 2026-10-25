import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  BookOpen,
  CircleHelp,
  Copy,
  FilePlus2,
  Image as ImageIcon,
  Film,
  Headphones,
  LayoutTemplate,
  Palette,
  Plus,
  Redo2,
  Save,
  Sparkles,
  Shapes,
  Table2,
  Quote,
  Trash2,
  Type,
  Undo2,
} from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  addPresentationSlide,
  createPresentationElement,
  createPresentationQuickCheck,
  deletePresentationElement,
  deletePresentationSlide,
  duplicatePresentationSlide,
  getPresentationEditor,
  initializePresentation,
  presentationAssetUrl,
  reorderPresentationSlides,
  searchPresentationKnowledgeComponents,
  updatePresentationElement,
  updatePresentationQuickCheck,
  uploadPresentationImage,
  uploadPresentationAudio,
  uploadPresentationVideo,
  type PresentationEditorWorkspace,
  type PresentationElement,
  type PresentationElementType,
  type PresentationKnowledgeComponent,
  type PresentationQuickCheck,
} from './presentationApi.ts'
import './PresentationEditorPage.css'
import { PresentationAiPanel } from './PresentationAiPanel.tsx'

type StructuredElementType = Exclude<PresentationElementType, 'IMAGE' | 'VIDEO' | 'AUDIO' | 'QUICK_CHECK'>
type AutosaveStatus = 'saved' | 'dirty' | 'saving' | 'error' | 'conflict'

interface ElementSaveResult {
  readonly outcome: 'saved' | 'error' | 'conflict'
  readonly message?: string
}

const AUTOSAVE_DELAY_MS = 800
const ELEMENT_FORM_ID = 'presentation-element-properties-form'

export function PresentationEditorPage() {
  const { materialId = '' } = useParams()
  const imageInput = useRef<HTMLInputElement>(null)
  const videoInput = useRef<HTMLInputElement>(null)
  const audioInput = useRef<HTMLInputElement>(null)
  const [workspace, setWorkspace] = useState<PresentationEditorWorkspace | null>(null)
  const [selectedSlideId, setSelectedSlideId] = useState<string | null>(null)
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [revision, setRevision] = useState(0)
  const [autosaveStatus, setAutosaveStatus] = useState<AutosaveStatus>('saved')
  const [aiOpen, setAiOpen] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    getPresentationEditor(materialId, controller.signal)
      .then((result) => {
        setWorkspace(result)
        setSelectedSlideId((current) => result.document?.slides.some(slide => slide.id === current)
          ? current
          : result.document?.slides[0]?.id ?? null)
        setSelectedElementId((current) => result.document?.slides
          .some(slide => slide.elements.some(element => element.id === current)) ? current : null)
        setError('')
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError && requestError.status === 404
          ? 'Prezentacija nije pronađena, nije vaša ili nema spremljenu skicu.'
          : 'Editor prezentacije trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [materialId, revision])

  const reload = useCallback(() => setRevision(value => value + 1), [])
  const document = workspace?.document ?? null
  const slides = document?.slides ?? []
  const selectedIndex = slides.findIndex(slide => slide.id === selectedSlideId)
  const selectedSlide = slides[selectedIndex] ?? null
  const selectedElement = selectedSlide?.elements.find(element => element.id === selectedElementId) ?? null

  async function mutate(action: () => Promise<unknown>, successMessage: string) {
    if (busy) return
    setBusy(true); setError(''); setNotice('')
    try {
      await action()
      const refreshed = await getPresentationEditor(materialId)
      setWorkspace(refreshed)
      setSelectedSlideId((current) => refreshed.document?.slides.some(slide => slide.id === current)
        ? current
        : refreshed.document?.slides[0]?.id ?? null)
      setSelectedElementId((current) => refreshed.document?.slides
        .some(slide => slide.elements.some(element => element.id === current)) ? current : null)
      setNotice(successMessage)
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Promjenu prezentacije trenutačno nije moguće spremiti.')
    } finally {
      setBusy(false)
    }
  }

  function initialize() {
    if (!workspace) return
    void mutate(
      () => initializePresentation(materialId, workspace.materialVersionId, workspace.materialRowVersion),
      'Prazna prezentacija je započeta.',
    )
  }

  function addSlide() {
    if (!document) return
    void mutate(
      () => addPresentationSlide(materialId, document.rowVersion, selectedSlideId),
      'Dodan je novi prazni slajd.',
    )
  }

  function duplicateSlide() {
    if (!document || !selectedSlideId) return
    void mutate(
      () => duplicatePresentationSlide(materialId, selectedSlideId, document.rowVersion),
      'Slajd i njegovi elementi su duplicirani.',
    )
  }

  function removeSlide() {
    if (!document || !selectedSlideId || slides.length <= 1) return
    if (!window.confirm('Obrisati odabrani slajd i sve njegove elemente?')) return
    setSelectedElementId(null)
    void mutate(
      () => deletePresentationSlide(materialId, selectedSlideId, document.rowVersion),
      'Slajd je obrisan.',
    )
  }

  function moveSlide(direction: -1 | 1) {
    if (!document || selectedIndex < 0) return
    const targetIndex = selectedIndex + direction
    if (targetIndex < 0 || targetIndex >= slides.length) return
    const ordered = slides.map(slide => slide.id)
    ;[ordered[selectedIndex], ordered[targetIndex]] = [ordered[targetIndex], ordered[selectedIndex]]
    void mutate(
      () => reorderPresentationSlides(materialId, document.rowVersion, ordered),
      'Redoslijed slajdova je spremljen.',
    )
  }

  function addElement(type: StructuredElementType) {
    if (!document || !selectedSlide) return
    const preset = newElementPreset(type)
    void mutate(
      async () => {
        const result = await createPresentationElement(materialId, selectedSlide.id, {
          expectedDocumentRowVersion: document.rowVersion,
          elementTypeCode: type,
          ...preset,
        })
        setSelectedElementId(result.elementId)
      },
      elementName(type) + ' je dodan na slajd.',
    )
  }

  function uploadImage(file: File | undefined) {
    if (!file || !document || !selectedSlide) return
    void mutate(
      async () => {
        const result = await uploadPresentationImage(
          materialId,
          selectedSlide.id,
          file,
          document.rowVersion,
        )
        setSelectedElementId(result.elementId)
      },
      'Slika je sigurno provjerena i dodana na slajd.',
    )
  }

  function uploadMedia(file: File | undefined, type: 'VIDEO' | 'AUDIO') {
    if (!file || !document || !selectedSlide) return
    void mutate(
      async () => {
        const result = type === 'VIDEO'
          ? await uploadPresentationVideo(materialId, selectedSlide.id, file, document.rowVersion)
          : await uploadPresentationAudio(materialId, selectedSlide.id, file, document.rowVersion)
        setSelectedElementId(result.elementId)
      },
      type === 'VIDEO'
        ? 'MP4 video je sigurno provjeren i dodan na slajd.'
        : 'MP3 audio je sigurno provjeren i dodan na slajd.',
    )
  }

  function addQuickCheck() {
    if (!document || !selectedSlide) return
    void mutate(
      async () => {
        const result = await createPresentationQuickCheck(materialId, selectedSlide.id, {
          expectedDocumentRowVersion: document.rowVersion,
          zIndex: selectedSlide.elements.length,
          x: 430,
          y: 260,
          width: 740,
          height: 390,
          prompt: 'Koje vrijeme koristimo za radnje koje se događaju upravo sada?',
          options: ['Present Simple', 'Present Continuous', 'Past Simple'],
          correctOptionIndex: 1,
          difficulty: 2,
          evidenceType: 1,
          knowledgeComponentIds: [],
        })
        setSelectedElementId(result.elementId)
      },
      'Quick Check je dodan na slajd.',
    )
  }

  async function saveElement(element: PresentationElement, values: ElementFormValues): Promise<ElementSaveResult> {
    if (!document || !selectedSlide) {
      return { outcome: 'error', message: 'Odabrani element više nije dostupan.' }
    }

    setError('')
    setNotice('')
    try {
      if (element.elementTypeCode === 'QUICK_CHECK' && values.quickCheck) {
        const quickCheck = values.quickCheck
        const correctOptionIndex = quickCheck.options.findIndex(
          option => option.key === quickCheck.correctOptionKey,
        )
        await updatePresentationQuickCheck(materialId, selectedSlide.id, element.id, {
          expectedDocumentRowVersion: document.rowVersion,
          zIndex: values.zIndex,
          x: values.x,
          y: values.y,
          width: values.width,
          height: values.height,
          prompt: quickCheck.prompt,
          options: quickCheck.options.map(option => option.text),
          correctOptionIndex,
          difficulty: quickCheck.difficulty,
          evidenceType: quickCheck.evidenceType,
          knowledgeComponentIds: quickCheck.knowledgeComponents.map(component => component.id),
        })
      } else {
        await updatePresentationElement(materialId, selectedSlide.id, element.id, {
          expectedDocumentRowVersion: document.rowVersion,
          zIndex: values.zIndex,
          x: values.x,
          y: values.y,
          width: values.width,
          height: values.height,
          payloadJson: JSON.stringify(values.payload),
        })
      }

      const refreshed = await getPresentationEditor(materialId)
      setWorkspace(refreshed)
      setNotice('Sve promjene su spremljene.')
      return { outcome: 'saved' }
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.code === 'concurrency_conflict') {
        return {
          outcome: 'conflict',
          message: 'Prezentacija je promijenjena u drugoj kartici. Vaš unos je sačuvan lokalno i nije prepisao noviju verziju.',
        }
      }
      return {
        outcome: 'error',
        message: requestError instanceof ApiError
          ? requestError.message
          : 'Automatsko spremanje trenutačno nije uspjelo. Vaš unos je sačuvan u ovoj kartici.',
      }
    }
  }

  function removeElement() {
    if (!document || !selectedSlide || !selectedElement) return
    if (!window.confirm('Obrisati odabrani element?')) return
    void mutate(
      () => deletePresentationElement(
        materialId,
        selectedSlide.id,
        selectedElement.id,
        document.rowVersion,
      ),
      'Element je obrisan.',
    )
    setSelectedElementId(null)
  }

  if (loading) return <EditorState text="Učitavanje editora prezentacije…" />
  if (error && !workspace) return <EditorState text={error} error retry={() => { setLoading(true); setError(''); reload() }} />
  if (!workspace) return <EditorState text="Prezentacija nije dostupna." error />

  if (!document) {
    return <section className="presentation-start" aria-labelledby="presentation-start-title">
      <nav><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}/edit`}>Uredi</Link><span>›</span><strong>Prezentacija</strong></nav>
      <div>
        <FilePlus2 size={52} />
        <p>4.3 Novi materijal — Prezentacija</p>
        <h1 id="presentation-start-title">Započnite praznu prezentaciju</h1>
        <span>Stvorit će se dokument 16:9 s jednim praznim slajdom u skici v{workspace.materialVersionNumber}.</span>
        {error && <strong role="alert">{error}</strong>}
        <button className="ds-action" disabled={busy} onClick={initialize} type="button"><Plus size={18} /> Započni prezentaciju</button>
        <Link to={`/materials/${materialId}/edit`}>Povratak na podatke materijala</Link>
      </div>
    </section>
  }

  const editorWriteLocked = busy || autosaveStatus !== 'saved'
  const autosaveLabel = ({
    saved: 'Sve promjene spremljene',
    dirty: 'Čeka automatsko spremanje…',
    saving: 'Spremanje…',
    error: 'Promjene nisu spremljene',
    conflict: 'Otkriven je sukob verzija',
  } satisfies Record<AutosaveStatus, string>)[autosaveStatus]

  return <section className="presentation-editor" aria-labelledby="presentation-editor-title">
    <nav className="presentation-editor__breadcrumb"><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}/edit`}>Uredi materijal</Link><span>›</span><strong>Prezentacija</strong></nav>
    <header className="presentation-editor__hero">
      <div><h1 id="presentation-editor-title">4.3 Novi materijal – Prezentacija</h1><p>{workspace.title} · skica v{workspace.materialVersionNumber}</p></div>
      <div>
        <span className={`presentation-editor__save-state is-${autosaveStatus}`} role="status"><span aria-hidden="true" />{autosaveLabel}</span>
        <Link className="ds-action ds-action--secondary" to={`/materials/${materialId}/edit`}>Odustani</Link>
        <button className="ds-action ds-action--secondary" disabled={editorWriteLocked} onClick={() => setAiOpen(value => !value)} type="button"><Sparkles size={17} /> AI pomoć</button>
        <button className="ds-action ds-action--secondary" disabled={busy || autosaveStatus === 'saving'} form={selectedElement ? ELEMENT_FORM_ID : undefined} onClick={() => { if (!selectedElement) setNotice('Sve promjene su spremljene.') }} type={selectedElement ? 'submit' : 'button'}><Save size={17} /> Spremi nacrt</button>
        {editorWriteLocked
          ? <button className="ds-action" disabled type="button">Nastavi <ArrowRight size={17} /></button>
          : <Link className="ds-action" to={`/materials/${materialId}/presentation/review`}>Nastavi <ArrowRight size={17} /></Link>}
      </div>
    </header>

    <ol className="presentation-editor__steps"><li className="is-done"><span>✓</span> Vrsta i osnovni podaci</li><li className="is-done"><span>✓</span> Ciljevi i povezanost</li><li className="is-active"><span>3</span> Izrada prezentacije</li><li><span>4</span> Pregled i spremanje</li></ol>
    <div className="presentation-editor__intro"><h2>3. Izrada prezentacije</h2><p>Gradite 16:9 slajdove osnovnim i nastavnim elementima te privatnim, sigurnosno provjerenim medijima.</p></div>
    {(error || notice) && <div className={`presentation-editor__notice${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'}>{error || notice}</div>}
    {aiOpen && <PresentationAiPanel
      documentRowVersion={document.rowVersion}
      materialId={materialId}
      onApplied={() => { setNotice('AI prijedlog je potvrđen i spremljen kao učiteljeva skica.'); reload() }}
      onClose={() => setAiOpen(false)}
      selectedElement={selectedElement}
      selectedSlideId={selectedSlideId}
    />}

    <div className="presentation-editor__workspace" aria-busy={busy}>
      <div className="presentation-editor__toolbar" role="toolbar" aria-label="Alati prezentacije">
        <button disabled={editorWriteLocked} onClick={() => addElement('TEXT')} type="button"><Type size={18} /> Tekst</button>
        <button disabled={editorWriteLocked} onClick={() => imageInput.current?.click()} type="button"><ImageIcon size={18} /> Slika</button>
        <input ref={imageInput} accept="image/png,image/jpeg,image/webp" hidden type="file" onChange={event => { uploadImage(event.target.files?.[0]); event.target.value = '' }} />
        <button disabled={editorWriteLocked} onClick={() => addElement('SHAPE')} type="button"><Shapes size={18} /> Oblik</button>
        <button disabled={editorWriteLocked} onClick={() => addElement('TABLE')} type="button"><Table2 size={18} /> Tablica</button>
        <button disabled={editorWriteLocked} onClick={() => addElement('RULE')} type="button"><BookOpen size={18} /> Pravilo</button>
        <button disabled={editorWriteLocked} onClick={() => addElement('EXAMPLE')} type="button"><Quote size={18} /> Primjer</button>
        <button disabled={editorWriteLocked} onClick={() => addElement('VOCABULARY')} type="button"><Type size={18} /> Vokabular</button>
        <button disabled={editorWriteLocked} onClick={() => addElement('QUESTION')} type="button"><CircleHelp size={18} /> Pitanje</button>
        <button disabled={editorWriteLocked} onClick={addQuickCheck} type="button"><CircleHelp size={18} /> Quick Check</button>
        <button disabled={editorWriteLocked} onClick={() => videoInput.current?.click()} type="button"><Film size={18} /> Video</button>
        <input ref={videoInput} accept="video/mp4,.mp4" hidden type="file" onChange={event => { uploadMedia(event.target.files?.[0], 'VIDEO'); event.target.value = '' }} />
        <button disabled={editorWriteLocked} onClick={() => audioInput.current?.click()} type="button"><Headphones size={18} /> Audio</button>
        <input ref={audioInput} accept="audio/mpeg,.mp3" hidden type="file" onChange={event => { uploadMedia(event.target.files?.[0], 'AUDIO'); event.target.value = '' }} />
        <span />
        <button disabled title="Povijest poništavanja nije dio autosave contracta." type="button"><Undo2 size={17} /> Poništi</button>
        <button disabled title="Povijest ponavljanja nije dio autosave contracta." type="button"><Redo2 size={17} /> Ponovi</button>
        <button disabled title="Tema nije dio core element contracta." type="button"><Palette size={17} /> Tema</button>
        <button disabled title="Predlošci rasporeda nisu dio ove faze." type="button"><LayoutTemplate size={17} /> Raspored</button>
      </div>

      <aside className="presentation-editor__slides" aria-label="Slajdovi prezentacije">
        <ol>{slides.map((slide, index) => <li key={slide.id}>
          <button className={slide.id === selectedSlideId ? 'is-selected' : ''} onClick={() => { setSelectedSlideId(slide.id); setSelectedElementId(null) }} type="button" aria-current={slide.id === selectedSlideId ? 'true' : undefined}>
            <span>{index + 1}</span><div><FilePlus2 size={21} /><small>{slide.elements.length === 0 ? 'Prazan slajd' : `${slide.elements.length} elementa`}</small></div>
          </button>
        </li>)}</ol>
        <div className="presentation-editor__order">
          <button aria-label="Pomakni slajd gore" disabled={busy || selectedIndex <= 0} onClick={() => moveSlide(-1)} type="button"><ArrowUp /></button>
          <button aria-label="Pomakni slajd dolje" disabled={busy || selectedIndex < 0 || selectedIndex === slides.length - 1} onClick={() => moveSlide(1)} type="button"><ArrowDown /></button>
        </div>
        <button className="presentation-editor__add" disabled={editorWriteLocked || slides.length >= 200} onClick={addSlide} type="button"><Plus size={17} /> Dodaj slajd</button>
      </aside>

      <main className="presentation-editor__stage">
        <div className="presentation-editor__canvas" aria-label={`Slajd ${Math.max(1, selectedIndex + 1)}`}>
          {selectedSlide?.elements.length === 0 && <div className="presentation-editor__empty"><FilePlus2 size={38} /><h2>Prazan slajd</h2><p>Odaberite osnovni element iz alatne trake.</p></div>}
          {selectedSlide?.elements.map(element => <CanvasElement
            element={element}
            key={element.id}
            selected={element.id === selectedElementId}
            onSelect={() => setSelectedElementId(element.id)}
          />)}
        </div>
        <footer>
          <button disabled={selectedIndex <= 0} onClick={() => { setSelectedSlideId(slides[selectedIndex - 1]?.id ?? null); setSelectedElementId(null) }} type="button"><ArrowLeft size={16} /> Prethodni</button>
          <span>{Math.max(1, selectedIndex + 1)} / {slides.length}</span>
          <button disabled={selectedIndex < 0 || selectedIndex >= slides.length - 1} onClick={() => { setSelectedSlideId(slides[selectedIndex + 1]?.id ?? null); setSelectedElementId(null) }} type="button">Sljedeći <ArrowRight size={16} /></button>
        </footer>
      </main>

      <aside className="presentation-editor__properties" aria-label="Postavke elementa">
        <div role="tablist"><button className={!selectedElement ? 'is-active' : ''} role="tab" aria-selected={!selectedElement}>Slajd</button><button className={selectedElement ? 'is-active' : ''} disabled={!selectedElement} role="tab" aria-selected={Boolean(selectedElement)}>Element</button><button disabled role="tab" aria-selected="false">Stranica</button></div>
        {selectedSlide && selectedSlide.elements.length > 0 && <nav className="presentation-editor__layers" aria-label="Elementi slajda">
          <strong>Elementi slajda</strong>
          {[...selectedSlide.elements].reverse().map(element => <button className={element.id === selectedElementId ? 'is-selected' : ''} key={element.id} onClick={() => setSelectedElementId(element.id)} type="button"><span>{element.zIndex + 1}</span>{elementName(element.elementTypeCode)}</button>)}
        </nav>}
        {selectedElement
          ? <ElementProperties key={`${selectedElement.id}-${document.rowVersion}`} materialId={materialId} materialVersionId={workspace.materialVersionId} element={selectedElement} busy={busy} count={selectedSlide?.elements.length ?? 0} onDelete={removeElement} onReload={reload} onSave={saveElement} onStatusChange={setAutosaveStatus} />
          : <><section><h2>Raspored slajda</h2><select aria-label="Raspored slajda" disabled><option>Prazan slajd</option></select><button disabled type="button">Promijeni raspored</button></section><section><h2>Pozadina</h2><div><span /><code>#FFFFFF</code><button disabled type="button">Promijeni</button></div></section><p><strong>Phase 7.7</strong> Strukturne promjene spremaju se odmah, a uređivanje elementa automatski nakon kratke stanke.</p></>}
      </aside>
    </div>

    <div className="presentation-editor__slide-actions">
      <button disabled={editorWriteLocked || slides.length >= 200} onClick={duplicateSlide} type="button"><Copy size={17} /> Dupliciraj odabrani slajd</button>
      <button disabled={editorWriteLocked || slides.length <= 1} onClick={removeSlide} type="button"><Trash2 size={17} /> Obriši odabrani slajd</button>
    </div>
  </section>
}

interface ElementFormValues {
  readonly zIndex: number
  readonly x: number
  readonly y: number
  readonly width: number
  readonly height: number
  readonly payload: Record<string, unknown>
  readonly quickCheck: PresentationQuickCheck | null
}

interface ElementRecoverySnapshot {
  readonly schemaVersion: 1
  readonly baseElementSignature: string
  readonly values: ElementFormValues
}

function ElementProperties({ materialId, materialVersionId, element, busy, count, onDelete, onReload, onSave, onStatusChange }: {
  readonly materialId: string
  readonly materialVersionId: string
  readonly element: PresentationElement
  readonly busy: boolean
  readonly count: number
  readonly onDelete: () => void
  readonly onReload: () => void
  readonly onSave: (element: PresentationElement, values: ElementFormValues) => Promise<ElementSaveResult>
  readonly onStatusChange: (status: AutosaveStatus) => void
}) {
  const formRef = useRef<HTMLFormElement>(null)
  const serverValues: ElementFormValues = {
    zIndex: element.zIndex,
    x: element.x,
    y: element.y,
    width: element.width,
    height: element.height,
    payload: parsePayload(element.payloadJson),
    quickCheck: element.quickCheck,
  }
  const baseElementSignature = serializeElementForm(serverValues)
  const recoveryKey = `plus5:presentation-draft:${materialVersionId}:${element.id}`
  const recoveredValues = readElementRecovery(recoveryKey, baseElementSignature)
  const [values, setValues] = useState<ElementFormValues>(recoveredValues ?? serverValues)
  const [status, setStatus] = useState<AutosaveStatus>(recoveredValues ? 'dirty' : 'saved')
  const [statusMessage, setStatusMessage] = useState(recoveredValues
    ? 'Vraćen je nespremljeni unos iz ove kartice.'
    : 'Sve promjene su spremljene.')
  const lastSavedSignature = useRef(baseElementSignature)
  const saving = useRef(false)

  useEffect(() => onStatusChange(status), [onStatusChange, status])

  useEffect(() => {
    const currentSignature = serializeElementForm(values)
    if (currentSignature === lastSavedSignature.current) {
      try { window.sessionStorage.removeItem(recoveryKey) } catch { /* storage is best effort */ }
      return
    }

    const snapshot: ElementRecoverySnapshot = {
      schemaVersion: 1,
      baseElementSignature,
      values,
    }
    try { window.sessionStorage.setItem(recoveryKey, JSON.stringify(snapshot)) } catch { /* storage is best effort */ }
  }, [baseElementSignature, recoveryKey, values])

  const persist = useCallback(async (snapshot: ElementFormValues) => {
    if (saving.current || serializeElementForm(snapshot) === lastSavedSignature.current) {
      setStatus('saved')
      setStatusMessage('Sve promjene su spremljene.')
      return
    }
    if (!formRef.current?.checkValidity()) {
      setStatus('dirty')
      setStatusMessage('Dovršite obavezna polja prije spremanja.')
      return
    }

    saving.current = true
    setStatus('saving')
    setStatusMessage('Spremanje promjena…')
    const result = await onSave(element, snapshot)
    saving.current = false

    if (result.outcome === 'saved') {
      lastSavedSignature.current = serializeElementForm(snapshot)
      try { window.sessionStorage.removeItem(recoveryKey) } catch { /* storage is best effort */ }
      setStatus('saved')
      setStatusMessage('Sve promjene su spremljene.')
      return
    }

    setStatus(result.outcome)
    setStatusMessage(result.message ?? 'Promjene nije moguće spremiti.')
  }, [element, onSave, recoveryKey])

  useEffect(() => {
    if (status !== 'dirty') return
    const timer = window.setTimeout(() => { void persist(values) }, AUTOSAVE_DELAY_MS)
    return () => window.clearTimeout(timer)
  }, [persist, status, values])

  useEffect(() => {
    if (status === 'saved') return
    const warn = (event: BeforeUnloadEvent) => {
      event.preventDefault()
      event.returnValue = ''
    }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [status])

  const markDirty = (update: (current: ElementFormValues) => ElementFormValues) => {
    setValues(update)
    setStatus('dirty')
    setStatusMessage('Nespremljene promjene. Automatsko spremanje uskoro počinje.')
  }
  const updateNumber = (name: keyof Omit<ElementFormValues, 'payload'>, value: string) => markDirty(current => ({ ...current, [name]: Number(value) }))
  const updatePayload = (name: string, value: unknown) => markDirty(current => ({ ...current, payload: { ...current.payload, [name]: value } }))
  const updateQuickCheck = (changes: Partial<PresentationQuickCheck>) => markDirty(current => ({
    ...current,
    quickCheck: current.quickCheck ? { ...current.quickCheck, ...changes } : null,
  }))

  const retry = () => { void persist(values) }
  const reloadLatest = () => {
    try { window.sessionStorage.removeItem(recoveryKey) } catch { /* storage is best effort */ }
    onReload()
  }

  return <form className="presentation-element-form" id={ELEMENT_FORM_ID} ref={formRef} onSubmit={event => { event.preventDefault(); void persist(values) }}>
    <div className={`presentation-element-form__save-state is-${status}`} role={status === 'error' || status === 'conflict' ? 'alert' : 'status'}>
      <span aria-hidden="true" />
      <div><strong>{status === 'conflict' ? 'Sukob verzija' : status === 'error' ? 'Spremanje nije uspjelo' : status === 'saving' ? 'Automatsko spremanje' : status === 'dirty' ? 'Nespremljene promjene' : 'Spremljeno'}</strong><small>{statusMessage}</small></div>
      {status === 'error' && <button onClick={retry} type="button">Pokušaj ponovno</button>}
      {status === 'conflict' && <button onClick={reloadLatest} type="button">Učitaj noviju verziju</button>}
    </div>
    <fieldset className="presentation-element-form__fields" disabled={busy || status === 'saving'}>
    <header><div><small>Odabrani element</small><h2>{elementName(element.elementTypeCode)}</h2></div><button aria-label="Obriši element" disabled={busy} onClick={onDelete} type="button"><Trash2 size={17} /></button></header>
    {element.elementTypeCode === 'TEXT' && <>
      <label>Sadržaj<textarea maxLength={4000} required value={String(values.payload.text ?? '')} onChange={event => updatePayload('text', event.target.value)} /></label>
      <label>Veličina teksta<input min="12" max="160" type="number" value={Number(values.payload.fontSize ?? 52)} onChange={event => updatePayload('fontSize', Number(event.target.value))} /></label>
      <label>Debljina<select value={String(values.payload.fontWeight ?? 'bold')} onChange={event => updatePayload('fontWeight', event.target.value)}><option value="regular">Regular</option><option value="bold">Bold</option></select></label>
      <label>Poravnanje<select value={String(values.payload.align ?? 'left')} onChange={event => updatePayload('align', event.target.value)}><option value="left">Lijevo</option><option value="center">Sredina</option><option value="right">Desno</option></select></label>
      <label>Boja<input type="color" value={String(values.payload.color ?? '#173E67')} onChange={event => updatePayload('color', event.target.value.toUpperCase())} /></label>
    </>}
    {element.elementTypeCode === 'IMAGE' && <>
      <label>Alternativni opis<input maxLength={500} value={String(values.payload.altText ?? '')} onChange={event => updatePayload('altText', event.target.value)} /></label>
      <label>Prikaz<select value={String(values.payload.fit ?? 'contain')} onChange={event => updatePayload('fit', event.target.value)}><option value="contain">Cijela slika</option><option value="cover">Ispuni okvir</option></select></label>
      <p className="presentation-element-form__asset">{element.asset?.originalFileName}<small>{element.asset ? formatBytes(element.asset.sizeBytes) : 'Datoteka nije dostupna'}</small></p>
    </>}
    {element.elementTypeCode === 'SHAPE' && <>
      <label>Oblik<select value={String(values.payload.shape ?? 'rectangle')} onChange={event => updatePayload('shape', event.target.value)}><option value="rectangle">Pravokutnik</option><option value="ellipse">Elipsa</option><option value="line">Linija</option></select></label>
      <label>Ispuna<input type="color" value={String(values.payload.fill ?? '#FFF3BF')} onChange={event => updatePayload('fill', event.target.value.toUpperCase())} /></label>
      <label>Rub<input type="color" value={String(values.payload.stroke ?? '#F4B400')} onChange={event => updatePayload('stroke', event.target.value.toUpperCase())} /></label>
      <label>Debljina ruba<input min="0" max="20" type="number" value={Number(values.payload.strokeWidth ?? 4)} onChange={event => updatePayload('strokeWidth', Number(event.target.value))} /></label>
    </>}
    {element.elementTypeCode === 'TABLE' && <TableFields payload={values.payload} onChange={updatePayload} />}
    {element.elementTypeCode === 'RULE' && <>
      <label>Naslov pravila<input maxLength={160} required value={String(values.payload.heading ?? '')} onChange={event => updatePayload('heading', event.target.value)} /></label>
      <label>Pravilo<textarea maxLength={4000} required value={String(values.payload.body ?? '')} onChange={event => updatePayload('body', event.target.value)} /></label>
      <label>Naglasna boja<input type="color" value={String(values.payload.accentColor ?? '#F4B400')} onChange={event => updatePayload('accentColor', event.target.value.toUpperCase())} /></label>
    </>}
    {element.elementTypeCode === 'EXAMPLE' && <>
      <label>Primjer<textarea maxLength={2000} required value={String(values.payload.example ?? '')} onChange={event => updatePayload('example', event.target.value)} /></label>
      <label>Objašnjenje<textarea maxLength={2000} value={String(values.payload.explanation ?? '')} onChange={event => updatePayload('explanation', event.target.value)} /></label>
      <label>Naglasna boja<input type="color" value={String(values.payload.accentColor ?? '#1767FF')} onChange={event => updatePayload('accentColor', event.target.value.toUpperCase())} /></label>
    </>}
    {element.elementTypeCode === 'VOCABULARY' && <>
      <label>Pojam<input maxLength={200} required value={String(values.payload.term ?? '')} onChange={event => updatePayload('term', event.target.value)} /></label>
      <label>Značenje<textarea maxLength={1000} required value={String(values.payload.definition ?? '')} onChange={event => updatePayload('definition', event.target.value)} /></label>
      <label>Primjer uporabe<textarea maxLength={2000} value={String(values.payload.example ?? '')} onChange={event => updatePayload('example', event.target.value)} /></label>
    </>}
    {element.elementTypeCode === 'QUESTION' && <>
      <label>Pitanje<textarea maxLength={4000} required value={String(values.payload.prompt ?? '')} onChange={event => updatePayload('prompt', event.target.value)} /></label>
      <label>Napomena za raspravu<textarea maxLength={4000} value={String(values.payload.discussionNote ?? '')} onChange={event => updatePayload('discussionNote', event.target.value)} /></label>
      <p className="presentation-element-form__asset"><strong>Nastavno pitanje</strong><small>Nije kviz, zadatak za ocjenjivanje ni dokaz znanja.</small></p>
    </>}
    {element.elementTypeCode === 'VIDEO' && <>
      <label>Naslov videa<input maxLength={200} value={String(values.payload.title ?? '')} onChange={event => updatePayload('title', event.target.value)} /></label>
      <label className="presentation-element-form__check"><input checked disabled type="checkbox" /> Prikaži kontrole reprodukcije</label>
      <p className="presentation-element-form__asset">{element.asset?.originalFileName}<small>Privatni MP4 · {element.asset ? formatBytes(element.asset.sizeBytes) : 'Datoteka nije dostupna'}</small></p>
    </>}
    {element.elementTypeCode === 'AUDIO' && <>
      <label className="presentation-element-form__check"><input checked disabled type="checkbox" /> Prikaži kontrole reprodukcije</label>
      <label className="presentation-element-form__check"><input checked={false} disabled type="checkbox" /> Automatska reprodukcija</label>
      <p className="presentation-element-form__asset">{element.asset?.originalFileName}<small>Privatni MP3 · ručno pokretanje · {element.asset ? formatBytes(element.asset.sizeBytes) : 'Datoteka nije dostupna'}</small></p>
    </>}
    {element.elementTypeCode === 'QUICK_CHECK' && values.quickCheck && <QuickCheckFields
      materialId={materialId}
      quickCheck={values.quickCheck}
      onChange={updateQuickCheck}
    />}
    <fieldset><legend>Položaj i veličina</legend>
      <label>X<input min="0" max="1599" step="1" type="number" value={values.x} onChange={event => updateNumber('x', event.target.value)} /></label>
      <label>Y<input min="0" max="899" step="1" type="number" value={values.y} onChange={event => updateNumber('y', event.target.value)} /></label>
      <label>Širina<input min="1" max="1600" step="1" type="number" value={values.width} onChange={event => updateNumber('width', event.target.value)} /></label>
      <label>Visina<input min="1" max="900" step="1" type="number" value={values.height} onChange={event => updateNumber('height', event.target.value)} /></label>
      <label>Sloj<input min="0" max={Math.max(0, count - 1)} type="number" value={values.zIndex} onChange={event => updateNumber('zIndex', event.target.value)} /></label>
    </fieldset>
    </fieldset>
    <button className="ds-action" disabled={busy || status === 'saving'} type="submit"><Save size={17} /> Spremi sada</button>
  </form>
}

function serializeElementForm(values: ElementFormValues): string {
  return JSON.stringify(values)
}

function readElementRecovery(key: string, baseElementSignature: string): ElementFormValues | null {
  try {
    const raw = window.sessionStorage.getItem(key)
    if (!raw) return null
    const snapshot = JSON.parse(raw) as Partial<ElementRecoverySnapshot>
    if (snapshot.schemaVersion !== 1
      || snapshot.baseElementSignature !== baseElementSignature
      || !snapshot.values) {
      window.sessionStorage.removeItem(key)
      return null
    }
    return serializeElementForm(snapshot.values) === baseElementSignature ? null : snapshot.values
  } catch {
    try { window.sessionStorage.removeItem(key) } catch { /* storage is best effort */ }
    return null
  }
}

function TableFields({ payload, onChange }: { readonly payload: Record<string, unknown>; readonly onChange: (name: string, value: unknown) => void }) {
  const cells = Array.isArray(payload.cells) ? payload.cells as string[][] : [['Pojam', 'Opis'], ['Primjer', 'Vrijednost']]
  const text = cells.map(row => row.join(' | ')).join('\n')
  return <>
    <label>Ćelije<textarea value={text} onChange={event => onChange('cells', event.target.value.split('\n').filter(Boolean).slice(0, 20).map(row => row.split('|').slice(0, 10).map(cell => cell.trim().slice(0, 500))))} /><small>Redovi su u novom retku, ćelije odvojite znakom |.</small></label>
    <label className="presentation-element-form__check"><input type="checkbox" checked={Boolean(payload.headerRow)} onChange={event => onChange('headerRow', event.target.checked)} /> Prvi red je zaglavlje</label>
  </>
}

function QuickCheckFields({ materialId, quickCheck, onChange }: {
  readonly materialId: string
  readonly quickCheck: PresentationQuickCheck
  readonly onChange: (changes: Partial<PresentationQuickCheck>) => void
}) {
  const [pickerOpen, setPickerOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [candidates, setCandidates] = useState<readonly PresentationKnowledgeComponent[]>([])
  const [catalogLoading, setCatalogLoading] = useState(false)
  const [catalogError, setCatalogError] = useState('')
  const [catalogRevision, setCatalogRevision] = useState(0)

  useEffect(() => {
    if (!pickerOpen) return
    const controller = new AbortController()
    const delay = window.setTimeout(() => {
      setCatalogLoading(true)
      setCatalogError('')
      searchPresentationKnowledgeComponents(materialId, search, controller.signal)
        .then(result => setCandidates(result.items))
        .catch((requestError: unknown) => {
          if (requestError instanceof DOMException && requestError.name === 'AbortError') return
          setCandidates([])
          setCatalogError('Katalog Knowledge Componenta trenutačno nije moguće učitati.')
        })
        .finally(() => { if (!controller.signal.aborted) setCatalogLoading(false) })
    }, 200)
    return () => { window.clearTimeout(delay); controller.abort() }
  }, [materialId, pickerOpen, search, catalogRevision])

  const selectedIds = new Set(quickCheck.knowledgeComponents.map(component => component.id))
  const availableCandidates = candidates.filter(component => !selectedIds.has(component.id))
  const addKnowledgeComponent = (component: PresentationKnowledgeComponent) => onChange({
    knowledgeComponents: [...quickCheck.knowledgeComponents, component],
    knowledgeMappingPending: false,
  })
  const removeKnowledgeComponent = (componentId: string) => {
    const knowledgeComponents = quickCheck.knowledgeComponents.filter(
      component => component.id !== componentId,
    )
    onChange({
      knowledgeComponents,
      knowledgeMappingPending: knowledgeComponents.length === 0,
    })
  }
  const updateOption = (index: number, text: string) => onChange({
    options: quickCheck.options.map((option, optionIndex) => optionIndex === index
      ? { ...option, text }
      : option),
  })
  const addOption = () => {
    if (quickCheck.options.length >= 8) return
    onChange({
      options: [...quickCheck.options, {
        key: String.fromCharCode(65 + quickCheck.options.length),
        text: 'Novi odgovor',
      }],
    })
  }
  const removeOption = (index: number) => {
    if (quickCheck.options.length <= 2) return
    const correctIndex = quickCheck.options.findIndex(
      option => option.key === quickCheck.correctOptionKey,
    )
    const options = quickCheck.options
      .filter((_, optionIndex) => optionIndex !== index)
      .map((option, optionIndex) => ({
        ...option,
        key: String.fromCharCode(65 + optionIndex),
      }))
    const nextCorrectIndex = correctIndex === index
      ? 0
      : correctIndex > index ? correctIndex - 1 : Math.max(0, correctIndex)
    onChange({
      options,
      correctOptionKey: options[nextCorrectIndex]?.key ?? 'A',
    })
  }

  return <section className="presentation-quick-check-fields" aria-label="Postavke Quick Checka">
    <label>Pitanje<textarea maxLength={4000} required value={quickCheck.prompt} onChange={event => onChange({ prompt: event.target.value })} /></label>
    <fieldset>
      <legend>Odgovori · označite jedan točan</legend>
      {quickCheck.options.map((option, index) => <label className="presentation-quick-check-fields__option" key={option.key}>
        <input
          checked={option.key === quickCheck.correctOptionKey}
          name="quick-check-correct-answer"
          onChange={() => onChange({ correctOptionKey: option.key })}
          type="radio"
        />
        <input
          aria-label={`Odgovor ${option.key}`}
          maxLength={1000}
          required
          value={option.text}
          onChange={event => updateOption(index, event.target.value)}
        />
        <button aria-label={`Ukloni odgovor ${option.key}`} disabled={quickCheck.options.length <= 2} onClick={() => removeOption(index)} type="button"><Trash2 size={15} /></button>
      </label>)}
      <button disabled={quickCheck.options.length >= 8} onClick={addOption} type="button"><Plus size={15} /> Dodaj odgovor</button>
    </fieldset>
    <label>Težina<select value={quickCheck.difficulty} onChange={event => onChange({ difficulty: Number(event.target.value) })}>
      {[1, 2, 3, 4, 5].map(value => <option key={value} value={value}>{value}</option>)}
    </select></label>
    <label>Vrsta dokaza<select value={quickCheck.evidenceType} onChange={event => onChange({ evidenceType: Number(event.target.value) })}>
      <option value="1">Prepoznavanje</option>
      <option value="2">Razumijevanje</option>
      <option value="3">Primjena</option>
      <option value="4">Produkcija</option>
    </select></label>
    <section className="presentation-knowledge-mapping" aria-label="Povezanost sa znanjem">
      <header>
        <div><strong>Povezanost sa znanjem</strong><small>{quickCheck.knowledgeComponents.length === 0 ? 'Potrebno prije objave' : `${quickCheck.knowledgeComponents.length} odabrano`}</small></div>
        <button onClick={() => setPickerOpen(value => !value)} type="button"><Plus size={15} /> Odaberi komponentu</button>
      </header>
      {quickCheck.knowledgeComponents.length === 0
        ? <p className="presentation-knowledge-mapping__empty">Quick Check još nije povezan s Knowledge Componentom.</p>
        : <ul className="presentation-knowledge-mapping__selected">{quickCheck.knowledgeComponents.map(component => <li key={component.id}>
          <span><strong>{component.name}</strong><small>{component.knowledgeAreaName} · {component.knowledgeModelCode} {component.knowledgeModelVersion}{component.knowledgeModelStatus !== 'Published' || component.knowledgeComponentStatus !== 'Active' ? ` · ${component.knowledgeModelStatus}/${component.knowledgeComponentStatus}` : ''}</small></span>
          <button aria-label={`Ukloni ${component.name}`} onClick={() => removeKnowledgeComponent(component.id)} type="button"><Trash2 size={14} /></button>
        </li>)}</ul>}
      {pickerOpen && <div className="presentation-knowledge-picker">
        <label>Pretraži objavljene komponente<input maxLength={200} placeholder="Naziv, područje ili model" value={search} onChange={event => setSearch(event.target.value)} /></label>
        {catalogLoading && <p>Učitavanje kataloga…</p>}
        {catalogError && <div role="alert"><p>{catalogError}</p><button onClick={() => setCatalogRevision(value => value + 1)} type="button">Pokušaj ponovno</button></div>}
        {!catalogLoading && !catalogError && availableCandidates.length === 0 && <p>Nema dostupnih aktivnih leaf komponenti.</p>}
        {!catalogLoading && !catalogError && availableCandidates.length > 0 && <ul>{availableCandidates.map(component => <li key={component.id}>
          <button onClick={() => addKnowledgeComponent(component)} type="button"><strong>{component.name}</strong><small>{component.knowledgeAreaName} · {component.knowledgeModelCode} {component.knowledgeModelVersion}</small></button>
        </li>)}</ul>}
      </div>}
    </section>
  </section>
}

function CanvasElement({ element, selected, onSelect }: { readonly element: PresentationElement; readonly selected: boolean; readonly onSelect: () => void }) {
  const payload = parsePayload(element.payloadJson)
  const style = {
    left: `${element.x / 16}%`,
    top: `${element.y / 9}%`,
    width: `${element.width / 16}%`,
    height: `${element.height / 9}%`,
    zIndex: element.zIndex + 1,
  }
  return <div className={`presentation-canvas-element presentation-canvas-element--${element.elementTypeCode.toLowerCase()}${selected ? ' is-selected' : ''}`} onClick={event => { event.stopPropagation(); onSelect() }} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') onSelect() }} role="button" tabIndex={0} style={style}>
    {element.elementTypeCode === 'TEXT' && <span style={{ color: String(payload.color), fontSize: `${Number(payload.fontSize) / 16}cqw`, fontWeight: payload.fontWeight === 'bold' ? 800 : 400, textAlign: String(payload.align) as 'left' | 'center' | 'right' }}>{String(payload.text ?? '')}</span>}
    {element.elementTypeCode === 'IMAGE' && element.asset && <img alt={String(payload.altText ?? '')} src={presentationAssetUrl(element.asset)} style={{ objectFit: String(payload.fit) as 'contain' | 'cover' }} />}
    {element.elementTypeCode === 'SHAPE' && <span style={{ background: String(payload.fill), borderColor: String(payload.stroke), borderRadius: payload.shape === 'ellipse' ? '50%' : payload.shape === 'line' ? '0' : '.4rem', borderWidth: payload.shape === 'line' ? '0 0 3px' : `${Number(payload.strokeWidth)}px` }} />}
    {element.elementTypeCode === 'TABLE' && <CanvasTable payload={payload} />}
    {element.elementTypeCode === 'RULE' && <article className="presentation-teaching-card" style={{ borderColor: String(payload.accentColor) }}><small>PRAVILO</small><strong>{String(payload.heading ?? '')}</strong><p>{String(payload.body ?? '')}</p></article>}
    {element.elementTypeCode === 'EXAMPLE' && <article className="presentation-teaching-card" style={{ borderColor: String(payload.accentColor) }}><small>PRIMJER</small><strong>{String(payload.example ?? '')}</strong><p>{String(payload.explanation ?? '')}</p></article>}
    {element.elementTypeCode === 'VOCABULARY' && <article className="presentation-teaching-card"><small>VOKABULAR</small><strong>{String(payload.term ?? '')}</strong><p>{String(payload.definition ?? '')}</p><em>{String(payload.example ?? '')}</em></article>}
    {element.elementTypeCode === 'QUESTION' && <article className="presentation-teaching-card presentation-teaching-card--question"><small>PITANJE ZA RASPRAVU</small><strong>{String(payload.prompt ?? '')}</strong><p>{String(payload.discussionNote ?? '')}</p></article>}
    {element.elementTypeCode === 'VIDEO' && element.asset && <video controls preload="metadata" src={presentationAssetUrl(element.asset)} />}
    {element.elementTypeCode === 'AUDIO' && element.asset && <div className="presentation-audio"><Headphones aria-hidden="true" /><strong>{element.asset.originalFileName}</strong><audio controls preload="metadata" src={presentationAssetUrl(element.asset)} /></div>}
    {element.elementTypeCode === 'QUICK_CHECK' && element.quickCheck && <article className="presentation-quick-check">
      <small>QUICK CHECK · 1 BOD</small>
      <strong>{element.quickCheck.prompt}</strong>
      <ol>{element.quickCheck.options.map(option => <li className={option.key === element.quickCheck?.correctOptionKey ? 'is-correct' : ''} key={option.key}><span>{option.key}</span>{option.text}</li>)}</ol>
    </article>}
  </div>
}

function CanvasTable({ payload }: { readonly payload: Record<string, unknown> }) {
  const rows = Array.isArray(payload.cells) ? payload.cells as string[][] : []
  return <table><tbody>{rows.map((row, rowIndex) => <tr key={rowIndex}>{row.map((cell, cellIndex) => {
    const Cell = rowIndex === 0 && payload.headerRow ? 'th' : 'td'
    return <Cell key={cellIndex}>{cell}</Cell>
  })}</tr>)}</tbody></table>
}

function newElementPreset(type: StructuredElementType) {
  switch (type) {
    case 'TEXT': return { x: 120, y: 70, width: 1360, height: 140, payloadJson: JSON.stringify({ text: 'Novi tekst', fontSize: 52, fontWeight: 'bold', color: '#173E67', align: 'left' }) }
    case 'SHAPE': return { x: 120, y: 300, width: 360, height: 300, payloadJson: JSON.stringify({ shape: 'rectangle', fill: '#FFF3BF', stroke: '#F4B400', strokeWidth: 4 }) }
    case 'TABLE': return { x: 1100, y: 260, width: 380, height: 380, payloadJson: JSON.stringify({ cells: [['Pojam', 'Opis'], ['Primjer', 'Vrijednost']], headerRow: true }) }
    case 'RULE': return { x: 130, y: 250, width: 650, height: 330, payloadJson: JSON.stringify({ heading: 'Zapamti pravilo', body: 'Upišite jasno i kratko nastavno pravilo.', accentColor: '#F4B400' }) }
    case 'EXAMPLE': return { x: 820, y: 250, width: 650, height: 330, payloadJson: JSON.stringify({ example: 'Primjer', explanation: 'Objasnite kako se pravilo primjenjuje.', accentColor: '#1767FF' }) }
    case 'VOCABULARY': return { x: 160, y: 600, width: 560, height: 220, payloadJson: JSON.stringify({ term: 'Novi pojam', definition: 'Upišite značenje pojma.', example: 'Dodajte primjer uporabe.' }) }
    case 'QUESTION': return { x: 780, y: 600, width: 660, height: 220, payloadJson: JSON.stringify({ prompt: 'Pitanje za učenike', discussionNote: 'Napomena za vođenu raspravu.' }) }
  }
}

function parsePayload(payloadJson: string): Record<string, unknown> {
  try {
    const value: unknown = JSON.parse(payloadJson)
    return value !== null && typeof value === 'object' && !Array.isArray(value)
      ? value as Record<string, unknown>
      : {}
  } catch {
    return {}
  }
}

function elementName(type: PresentationElementType): string {
  return ({ TEXT: 'Tekst', IMAGE: 'Slika', SHAPE: 'Oblik', TABLE: 'Tablica', RULE: 'Pravilo', EXAMPLE: 'Primjer', VOCABULARY: 'Vokabular', QUESTION: 'Pitanje', VIDEO: 'Video', AUDIO: 'Audio', QUICK_CHECK: 'Quick Check' })[type]
}

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function EditorState({ text, error, retry }: { readonly text: string; readonly error?: boolean; readonly retry?: () => void }) {
  return <section className="presentation-editor-state" role={error ? 'alert' : 'status'}><strong>{text}</strong>{retry && <button className="ds-action" onClick={retry} type="button">Pokušaj ponovno</button>}<Link to="/materials">Povratak u biblioteku</Link></section>
}
