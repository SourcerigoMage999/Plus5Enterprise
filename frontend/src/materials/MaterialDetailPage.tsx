import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { getMaterial, type MaterialDetail, type MaterialFileFormat } from './materialsApi.ts'
import './MaterialDetailPage.css'

export function MaterialDetailPage() {
  const { materialId = '' } = useParams()
  const [material, setMaterial] = useState<MaterialDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getMaterial(materialId, controller.signal)
      .then(setMaterial)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError && requestError.status === 404
          ? 'Materijal nije pronađen ili mu nemate pristup.'
          : 'Detalj materijala trenutačno nije moguće učitati.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [materialId, revision])

  if (loading) return <MaterialDetailState message="Učitavanje detalja materijala…" />
  if (error) return <MaterialDetailState message={error} error onRetry={() => {
    setLoading(true)
    setError(null)
    setMaterial(null)
    setRevision(value => value + 1)
  }} />
  if (!material) return null
  return <MaterialDetailContent material={material} />
}

function MaterialDetailContent({ material }: { readonly material: MaterialDetail }) {
  const [copyState, setCopyState] = useState<'idle' | 'copied' | 'failed'>('idle')

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.href)
      setCopyState('copied')
    } catch {
      setCopyState('failed')
    }
  }

  return <section className="material-detail" aria-labelledby="material-detail-title">
    <nav className="material-detail__breadcrumb" aria-label="Putanja"><Link to="/materials">Materijali</Link><span>›</span><strong>Pregled materijala</strong></nav>
    <header className="material-detail__heading">
      <div><h1 id="material-detail-title">4.2 Pregled materijala</h1><p>Pregled sadržaja, metapodataka i povezanosti s učenjem.</p></div>
      <div className="material-detail__heading-actions">
        {material.isOwner ? <Link className="material-detail__edit-link" to={`/materials/${material.id}/edit`}>✎ Uredi</Link> : <button disabled title="Samo vlasnik može uređivati materijal" type="button">✎ Uredi</button>}
        <button disabled title="Povezivanje s pripremom sata dolazi u Phase 9" type="button">＋ Dodaj u pripremu</button>
        <button disabled title="Dodatne write akcije još nemaju zaključan contract" type="button" aria-label="Više akcija">•••</button>
      </div>
    </header>

    <section className="material-detail__identity ds-card" aria-label="Sažetak materijala">
      <FormatPreview format={material.file.format} />
      <div className="material-detail__identity-copy">
        <div className="material-detail__badges"><span>{materialTypeLabel(material.materialTypeCode)}</span><span>v{material.versionNumber}</span>{!material.isOwner && <span>Dijeljeno · {material.shareAccess === 'use' ? 'korištenje' : 'pregled'}</span>}</div>
        <h2>{material.title}</h2>
        <p>{material.description ?? 'Opis materijala nije unesen.'}</p>
        <dl><Fact label="Program" value={material.program?.name ?? 'Nije povezano'} /><Fact label="Razred" value={material.schoolGrade?.code ?? material.schoolGrade?.name ?? 'Nije povezano'} /><Fact label="Predmet" value={material.subject ?? 'Nije uneseno'} /><Fact label="Razina" value={material.proficiencyLevel?.code ?? material.proficiencyLevel?.name ?? 'Nije povezano'} /></dl>
      </div>
      <div className="material-detail__file-actions">
        <button aria-label="Otvori" disabled title="Sigurno otvaranje zahtijeva storage adapter i kratkotrajni access URL" type="button">↗ Otvori</button>
        <button aria-label="Preuzmi" disabled title="Sigurno preuzimanje zahtijeva storage adapter i kratkotrajni access URL" type="button">⇩ Preuzmi</button>
        <button aria-label="Prezentiraj" disabled title="Presentation runtime još nema zaključan contract" type="button">▷ Prezentiraj</button>
        <button aria-label="Kopiraj link" onClick={copyLink} type="button">{copyState === 'copied' ? '✓ Link kopiran' : copyState === 'failed' ? 'Link nije moguće kopirati' : '⌁ Kopiraj link'}</button>
      </div>
    </section>

    <div className="material-detail__layout">
      <main className="material-detail__main">
        <section className="material-detail__card material-detail__preview-card">
          <header><h2>Pregled sadržaja</h2><span>{material.file.format.toUpperCase()}</span></header>
          <div className={`material-detail__document material-detail__document--${material.file.format}`}><div className="material-detail__document-mark" aria-hidden="true">{formatMark(material.file.format)}</div><strong>{material.file.originalFileName}</strong><p>Siguran inline preview aktivirat će se tek uz storage-read adapter. Metapodaci su stvarni; sadržaj datoteke nije simuliran.</p><span>{formatBytes(material.file.sizeBytes)}</span></div>
        </section>
        <section className="material-detail__card">
          <h2>Detalji materijala</h2>
          <dl className="material-detail__details"><Fact label="Naziv datoteke" value={material.file.originalFileName} /><Fact label="Format" value={material.file.format.toUpperCase()} /><Fact label="Veličina" value={formatBytes(material.file.sizeBytes)} /><Fact label="Jezik" value={material.languageCode ?? 'Nije uneseno'} /><Fact label="Dodano" value={formatDate(material.addedAtUtc)} /><Fact label="Verzija" value={String(material.versionNumber)} /></dl>
          <div className="material-detail__tags"><strong>Oznake</strong>{material.tags.length ? <div>{material.tags.map(tag => <span key={tag}>{tag}</span>)}</div> : <p>Nema slobodnih oznaka.</p>}</div>
        </section>
        <section className="material-detail__card">
          <h2>Standardi i kurikulum</h2>
          {material.curriculumOutcomes.length ? <ul className="material-detail__outcomes">{material.curriculumOutcomes.map(outcome => <li key={outcome.id}><span>{outcome.officialCode ?? outcome.curriculumCode}</span><div><strong>{outcome.title}</strong><small>{outcome.curriculumName} · {outcome.curriculumVersion}</small></div></li>)}</ul> : <EmptyCopy title="Nema povezanih ishoda" text="Ova verzija materijala nema snapshot povezanosti s kurikulumskim ishodima." />}
        </section>
        <section className="material-detail__card material-detail__tasks" aria-labelledby="material-tasks-title">
          <header><div><h2 id="material-tasks-title">◇ Zadaci i procjena</h2><p>Svaki procjenjivi zadatak ima vlastiti verzionirani metadata snapshot. Sam materijal nije automatski dokaz znanja.</p></div><span>{material.tasks.length} {material.tasks.length === 1 ? 'zadatak' : 'zadataka'}</span></header>
          {material.tasks.length ? <ol>{material.tasks.map((task, index) => <li key={task.versionId}>
            <div className="material-detail__task-heading"><span>Zadatak {index + 1}</span><div><strong>{taskTypeLabel(task.taskTypeCode)}</strong><small>Task v{task.versionNumber}</small></div></div>
            <p>{task.prompt}</p>
            <div className="material-detail__task-badges"><span>Težina {task.difficulty}/5</span><span>{evidenceTypeLabel(task.evidenceType)}</span><span>{formatPoints(task.maxPoints)}</span></div>
            <div className="material-detail__task-components">{task.knowledgeComponents.map(component => <span key={component.id}>{component.knowledgeAreaName} → {component.name}<small>{component.knowledgeModelCode} · {component.knowledgeModelVersion}</small></span>)}</div>
            <dl><Fact label="Točan odgovor" value={task.correctAnswer ?? 'Nije primjenjivo'} /><Fact label="Kriterij vrednovanja" value={task.evaluationCriterion ?? 'Nije primjenjivo'} /></dl>
          </li>)}</ol> : <EmptyCopy title="Nema procjenjivih zadataka" text="Ova verzija materijala ne sadrži strukturirani Task metadata i samo korištenje materijala ne stvara Evidence Event." />}
        </section>
      </main>
      <aside className="material-detail__aside" aria-label="Povezanost s učenjem i korištenje">
        <section className="material-detail__card material-detail__goal"><h2>◎ Cilj učenja</h2><p>{material.learningGoal ?? 'Cilj učenja nije unesen za ovu verziju materijala.'}</p></section>
        <section className="material-detail__card"><h2>⌘ Knowledge Components</h2>{material.knowledgeComponents.length ? <ul className="material-detail__components">{material.knowledgeComponents.map(component => <li key={component.id}><span>{component.knowledgeAreaName}</span><strong>{component.name}</strong><small>{component.knowledgeModelCode} · {component.knowledgeModelVersion} · {component.knowledgeModelStatus === 'retired' ? 'povijesni model' : 'objavljen model'}</small></li>)}</ul> : <EmptyCopy title="Nema povezanih komponenti" text="Ova verzija materijala nema Knowledge Component mapping." />}</section>
        <section className="material-detail__card material-detail__future"><h2>↗ Korištenje</h2><p>Broj korištenja, povezanost sa satovima i readiness utjecaj nisu dostupni bez zaključanog Lesson/usage contracta.</p></section>
      </aside>
    </div>
  </section>
}

function MaterialDetailState({ message, error = false, onRetry }: { readonly message: string; readonly error?: boolean; readonly onRetry?: () => void }) { return <section className={`material-detail-state${error ? ' material-detail-state--error' : ''}`} role={error ? 'alert' : 'status'}><strong>{message}</strong>{onRetry && <button onClick={onRetry} type="button">Pokušaj ponovno</button>}<Link to="/materials">Povratak na materijale</Link></section> }
function FormatPreview({ format }: { readonly format: MaterialFileFormat }) { return <div className={`material-detail__format material-detail__format--${format}`} aria-hidden="true"><span>{formatMark(format)}</span><small>{format.toUpperCase()}</small></div> }
function Fact({ label, value }: { readonly label: string; readonly value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function EmptyCopy({ title, text }: { readonly title: string; readonly text: string }) { return <div className="material-detail__empty"><strong>{title}</strong><p>{text}</p></div> }
function formatMark(format: MaterialFileFormat) { if (format === 'pptx') return '▤'; if (format === 'mp4') return '▶'; if (format === 'zip') return '▱'; if (format === 'docx') return '▧'; return '▥' }
function materialTypeLabel(code: string) { const labels: Record<string, string> = { PRESENTATION: 'Prezentacija', WORKSHEET: 'Radni list', CONVERSATION_CARDS: 'Kartice za razgovor', INTERACTIVE_EXERCISE: 'Interaktivna vježba', VIDEO: 'Video', AUDIO: 'Audio', QUIZ: 'Kviz', IMAGE: 'Slika', POSTER: 'Plakat', MAP: 'Mapa' }; return labels[code] ?? code.toLocaleLowerCase('hr').replaceAll('_', ' ') }
function formatBytes(value: number) { if (value < 1024) return `${value} B`; if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`; return `${(value / (1024 * 1024)).toFixed(1)} MB` }
function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) }
function taskTypeLabel(code: string) { return code.toLocaleLowerCase('hr').replaceAll('_', ' ') }
function evidenceTypeLabel(value: MaterialDetail['tasks'][number]['evidenceType']) { const labels = { recognition: 'Prepoznavanje', understanding: 'Razumijevanje', application: 'Primjena', production: 'Produkcija' }; return labels[value] }
function formatPoints(value: number) { return `${new Intl.NumberFormat('hr-HR', { maximumFractionDigits: 4 }).format(value)} ${value === 1 ? 'bod' : 'bodova'}` }
