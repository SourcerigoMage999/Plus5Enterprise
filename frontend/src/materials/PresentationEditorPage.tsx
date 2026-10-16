import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  Copy,
  FilePlus2,
  Image as ImageIcon,
  LayoutTemplate,
  Palette,
  Plus,
  Redo2,
  Save,
  Shapes,
  Table2,
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
  deletePresentationElement,
  deletePresentationSlide,
  duplicatePresentationSlide,
  getPresentationEditor,
  initializePresentation,
  presentationAssetUrl,
  reorderPresentationSlides,
  updatePresentationElement,
  uploadPresentationImage,
  type PresentationEditorWorkspace,
  type PresentationElement,
  type PresentationElementType,
} from './presentationApi.ts'
import './PresentationEditorPage.css'

type CoreElementType = Exclude<PresentationElementType, 'IMAGE'>

export function PresentationEditorPage() {
  const { materialId = '' } = useParams()
  const imageInput = useRef<HTMLInputElement>(null)
  const [workspace, setWorkspace] = useState<PresentationEditorWorkspace | null>(null)
  const [selectedSlideId, setSelectedSlideId] = useState<string | null>(null)
  const [selectedElementId, setSelectedElementId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [revision, setRevision] = useState(0)

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

  function addElement(type: CoreElementType) {
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

  function saveElement(element: PresentationElement, values: ElementFormValues) {
    if (!document || !selectedSlide) return
    void mutate(
      () => updatePresentationElement(materialId, selectedSlide.id, element.id, {
        expectedDocumentRowVersion: document.rowVersion,
        zIndex: values.zIndex,
        x: values.x,
        y: values.y,
        width: values.width,
        height: values.height,
        payloadJson: JSON.stringify(values.payload),
      }),
      'Element je spremljen.',
    )
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

  return <section className="presentation-editor" aria-labelledby="presentation-editor-title">
    <nav className="presentation-editor__breadcrumb"><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}/edit`}>Uredi materijal</Link><span>›</span><strong>Prezentacija</strong></nav>
    <header className="presentation-editor__hero">
      <div><h1 id="presentation-editor-title">4.3 Novi materijal – Prezentacija</h1><p>{workspace.title} · skica v{workspace.materialVersionNumber}</p></div>
      <div><Link className="ds-action ds-action--secondary" to={`/materials/${materialId}/edit`}>Odustani</Link><button className="ds-action ds-action--secondary" disabled type="button"><Save size={17} /> Spremi nacrt</button><button className="ds-action" disabled type="button">Nastavi <ArrowRight size={17} /></button></div>
    </header>

    <ol className="presentation-editor__steps"><li className="is-done"><span>✓</span> Vrsta i osnovni podaci</li><li className="is-done"><span>✓</span> Ciljevi i povezanost</li><li className="is-active"><span>3</span> Izrada prezentacije</li><li><span>4</span> Pregled i spremanje</li></ol>
    <div className="presentation-editor__intro"><h2>3. Izrada prezentacije</h2><p>Dodajte tekst, sigurnu sliku, oblik ili tablicu i uredite ih na 16:9 platnu.</p></div>
    {(error || notice) && <div className={`presentation-editor__notice${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'}>{error || notice}</div>}

    <div className="presentation-editor__workspace" aria-busy={busy}>
      <div className="presentation-editor__toolbar" role="toolbar" aria-label="Alati prezentacije">
        <button disabled={busy} onClick={() => addElement('TEXT')} type="button"><Type size={18} /> Tekst</button>
        <button disabled={busy} onClick={() => imageInput.current?.click()} type="button"><ImageIcon size={18} /> Slika</button>
        <input ref={imageInput} accept="image/png,image/jpeg,image/webp" hidden type="file" onChange={event => { uploadImage(event.target.files?.[0]); event.target.value = '' }} />
        <button disabled={busy} onClick={() => addElement('SHAPE')} type="button"><Shapes size={18} /> Oblik</button>
        <button disabled={busy} onClick={() => addElement('TABLE')} type="button"><Table2 size={18} /> Tablica</button>
        <span />
        <button disabled title="Undo dolazi s autosave/version safety fazom." type="button"><Undo2 size={17} /> Poništi</button>
        <button disabled title="Redo dolazi s autosave/version safety fazom." type="button"><Redo2 size={17} /> Ponovi</button>
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
        <button className="presentation-editor__add" disabled={busy || slides.length >= 200} onClick={addSlide} type="button"><Plus size={17} /> Dodaj slajd</button>
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
          ? <ElementProperties key={`${selectedElement.id}-${document.rowVersion}`} element={selectedElement} busy={busy} count={selectedSlide?.elements.length ?? 0} onDelete={removeElement} onSave={saveElement} />
          : <><section><h2>Raspored slajda</h2><select aria-label="Raspored slajda" disabled><option>Prazan slajd</option></select><button disabled type="button">Promijeni raspored</button></section><section><h2>Pozadina</h2><div><span /><code>#FFFFFF</code><button disabled type="button">Promijeni</button></div></section><p><strong>Phase 7.3</strong> Osnovni elementi spremaju se strukturirano. Quick Check, nastavni elementi, autosave i objava dolaze u kasnijim fazama.</p></>}
      </aside>
    </div>

    <div className="presentation-editor__slide-actions">
      <button disabled={busy || slides.length >= 200} onClick={duplicateSlide} type="button"><Copy size={17} /> Dupliciraj odabrani slajd</button>
      <button disabled={busy || slides.length <= 1} onClick={removeSlide} type="button"><Trash2 size={17} /> Obriši odabrani slajd</button>
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
}

function ElementProperties({ element, busy, count, onDelete, onSave }: {
  readonly element: PresentationElement
  readonly busy: boolean
  readonly count: number
  readonly onDelete: () => void
  readonly onSave: (element: PresentationElement, values: ElementFormValues) => void
}) {
  const [values, setValues] = useState<ElementFormValues>({
    zIndex: element.zIndex,
    x: element.x,
    y: element.y,
    width: element.width,
    height: element.height,
    payload: parsePayload(element.payloadJson),
  })
  const updateNumber = (name: keyof Omit<ElementFormValues, 'payload'>, value: string) => setValues(current => ({ ...current, [name]: Number(value) }))
  const updatePayload = (name: string, value: unknown) => setValues(current => ({ ...current, payload: { ...current.payload, [name]: value } }))

  return <form className="presentation-element-form" onSubmit={event => { event.preventDefault(); onSave(element, values) }}>
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
    <fieldset><legend>Položaj i veličina</legend>
      <label>X<input min="0" max="1599" step="1" type="number" value={values.x} onChange={event => updateNumber('x', event.target.value)} /></label>
      <label>Y<input min="0" max="899" step="1" type="number" value={values.y} onChange={event => updateNumber('y', event.target.value)} /></label>
      <label>Širina<input min="1" max="1600" step="1" type="number" value={values.width} onChange={event => updateNumber('width', event.target.value)} /></label>
      <label>Visina<input min="1" max="900" step="1" type="number" value={values.height} onChange={event => updateNumber('height', event.target.value)} /></label>
      <label>Sloj<input min="0" max={Math.max(0, count - 1)} type="number" value={values.zIndex} onChange={event => updateNumber('zIndex', event.target.value)} /></label>
    </fieldset>
    <button className="ds-action" disabled={busy} type="submit"><Save size={17} /> Spremi element</button>
  </form>
}

function TableFields({ payload, onChange }: { readonly payload: Record<string, unknown>; readonly onChange: (name: string, value: unknown) => void }) {
  const cells = Array.isArray(payload.cells) ? payload.cells as string[][] : [['Pojam', 'Opis'], ['Primjer', 'Vrijednost']]
  const text = cells.map(row => row.join(' | ')).join('\n')
  return <>
    <label>Ćelije<textarea value={text} onChange={event => onChange('cells', event.target.value.split('\n').filter(Boolean).slice(0, 20).map(row => row.split('|').slice(0, 10).map(cell => cell.trim().slice(0, 500))))} /><small>Redovi su u novom retku, ćelije odvojite znakom |.</small></label>
    <label className="presentation-element-form__check"><input type="checkbox" checked={Boolean(payload.headerRow)} onChange={event => onChange('headerRow', event.target.checked)} /> Prvi red je zaglavlje</label>
  </>
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
  return <button className={`presentation-canvas-element presentation-canvas-element--${element.elementTypeCode.toLowerCase()}${selected ? ' is-selected' : ''}`} onClick={event => { event.stopPropagation(); onSelect() }} style={style} type="button">
    {element.elementTypeCode === 'TEXT' && <span style={{ color: String(payload.color), fontSize: `${Number(payload.fontSize) / 16}cqw`, fontWeight: payload.fontWeight === 'bold' ? 800 : 400, textAlign: String(payload.align) as 'left' | 'center' | 'right' }}>{String(payload.text ?? '')}</span>}
    {element.elementTypeCode === 'IMAGE' && element.asset && <img alt={String(payload.altText ?? '')} src={presentationAssetUrl(element.asset)} style={{ objectFit: String(payload.fit) as 'contain' | 'cover' }} />}
    {element.elementTypeCode === 'SHAPE' && <span style={{ background: String(payload.fill), borderColor: String(payload.stroke), borderRadius: payload.shape === 'ellipse' ? '50%' : payload.shape === 'line' ? '0' : '.4rem', borderWidth: payload.shape === 'line' ? '0 0 3px' : `${Number(payload.strokeWidth)}px` }} />}
    {element.elementTypeCode === 'TABLE' && <CanvasTable payload={payload} />}
  </button>
}

function CanvasTable({ payload }: { readonly payload: Record<string, unknown> }) {
  const rows = Array.isArray(payload.cells) ? payload.cells as string[][] : []
  return <table><tbody>{rows.map((row, rowIndex) => <tr key={rowIndex}>{row.map((cell, cellIndex) => {
    const Cell = rowIndex === 0 && payload.headerRow ? 'th' : 'td'
    return <Cell key={cellIndex}>{cell}</Cell>
  })}</tr>)}</tbody></table>
}

function newElementPreset(type: CoreElementType) {
  switch (type) {
    case 'TEXT': return { x: 120, y: 70, width: 1360, height: 140, payloadJson: JSON.stringify({ text: 'Novi tekst', fontSize: 52, fontWeight: 'bold', color: '#173E67', align: 'left' }) }
    case 'SHAPE': return { x: 120, y: 300, width: 360, height: 300, payloadJson: JSON.stringify({ shape: 'rectangle', fill: '#FFF3BF', stroke: '#F4B400', strokeWidth: 4 }) }
    case 'TABLE': return { x: 1100, y: 260, width: 380, height: 380, payloadJson: JSON.stringify({ cells: [['Pojam', 'Opis'], ['Primjer', 'Vrijednost']], headerRow: true }) }
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
  return ({ TEXT: 'Tekst', IMAGE: 'Slika', SHAPE: 'Oblik', TABLE: 'Tablica' })[type]
}

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function EditorState({ text, error, retry }: { readonly text: string; readonly error?: boolean; readonly retry?: () => void }) {
  return <section className="presentation-editor-state" role={error ? 'alert' : 'status'}><strong>{text}</strong>{retry && <button className="ds-action" onClick={retry} type="button">Pokušaj ponovno</button>}<Link to="/materials">Povratak u biblioteku</Link></section>
}
