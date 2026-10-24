import {
  ArrowLeft,
  Check,
  ChevronLeft,
  ChevronRight,
  Eye,
  FileCheck2,
  GraduationCap,
  ListChecks,
  Save,
  X,
} from 'lucide-react'
import { type CSSProperties, type ReactNode, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  getPresentationReview,
  presentationAssetUrl,
  publishPresentation,
  type PresentationElement,
  type PresentationReviewWorkspace,
  type PresentationSlide,
} from './presentationApi.ts'
import './PresentationReviewPage.css'

export function PresentationReviewPage() {
  const { materialId = '' } = useParams()
  const navigate = useNavigate()
  const [workspace, setWorkspace] = useState<PresentationReviewWorkspace | null>(null)
  const [selectedSlide, setSelectedSlide] = useState(0)
  const [previewOpen, setPreviewOpen] = useState(false)
  const [loading, setLoading] = useState(true)
  const [publishing, setPublishing] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    getPresentationReview(materialId, controller.signal)
      .then(setWorkspace)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError && requestError.status === 404
          ? 'Prezentacija nije pronađena, nije vaša ili skica više nije dostupna.'
          : 'Pregled prezentacije trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [materialId])

  async function publish() {
    if (!workspace || publishing || workspace.publishBlockers.length > 0) return
    setPublishing(true)
    setError('')
    try {
      await publishPresentation(materialId, workspace)
      navigate(`/materials/${materialId}`, { replace: true })
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Materijal trenutačno nije moguće spremiti.')
      setPublishing(false)
    }
  }

  if (loading) return <ReviewState text="Učitavanje pregleda prezentacije…" />
  if (!workspace) return <ReviewState text={error || 'Prezentacija nije dostupna.'} error />

  const slides = workspace.document.slides
  const activeSlide = slides[selectedSlide] ?? slides[0]

  return <section className="presentation-review" aria-labelledby="presentation-review-title">
    <nav className="presentation-review__breadcrumb"><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}/presentation`}>Prezentacija</Link><span>›</span><strong>Pregled i spremanje</strong></nav>
    <header className="presentation-review__hero">
      <div><p>4.3 Novi materijal – Prezentacija</p><h1 id="presentation-review-title">Pregled i spremanje</h1><span>Provjerite podatke i sadržaj prije objave u svojoj biblioteci.</span></div>
      <div><Link className="ds-action ds-action--secondary" to={`/materials/${materialId}/presentation`}><ArrowLeft size={17} /> Natrag na uređivanje</Link><button className="ds-action" disabled={publishing || workspace.publishBlockers.length > 0} onClick={() => void publish()} type="button"><Save size={17} /> {publishing ? 'Spremanje…' : 'Spremi materijal'}</button></div>
    </header>

    <ol className="presentation-review__steps"><li className="is-done"><span>✓</span> Vrsta i osnovni podaci</li><li className="is-done"><span>✓</span> Ciljevi i povezanost</li><li className="is-done"><span>✓</span> Izrada prezentacije</li><li className="is-active"><span>4</span> Pregled i spremanje</li></ol>

    {error && <div className="presentation-review__notice is-error" role="alert">{error}</div>}
    {workspace.publishBlockers.length > 0 && <div className="presentation-review__notice" role="alert"><strong>Prije spremanja dovršite:</strong><ul>{workspace.publishBlockers.map(blocker => <li key={blocker}>{blocker}</li>)}</ul></div>}

    <div className="presentation-review__layout">
      <div className="presentation-review__main">
        <article className="presentation-review__card presentation-review__summary">
          <header><div><FileCheck2 /><span><small>Naziv materijala</small><h2>{workspace.title}</h2></span></div><span className="presentation-review__status"><Check size={15} /> Skica spremna</span></header>
          <dl><div><dt>Vrsta</dt><dd>Prezentacija</dd></div><div><dt>Predmet</dt><dd>{workspace.subject || 'Nije navedeno'}</dd></div><div><dt>Jezik</dt><dd>{workspace.languageCode || 'Nije navedeno'}</dd></div><div><dt>Vidljivost</dt><dd>{workspace.visibility === 'Private' ? 'Privatno' : 'Podijeljeno'}</dd></div><div><dt>Program</dt><dd>{workspace.programName || 'Nije povezano'}</dd></div><div><dt>Razred / razina</dt><dd>{[workspace.schoolGradeName, workspace.proficiencyLevelName].filter(Boolean).join(' · ') || 'Nije navedeno'}</dd></div></dl>
          {workspace.description && <p>{workspace.description}</p>}
        </article>

        <article className="presentation-review__card">
          <header><div><GraduationCap /><span><small>Pedagoška povezanost</small><h2>Ciljevi i ishodi</h2></span></div></header>
          <div className="presentation-review__pedagogy"><section><h3>Cilj učenja</h3><p>{workspace.learningGoal || 'Cilj učenja nije naveden.'}</p></section><section><h3>Komponente znanja</h3>{workspace.knowledgeComponents.length > 0 ? <ul>{workspace.knowledgeComponents.map(component => <li key={component.id}>{component.name}<small>{component.knowledgeAreaName} · {component.knowledgeModelCode} {component.knowledgeModelVersion}</small></li>)}</ul> : <p>Nema povezanih komponenti.</p>}</section><section><h3>Ishodi kurikuluma</h3>{workspace.curriculumOutcomes.length > 0 ? <ul>{workspace.curriculumOutcomes.map(outcome => <li key={outcome.id}>{outcome.officialCode && <strong>{outcome.officialCode}</strong>} {outcome.title}</li>)}</ul> : <p>Nema povezanih ishoda.</p>}</section></div>
          {workspace.tags.length > 0 && <div className="presentation-review__tags">{workspace.tags.map(tag => <span key={tag}>{tag}</span>)}</div>}
        </article>

        <article className="presentation-review__card presentation-review__preview">
          <header><div><Eye /><span><small>Stvarni spremljeni sadržaj</small><h2>Pregled prezentacije</h2></span></div><button className="ds-action ds-action--secondary" onClick={() => setPreviewOpen(true)} type="button"><Eye size={16} /> Pogledaj cijelu prezentaciju</button></header>
          <SlideCanvas slide={activeSlide} />
          <div className="presentation-review__thumbnails">{slides.map((slide, index) => <button className={index === selectedSlide ? 'is-selected' : ''} key={slide.id} onClick={() => setSelectedSlide(index)} type="button"><span>{index + 1}</span><SlideCanvas slide={slide} compact /></button>)}</div>
        </article>
      </div>

      <aside className="presentation-review__side">
        <article className="presentation-review__card"><header><div><ListChecks /><span><small>Sažetak sadržaja</small><h2>Prezentacija</h2></span></div></header><dl className="presentation-review__counts"><div><dt>{slides.length}</dt><dd>slajdova</dd></div><div><dt>{workspace.knowledgeComponents.length}</dt><dd>komponenti znanja</dd></div><div><dt>{workspace.curriculumOutcomes.length}</dt><dd>ishoda</dd></div><div><dt>{workspace.quickCheckCount}</dt><dd>Quick Checka</dd></div></dl></article>
        <article className="presentation-review__ready"><Check size={22} /><div><strong>Spremno za biblioteku</strong><p>Spremanjem nastaje nepromjenjiva verzija prezentacije. Materijal će biti dostupan u vašoj biblioteci.</p></div></article>
      </aside>
    </div>

    {previewOpen && <div className="presentation-preview-modal" role="dialog" aria-modal="true" aria-label="Cijela prezentacija"><div><header><strong>{workspace.title}</strong><button aria-label="Zatvori pregled" onClick={() => setPreviewOpen(false)} type="button"><X /></button></header><SlideCanvas slide={activeSlide} /><footer><button disabled={selectedSlide === 0} onClick={() => setSelectedSlide(value => value - 1)} type="button"><ChevronLeft /> Prethodni</button><span>{selectedSlide + 1} / {slides.length}</span><button disabled={selectedSlide >= slides.length - 1} onClick={() => setSelectedSlide(value => value + 1)} type="button">Sljedeći <ChevronRight /></button></footer></div></div>}
  </section>
}

function SlideCanvas({ slide, compact = false }: { slide: PresentationSlide, compact?: boolean }) {
  return <div className={`presentation-review-canvas${compact ? ' is-compact' : ''}`} aria-label={`Slajd ${slide.sortOrder + 1}`}>
    {slide.elements.length === 0 && <div className="presentation-review-canvas__empty">Prazan slajd</div>}
    {slide.elements.map(element => <ReviewElement element={element} key={element.id} />)}
  </div>
}

function ReviewElement({ element }: { element: PresentationElement }) {
  const payload = parsePayload(element.payloadJson)
  const style = {
    left: `${element.x / 16}%`,
    top: `${element.y / 9}%`,
    width: `${element.width / 16}%`,
    height: `${element.height / 9}%`,
    zIndex: element.zIndex,
  } satisfies CSSProperties
  let content: ReactNode
  if (element.elementTypeCode === 'IMAGE' && element.asset) {
    content = <img alt={text(payload.altText)} src={presentationAssetUrl(element.asset)} />
  } else if (element.elementTypeCode === 'QUICK_CHECK' && element.quickCheck) {
    content = <div className="presentation-review-element__card"><small>QUICK CHECK</small><strong>{element.quickCheck.prompt}</strong></div>
  } else if (element.elementTypeCode === 'VIDEO' || element.elementTypeCode === 'AUDIO') {
    content = <div className="presentation-review-element__media"><strong>{element.elementTypeCode === 'VIDEO' ? 'Video' : 'Audio'}</strong><span>{element.asset?.originalFileName}</span></div>
  } else {
    content = <div className={`presentation-review-element__card is-${element.elementTypeCode.toLowerCase()}`}><small>{element.elementTypeCode}</small><strong>{text(payload.text) || text(payload.title) || text(payload.rule) || text(payload.term) || 'Nastavni sadržaj'}</strong><span>{text(payload.body) || text(payload.example) || text(payload.definition) || text(payload.question)}</span></div>
  }
  return <div className="presentation-review-element" style={style}>{content}</div>
}

function parsePayload(value: string): Record<string, unknown> {
  try { return JSON.parse(value) as Record<string, unknown> } catch { return {} }
}

function text(value: unknown): string { return typeof value === 'string' ? value : '' }

function ReviewState({ text: value, error = false }: { text: string, error?: boolean }) {
  return <section className={`presentation-review-state${error ? ' is-error' : ''}`}><FileCheck2 size={42} /><p>{value}</p></section>
}
