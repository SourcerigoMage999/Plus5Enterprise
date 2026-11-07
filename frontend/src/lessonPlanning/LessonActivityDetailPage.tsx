import { useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  createLessonActivityAiSuggestion,
  deleteLessonActivity,
  duplicateLessonActivity,
  getLessonActivityDetail,
  resolveLessonActivityAiSuggestion,
  updateLessonActivity,
  updateLessonActivityKnowledge,
  type LessonActivityAiSuggestion,
  type LessonActivityDetail,
} from './lessonPlanningApi.ts'
import './LessonActivityDetailPage.css'

interface FormState {
  name: string; purpose: string; durationMinutes: number; workMode: 'Individual' | 'Pair' | 'Group'
  description: string; purposeDescription: string; teacherInstructions: string; studentInstructions: string; notes: string
  showTeacherInstructionsOnBoard: boolean; showStudentInstructionsOnBoard: boolean; showProgressOnBoard: boolean
  knowledgeComponentIds: string[]
}

export function LessonActivityDetailPage() {
  const { lessonPlanId = '', activityId = '' } = useParams()
  const navigate = useNavigate()
  const [detail, setDetail] = useState<LessonActivityDetail | null>(null)
  const [form, setForm] = useState<FormState | null>(null)
  const [baseline, setBaseline] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [suggestion, setSuggestion] = useState<LessonActivityAiSuggestion | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getLessonActivityDetail(lessonPlanId, activityId, controller.signal)
      .then(value => { const next = toForm(value); setDetail(value); setForm(next); setBaseline(JSON.stringify(next)) })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError('Aktivnost trenutačno nije moguće učitati.')
      })
    return () => controller.abort()
  }, [lessonPlanId, activityId])

  const dirty = useMemo(() => form !== null && JSON.stringify(form) !== baseline, [form, baseline])
  useEffect(() => {
    const handler = (event: BeforeUnloadEvent) => { if (dirty) event.preventDefault() }
    window.addEventListener('beforeunload', handler); return () => window.removeEventListener('beforeunload', handler)
  }, [dirty])

  function change<K extends keyof FormState>(key: K, value: FormState[K]) { setForm(current => current ? { ...current, [key]: value } : current) }
  function cancel() { if (!dirty || window.confirm('Odbaciti nespremljene promjene?')) navigate(`/lesson-plans/${lessonPlanId}/editor`) }

  async function save() {
    if (!detail || !form || saving) return
    setSaving(true); setError(null)
    try {
      let updated = await updateLessonActivity(lessonPlanId, activityId, {
        rowVersion: detail.rowVersion, name: form.name, purpose: form.purpose, durationMinutes: form.durationMinutes,
        workMode: form.workMode, description: empty(form.description), purposeDescription: empty(form.purposeDescription),
        teacherInstructions: empty(form.teacherInstructions), studentInstructions: empty(form.studentInstructions), notes: empty(form.notes),
        showTeacherInstructionsOnBoard: form.showTeacherInstructionsOnBoard,
        showStudentInstructionsOnBoard: form.showStudentInstructionsOnBoard, showProgressOnBoard: form.showProgressOnBoard,
      })
      if (JSON.stringify([...updated.selectedKnowledgeComponentIds].sort()) !== JSON.stringify([...form.knowledgeComponentIds].sort()))
        updated = await updateLessonActivityKnowledge(lessonPlanId, activityId, updated.rowVersion, form.knowledgeComponentIds)
      const next = toForm(updated); setDetail(updated); setForm(next); setBaseline(JSON.stringify(next))
    } catch (requestError) { setError(message(requestError, 'Promjene nisu spremljene.')) }
    finally { setSaving(false) }
  }

  async function duplicate() {
    if (!detail || dirty || saving) return
    setSaving(true)
    try { await duplicateLessonActivity(lessonPlanId, activityId, detail.rowVersion); navigate(`/lesson-plans/${lessonPlanId}/editor`) }
    catch (requestError) { setError(message(requestError, 'Aktivnost nije duplicirana.')); setSaving(false) }
  }
  async function remove() {
    if (!detail || saving || !window.confirm('Trajno ukloniti ovu aktivnost iz nacrta?')) return
    setSaving(true)
    try { await deleteLessonActivity(lessonPlanId, activityId, detail.rowVersion); navigate(`/lesson-plans/${lessonPlanId}/editor`) }
    catch (requestError) { setError(message(requestError, 'Aktivnost nije uklonjena.')); setSaving(false) }
  }
  async function generate(replaces: string | null = null) {
    if (!detail || dirty || saving) return
    setSaving(true); setError(null)
    try { const result = await createLessonActivityAiSuggestion(lessonPlanId, activityId, detail.rowVersion, replaces); setSuggestion(result.suggestion) }
    catch (requestError) { setError(message(requestError, 'AI prijedlog trenutačno nije dostupan.')) }
    finally { setSaving(false) }
  }
  async function resolve(decision: 'Accept' | 'Reject') {
    if (!detail || !suggestion || saving) return
    setSaving(true)
    try {
      const result = await resolveLessonActivityAiSuggestion(lessonPlanId, activityId, suggestion.id, detail.rowVersion, decision)
      if (result.detail) { const next = toForm(result.detail); setDetail(result.detail); setForm(next); setBaseline(JSON.stringify(next)) }
      setSuggestion(result.suggestion)
    } catch (requestError) { setError(message(requestError, 'AI prijedlog nije obrađen.')) }
    finally { setSaving(false) }
  }

  if (!detail || !form) return <section className="lesson-activity-state"><h1>Detalji aktivnosti</h1><p>{error ?? 'Učitavanje…'}</p></section>
  const preview = suggestion?.contentJson ? JSON.parse(suggestion.contentJson) as Record<string, string | null> : null

  return <section className="lesson-activity-page">
    <nav aria-label="Putanja"><Link to="/lesson-plans">Priprema sata</Link><span>›</span><Link to={`/lesson-plans/${lessonPlanId}/editor`}>Plan sata</Link><span>›</span><span>Detalji aktivnosti</span></nav>
    <header className="lesson-activity-header"><div><p>5.5 Detalji — uređivanje aktivnosti</p><h1>{detail.name}</h1><span>{detail.knowledgeBlockCode} · {detail.knowledgeBlockName}</span></div><div className="lesson-activity-actions ds-page-actions--below-global-profile"><button type="button" onClick={cancel}>Odustani</button><button type="button" onClick={() => void duplicate()} disabled={dirty || saving}>Dupliciraj</button><button type="button" className="danger" onClick={() => void remove()} disabled={saving}>Izbriši</button><button type="button" className="primary" onClick={() => void save()} disabled={!dirty || saving}>Spremi</button></div></header>
    {error && <div className="lesson-activity-alert" role="alert">{error}</div>}
    <div className="lesson-activity-context"><span><small>{detail.participantType === 'student' ? 'Učenik' : 'Grupa'}</small><strong>{detail.participantName}</strong></span><span><small>Cilj sata</small><strong>{detail.goal}</strong></span><span><small>Trajanje plana</small><strong>{detail.targetDurationMinutes} min</strong></span></div>
    <div className="lesson-activity-grid">
      <main>
        <Card title="Osnovne informacije"><div className="lesson-activity-fields"><Label text="Naziv aktivnosti"><input value={form.name} maxLength={160} onChange={event => change('name', event.target.value)} /></Label><Label text="Trajanje"><input type="number" min={1} max={480} value={form.durationMinutes} onChange={event => change('durationMinutes', Number(event.target.value))} /></Label><Label text="Svrha"><select value={form.purpose} onChange={event => change('purpose', event.target.value)}>{['Engagement','ActivatePriorKnowledge','Instruction','Practice','CheckForUnderstanding','Other'].map(value => <option key={value}>{value}</option>)}</select></Label><Label text="Oblik rada"><select value={form.workMode} onChange={event => change('workMode', event.target.value as FormState['workMode'])}><option value="Individual">Individualno</option><option value="Pair">U paru</option><option value="Group">Grupno</option></select></Label></div><Label text="Opis"><textarea value={form.description} maxLength={300} onChange={event => change('description', event.target.value)} /></Label><Label text="Pedagoška svrha"><textarea value={form.purposeDescription} maxLength={300} onChange={event => change('purposeDescription', event.target.value)} /></Label></Card>
        <Card title="Upute"><Label text="Upute za učitelja"><textarea value={form.teacherInstructions} maxLength={1000} onChange={event => change('teacherInstructions', event.target.value)} /></Label><Label text="Upute za učenike"><textarea value={form.studentInstructions} maxLength={1000} onChange={event => change('studentInstructions', event.target.value)} /></Label></Card>
        <Card title="Komponente znanja"><div className="lesson-activity-checks">{detail.availableKnowledgeComponents.map(item => <label key={item.id}><input type="checkbox" checked={form.knowledgeComponentIds.includes(item.id)} onChange={() => change('knowledgeComponentIds', form.knowledgeComponentIds.includes(item.id) ? form.knowledgeComponentIds.filter(id => id !== item.id) : [...form.knowledgeComponentIds, item.id])} /><span>{item.name}<small>{item.modelCode} · {item.modelVersion}</small></span></label>)}</div></Card>
        <Card title="Materijali i resursi">{detail.materials.length ? <ul className="lesson-activity-materials">{detail.materials.map(item => <li key={item.materialVersionId}><strong>{item.title}</strong><span>{item.materialTypeCode} · v{item.versionNumber}</span></li>)}</ul> : <p>Nema povezanih materijala.</p>}<Link to={`/lesson-plans/${lessonPlanId}/editor`}>Uredi materijale u planu</Link></Card>
      </main>
      <aside>
        <Card title="Prikaz na ploči"><label><input type="checkbox" checked={form.showTeacherInstructionsOnBoard} onChange={event => change('showTeacherInstructionsOnBoard', event.target.checked)} /> Upute za učitelja</label><label><input type="checkbox" checked={form.showStudentInstructionsOnBoard} onChange={event => change('showStudentInstructionsOnBoard', event.target.checked)} /> Upute za učenike</label><label><input type="checkbox" checked={form.showProgressOnBoard} onChange={event => change('showProgressOnBoard', event.target.checked)} /> Napredak aktivnosti</label></Card>
        <Card title="Bilješke"><textarea value={form.notes} maxLength={500} onChange={event => change('notes', event.target.value)} /><small>Bilješke se nikad ne šalju AI provideru.</small></Card>
        <Card title="AI prijedlog aktivnosti" className="lesson-activity-ai"><p>{detail.aiStatus.enabled ? 'AI je uključen samo za ovu ručnu akciju. Vanjska usluga može se naplaćivati vlasniku sustava; u aplikaciji nema naplate.' : 'AI pomoć je isključena. Uređivanje aktivnosti radi bez vanjske usluge i bez troška.'}</p>{detail.aiStatus.enabled && <button type="button" onClick={() => void generate()} disabled={dirty || saving}>Generiraj prijedlog</button>}{dirty && detail.aiStatus.enabled && <small>Prije generiranja spremite ili odbacite promjene.</small>}{preview && <div className="lesson-activity-preview"><strong>{preview.name}</strong><p>{preview.description}</p><p>{preview.teacherInstructions}</p><div><button type="button" onClick={() => void resolve('Reject')}>Odbij</button><button type="button" onClick={() => void generate(suggestion?.id ?? null)}>Regeneriraj</button><button type="button" className="primary" onClick={() => void resolve('Accept')}>Primijeni</button></div></div>}</Card>
      </aside>
    </div>
  </section>
}

function Card({ title, className = '', children }: { readonly title: string; readonly className?: string; readonly children: ReactNode }) { return <section className={`lesson-activity-card ${className}`}><h2>{title}</h2>{children}</section> }
function Label({ text, children }: { readonly text: string; readonly children: ReactNode }) { return <label className="lesson-activity-label"><span>{text}</span>{children}</label> }
function empty(value: string) { return value.trim() || null }
function toForm(value: LessonActivityDetail): FormState { return { name: value.name, purpose: value.purpose, durationMinutes: value.durationMinutes, workMode: value.workMode, description: value.description ?? '', purposeDescription: value.purposeDescription ?? '', teacherInstructions: value.teacherInstructions ?? '', studentInstructions: value.studentInstructions ?? '', notes: value.notes ?? '', showTeacherInstructionsOnBoard: value.showTeacherInstructionsOnBoard, showStudentInstructionsOnBoard: value.showStudentInstructionsOnBoard, showProgressOnBoard: value.showProgressOnBoard, knowledgeComponentIds: [...value.selectedKnowledgeComponentIds] } }
function message(error: unknown, fallback: string) { return error instanceof ApiError ? error.message : fallback }
