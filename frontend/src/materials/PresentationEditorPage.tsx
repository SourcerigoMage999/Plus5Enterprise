import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  Copy,
  FilePlus2,
  LayoutTemplate,
  Palette,
  Plus,
  Redo2,
  Save,
  Trash2,
  Undo2,
} from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  addPresentationSlide,
  deletePresentationSlide,
  duplicatePresentationSlide,
  getPresentationEditor,
  initializePresentation,
  reorderPresentationSlides,
  type PresentationEditorWorkspace,
} from './presentationApi.ts'
import './PresentationEditorPage.css'

export function PresentationEditorPage() {
  const { materialId = '' } = useParams()
  const [workspace, setWorkspace] = useState<PresentationEditorWorkspace | null>(null)
  const [selectedSlideId, setSelectedSlideId] = useState<string | null>(null)
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

  async function mutate(action: () => Promise<unknown>, successMessage: string) {
    if (busy) return
    setBusy(true); setError(''); setNotice('')
    try {
      await action()
      setNotice(successMessage)
      reload()
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Promjenu slajda trenutačno nije moguće spremiti.')
    } finally {
      setBusy(false)
    }
  }

  function initialize() {
    if (!workspace) return
    void mutate(
      () => initializePresentation(
        materialId,
        workspace.materialVersionId,
        workspace.materialRowVersion),
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
      () => duplicatePresentationSlide(
        materialId,
        selectedSlideId,
        document.rowVersion),
      'Slajd je dupliciran.',
    )
  }

  function removeSlide() {
    if (!document || !selectedSlideId || slides.length <= 1) return
    if (!window.confirm('Obrisati odabrani prazni slajd?')) return
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
        <span>Stvorit će se dokument 16:9 s jednim praznim slajdom u skici v{workspace.materialVersionNumber}. Sadržajni elementi dolaze u sljedećoj fazi.</span>
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
    <div className="presentation-editor__intro"><h2>3. Izrada prezentacije</h2><p>Dodajte, duplicirajte, obrišite i posložite prazne slajdove.</p></div>
    {(error || notice) && <div className={`presentation-editor__notice${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'}>{error || notice}</div>}

    <div className="presentation-editor__workspace" aria-busy={busy}>
      <div className="presentation-editor__toolbar" role="toolbar" aria-label="Akcije slajda">
        <button disabled={busy || slides.length >= 200} onClick={addSlide} type="button"><Plus size={18} /> Dodaj slajd</button>
        <button disabled={busy || !selectedSlideId || slides.length >= 200} onClick={duplicateSlide} type="button"><Copy size={17} /> Dupliciraj</button>
        <button disabled={busy || !selectedSlideId || slides.length <= 1} onClick={removeSlide} type="button"><Trash2 size={17} /> Obriši</button>
        <span />
        <button disabled title="Undo dolazi s autosave/version safety fazom." type="button"><Undo2 size={17} /> Poništi</button>
        <button disabled title="Redo dolazi s autosave/version safety fazom." type="button"><Redo2 size={17} /> Ponovi</button>
        <button disabled title="Tema dolazi s core content elementima." type="button"><Palette size={17} /> Tema</button>
        <button disabled title="Raspored sadržaja dolazi u sljedećoj fazi." type="button"><LayoutTemplate size={17} /> Raspored</button>
      </div>

      <aside className="presentation-editor__slides" aria-label="Slajdovi prezentacije">
        <ol>{slides.map((slide, index) => <li key={slide.id}>
          <button className={slide.id === selectedSlideId ? 'is-selected' : ''} onClick={() => setSelectedSlideId(slide.id)} type="button" aria-current={slide.id === selectedSlideId ? 'true' : undefined}>
            <span>{index + 1}</span><div><FilePlus2 size={23} /><small>Prazan slajd</small></div>
          </button>
        </li>)}</ol>
        <div className="presentation-editor__order">
          <button aria-label="Pomakni slajd gore" disabled={busy || selectedIndex <= 0} onClick={() => moveSlide(-1)} type="button"><ArrowUp /></button>
          <button aria-label="Pomakni slajd dolje" disabled={busy || selectedIndex < 0 || selectedIndex === slides.length - 1} onClick={() => moveSlide(1)} type="button"><ArrowDown /></button>
        </div>
        <button className="presentation-editor__add" disabled={busy || slides.length >= 200} onClick={addSlide} type="button"><Plus size={17} /> Dodaj slajd</button>
      </aside>

      <main className="presentation-editor__stage">
        <div className="presentation-editor__canvas">
          <div><FilePlus2 size={46} /><h2>Prazan slajd {Math.max(1, selectedIndex + 1)}</h2><p>Tekst, slike, oblici i tablice dolaze u Phase 7.3.</p></div>
        </div>
        <footer><button disabled type="button"><ArrowLeft size={16} /> Prethodni</button><span>{Math.max(1, selectedIndex + 1)} / {slides.length}</span><button disabled type="button">Sljedeći <ArrowRight size={16} /></button></footer>
      </main>

      <aside className="presentation-editor__properties" aria-label="Postavke slajda">
        <div role="tablist"><button className="is-active" role="tab" aria-selected="true">Slajd</button><button disabled role="tab" aria-selected="false">Element</button><button disabled role="tab" aria-selected="false">Stranica</button></div>
        <section><h2>Raspored slajda</h2><select aria-label="Raspored slajda" disabled><option>Prazan slajd</option></select><button disabled type="button">Promijeni raspored</button></section>
        <section><h2>Pozadina</h2><div><span /><code>#FFFFFF</code><button disabled type="button">Promijeni</button></div></section>
        <p><strong>Phase 7.2</strong> sprema strukturu i redoslijed praznih slajdova. Sadržaj se ne simulira prije zaključanih handlera.</p>
      </aside>
    </div>
  </section>
}

function EditorState({ text, error, retry }: { readonly text: string; readonly error?: boolean; readonly retry?: () => void }) {
  return <section className="presentation-editor-state" role={error ? 'alert' : 'status'}><strong>{text}</strong>{retry && <button className="ds-action" onClick={retry} type="button">Pokušaj ponovno</button>}<Link to="/materials">Povratak u biblioteku</Link></section>
}
