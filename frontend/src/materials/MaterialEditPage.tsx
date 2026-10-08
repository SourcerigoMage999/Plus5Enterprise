import { BookOpen, Check, Clock3, Copy, Eye, FileText, History, RotateCcw, Save, Send, X } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import { GroupUnsavedGuard } from '../groups/GroupUnsavedGuard.tsx'
import {
  getMaterialEditWorkspace, getMaterialVersion, publishMaterialDraft, restoreMaterialVersion,
  saveMaterialDraft, type MaterialEditPayload, type MaterialEditWorkspace, type MaterialVersionSnapshot,
} from './materialsApi.ts'
import './MaterialEditPage.css'

interface EditValues {
  title: string; description: string; materialTypeCode: string; subject: string; languageCode: string
  programId: string; schoolGradeId: string; proficiencyLevelId: string; learningGoal: string; tags: string
  knowledgeComponentIds: string[]; curriculumOutcomeIds: string[]
}

export function MaterialEditPage() {
  const { materialId = '' } = useParams()
  const saved = useRef(false)
  const [workspace, setWorkspace] = useState<MaterialEditWorkspace | null>(null)
  const [values, setValues] = useState<EditValues | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [saving, setSaving] = useState(false)
  const [historyOpen, setHistoryOpen] = useState(false)
  const [preview, setPreview] = useState<MaterialVersionSnapshot | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getMaterialEditWorkspace(materialId, controller.signal).then((result) => {
      setWorkspace(result); setValues(toValues(result.editableVersion)); setError(''); saved.current = false
    }).catch((requestError: unknown) => {
      if (requestError instanceof DOMException && requestError.name === 'AbortError') return
      setError(requestError instanceof ApiError && requestError.status === 404
        ? 'Materijal nije pronađen ili niste njegov vlasnik.' : 'Radni prostor za uređivanje trenutačno nije moguće učitati.')
    }).finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [materialId, revision])

  const initial = useMemo(() => workspace ? JSON.stringify(toValues(workspace.editableVersion)) : '', [workspace])
  const dirty = values ? JSON.stringify(values) !== initial : false
  if (loading) return <EditState text="Učitavanje uređivača…" />
  if (error || !workspace || !values) return <EditState text={error || 'Materijal nije dostupan.'} error retry={() => { setLoading(true); setError(''); setRevision(value => value + 1) }} />

  function update<K extends keyof EditValues>(key: K, value: EditValues[K]) { setValues(current => current ? { ...current, [key]: value } : current); setNotice('') }
  async function saveDraft() {
    if (!values || !workspace || saving) return
    if (!values.title.trim() || !values.materialTypeCode) { setError('Naziv i vrsta materijala su obavezni.'); return }
    setSaving(true); setError(''); setNotice('')
    try {
      await saveMaterialDraft(materialId, payload(workspace, values))
      saved.current = true; setNotice('Skica je spremljena. Aktivna verzija nije promijenjena.'); setRevision(value => value + 1)
    } catch (requestError) { setError(requestError instanceof ApiError ? requestError.message : 'Skicu nije moguće spremiti.') }
    finally { setSaving(false) }
  }
  async function publish() {
    if (!workspace || workspace.editableVersion.status !== 'draft' || dirty || saving) return
    setSaving(true); setError(''); setNotice('')
    try {
      await publishMaterialDraft(materialId, workspace.rowVersion, workspace.editableVersion.id)
      saved.current = true; setNotice('Nova verzija je objavljena i postavljena kao trenutačna.'); setRevision(value => value + 1)
    } catch (requestError) { setError(requestError instanceof ApiError ? requestError.message : 'Novu verziju nije moguće objaviti.') }
    finally { setSaving(false) }
  }
  async function openVersion(versionId: string) {
    setError('')
    try { setPreview(await getMaterialVersion(materialId, versionId)) }
    catch { setError('Povijesnu verziju trenutačno nije moguće učitati.') }
  }
  async function restore(versionId: string) {
    if (!workspace || saving) return
    setSaving(true); setError('')
    try {
      await restoreMaterialVersion(materialId, versionId, workspace.rowVersion)
      saved.current = true; setPreview(null); setNotice('Povijesna verzija vraćena je kao nova skica.'); setRevision(value => value + 1)
    } catch (requestError) { setError(requestError instanceof ApiError ? requestError.message : 'Verziju nije moguće vratiti.') }
    finally { setSaving(false) }
  }

  const options = workspace.options
  return <section className="material-edit" aria-labelledby="material-edit-title">
    <GroupUnsavedGuard dirty={dirty} saving={saving} saved={saved} />
    <nav className="material-edit__breadcrumb"><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}`}>Pregled</Link><span>›</span><strong>Uredi</strong></nav>
    <header className="material-edit__hero">
      <div><h1 id="material-edit-title">4.5 Uredi materijal</h1><p>Nova pedagoška promjena sprema se kao verzionirana skica.</p></div>
      <div className="material-edit__top-actions">
        <span className="material-edit__version">v{workspace.editableVersion.versionNumber} · {workspace.editableVersion.status === 'draft' ? 'Skica' : 'Aktivna'}</span>
        <button className="ds-action ds-action--secondary" type="button" onClick={() => setHistoryOpen(value => !value)}><History size={17} /> Povijest</button>
        <button className="ds-action ds-action--secondary" disabled title="Duplikat dobiva novi Material ID; workflow dolazi nakon permission contracta." type="button"><Copy size={17} /> Dupliciraj</button>
        <button className="ds-action ds-action--secondary" disabled={saving || !dirty} onClick={() => void saveDraft()} type="button"><Save size={17} /> Spremi skicu</button>
        <button className="ds-action" disabled={saving || dirty || workspace.editableVersion.status !== 'draft'} onClick={() => void publish()} title={dirty ? 'Prvo spremite promjene u skicu.' : undefined} type="button"><Send size={17} /> Objavi novu verziju</button>
      </div>
    </header>
    {(error || notice) && <div className={`material-edit__notice${error ? ' is-error' : ''}`} role={error ? 'alert' : 'status'}>{error || notice}</div>}

    <ol className="material-edit__steps"><li className="is-active"><span>1</span> Sadržaj</li><li className="is-active"><span>2</span> Osnovni podaci</li><li className="is-active"><span>3</span> Cilj i mapiranje</li><li><span><Check size={15} /></span> Pregled i objava</li></ol>
    <div className="material-edit__layout">
      <main className="material-edit__canvas">
        <section className="material-edit__file-card">
          <div className="material-edit__file-icon"><FileText size={36} /><small>{workspace.editableVersion.file.format.toUpperCase()}</small></div>
          <div><span>Izvorna datoteka</span><strong>{workspace.editableVersion.file.originalFileName}</strong><small>{formatBytes(workspace.editableVersion.file.sizeBytes)} · sadržaj ostaje nepromijenjen</small></div>
        </section>
        <section className="material-edit__editor-placeholder">
          <BookOpen size={44} /><h2>Uređivanje sadržaja</h2><p>U ovoj fazi uređuju se metapodaci i pedagoško mapiranje. Uređivač slajdova i sadržaja pripada Phase 7 i nije simuliran.</p>
          <div><strong>{workspace.editableVersion.assessableTaskCount}</strong><span>verzioniranih procjenjivih zadataka prenosi se bez promjene u novu verziju</span></div>
        </section>
        <section className="material-edit__mapping">
          <h2>Cilj i pedagoško mapiranje</h2>
          <Field label="Cilj učenja"><textarea value={values.learningGoal} maxLength={2000} rows={4} onChange={event => update('learningGoal', event.target.value)} /></Field>
          <Choice title="Knowledge Components" items={options.knowledgeComponents.map(item => ({ id: item.id, label: item.name, detail: `${item.knowledgeAreaName} · ${item.knowledgeModelCode} ${item.knowledgeModelVersion}` }))} selected={values.knowledgeComponentIds} onChange={ids => update('knowledgeComponentIds', ids)} />
          <Choice title="Ishodi kurikuluma" items={options.curriculumOutcomes.map(item => ({ id: item.id, label: item.officialCode ?? item.title, detail: `${item.curriculumCode} ${item.curriculumVersion} · ${item.title}` }))} selected={values.curriculumOutcomeIds} onChange={ids => update('curriculumOutcomeIds', ids)} />
        </section>
      </main>
      <aside className="material-edit__sidebar">
        <section><h2>Osnovni podaci</h2>
          <Field label="Naziv *"><input value={values.title} maxLength={200} onChange={event => update('title', event.target.value)} /></Field>
          <Field label="Vrsta materijala *"><select value={values.materialTypeCode} onChange={event => update('materialTypeCode', event.target.value)}>{options.materialTypeCodes.map(code => <option key={code} value={code}>{labelType(code)}</option>)}</select></Field>
          <Field label="Predmet"><input value={values.subject} maxLength={160} onChange={event => update('subject', event.target.value)} /></Field>
          <Field label="Program"><select value={values.programId} onChange={event => update('programId', event.target.value)}><option value="">Bez programa</option>{options.programs.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <div className="material-edit__two"><Field label="Razred"><select value={values.schoolGradeId} onChange={event => update('schoolGradeId', event.target.value)}><option value="">—</option>{options.schoolGrades.map(item => <option key={item.id} value={item.id}>{item.code ?? item.name}</option>)}</select></Field><Field label="Razina"><select value={values.proficiencyLevelId} onChange={event => update('proficiencyLevelId', event.target.value)}><option value="">—</option>{options.proficiencyLevels.map(item => <option key={item.id} value={item.id}>{item.code ?? item.name}</option>)}</select></Field></div>
          <Field label="Jezik"><select value={values.languageCode} onChange={event => update('languageCode', event.target.value)}><option value="">—</option>{options.languageCodes.map(code => <option key={code}>{code}</option>)}</select></Field>
          <Field label="Opis"><textarea value={values.description} maxLength={2000} rows={4} onChange={event => update('description', event.target.value)} /></Field>
          <Field label="Oznake"><input value={values.tags} maxLength={500} placeholder="gramatika, A2" onChange={event => update('tags', event.target.value)} /></Field>
          <div className="material-edit__visibility"><Eye size={17} /><span>Vidljivost: <strong>{workspace.visibility === 'private' ? 'Privatno' : 'Dijeljeno'}</strong><small>Dozvole se uređuju u Phase 6.8.</small></span></div>
        </section>
      </aside>
    </div>

    {historyOpen && <aside className="material-edit__history" aria-label="Povijest verzija"><header><div><History size={20} /><h2>Povijest verzija</h2></div><button type="button" onClick={() => setHistoryOpen(false)} aria-label="Zatvori"><X /></button></header><ol>{workspace.history.map(item => <li key={item.id}><button type="button" onClick={() => void openVersion(item.id)}><span>v{item.versionNumber}</span><div><strong>{item.title}</strong><small>{statusLabel(item.status)} · {formatDate(item.createdAtUtc)}</small></div>{item.isCurrent && <em>Trenutačna</em>}</button></li>)}</ol></aside>}
    {preview && <dialog open className="material-edit__preview"><div><header><div><Clock3 size={20} /><h2>Verzija {preview.versionNumber}</h2></div><button onClick={() => setPreview(null)} type="button" aria-label="Zatvori"><X /></button></header><dl><Fact label="Status" value={statusLabel(preview.status)} /><Fact label="Naziv" value={preview.title} /><Fact label="Vrsta" value={labelType(preview.materialTypeCode)} /><Fact label="Predmet" value={preview.subject || 'Nije uneseno'} /><Fact label="Cilj učenja" value={preview.learningGoal || 'Nije unesen'} /><Fact label="Mapiranja" value={`${preview.knowledgeComponentIds.length} KC · ${preview.curriculumOutcomeIds.length} ishoda`} /></dl><p>Ovaj snapshot je samo za čitanje. Vraćanje stvara novu skicu i ne mijenja povijest.</p><button className="ds-action" disabled={preview.status === 'draft' || preview.id === workspace.currentVersionId || workspace.editableVersion.status === 'draft'} onClick={() => void restore(preview.id)} type="button"><RotateCcw size={17} /> Vrati kao novu skicu</button></div></dialog>}
  </section>
}

function toValues(source: MaterialVersionSnapshot): EditValues { return { title: source.title, description: source.description ?? '', materialTypeCode: source.materialTypeCode, subject: source.subject ?? '', languageCode: source.languageCode ?? '', programId: source.programId ?? '', schoolGradeId: source.schoolGradeId ?? '', proficiencyLevelId: source.proficiencyLevelId ?? '', learningGoal: source.learningGoal ?? '', tags: source.tags.join(', '), knowledgeComponentIds: [...source.knowledgeComponentIds], curriculumOutcomeIds: [...source.curriculumOutcomeIds] } }
function payload(workspace: MaterialEditWorkspace, values: EditValues): MaterialEditPayload { const empty = (value: string) => value.trim() || null; return { expectedRowVersion: workspace.rowVersion, draftVersionId: workspace.editableVersion.status === 'draft' ? workspace.editableVersion.id : null, title: values.title.trim(), description: empty(values.description), materialTypeCode: values.materialTypeCode, subject: empty(values.subject), languageCode: empty(values.languageCode), programId: empty(values.programId), schoolGradeId: empty(values.schoolGradeId), proficiencyLevelId: empty(values.proficiencyLevelId), learningGoal: empty(values.learningGoal), tags: values.tags.split(',').map(tag => tag.trim()).filter(Boolean), knowledgeComponentIds: values.knowledgeComponentIds, curriculumOutcomeIds: values.curriculumOutcomeIds } }
function Field({ label, children }: { readonly label: string; readonly children: React.ReactNode }) { return <label className="material-edit__field"><span>{label}</span>{children}</label> }
function Choice({ title, items, selected, onChange }: { readonly title: string; readonly items: readonly { id: string; label: string; detail: string }[]; readonly selected: readonly string[]; readonly onChange: (ids: string[]) => void }) { return <section className="material-edit__choices"><h3>{title}</h3><div>{items.length ? items.map(item => <label key={item.id}><input type="checkbox" checked={selected.includes(item.id)} onChange={event => onChange(event.target.checked ? [...selected, item.id] : selected.filter(id => id !== item.id))} /><span><strong>{item.label}</strong><small>{item.detail}</small></span></label>) : <p>Nema dostupnih stavki.</p>}</div></section> }
function Fact({ label, value }: { readonly label: string; readonly value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function EditState({ text, error, retry }: { readonly text: string; readonly error?: boolean; readonly retry?: () => void }) { return <section className="material-edit-state" role={error ? 'alert' : 'status'}><strong>{text}</strong>{retry && <button className="ds-action" onClick={retry}>Pokušaj ponovno</button>}<Link to="/materials">Povratak u biblioteku</Link></section> }
function statusLabel(value: string) { return value === 'active' ? 'Aktivna' : value === 'superseded' ? 'Zamijenjena' : 'Skica' }
function labelType(code: string) { return ({ PRESENTATION: 'Prezentacija', WORKSHEET: 'Radni list', CONVERSATION_CARDS: 'Kartice za razgovor', INTERACTIVE_EXERCISE: 'Interaktivna vježba', VIDEO: 'Video', AUDIO: 'Audio', QUIZ: 'Kviz', IMAGE: 'Slika', POSTER: 'Plakat', MAP: 'Mapa', OTHER: 'Ostalo' } as Record<string, string>)[code] ?? code }
function formatBytes(value: number) { return value >= 1024 * 1024 ? `${(value / 1024 / 1024).toFixed(1)} MB` : `${Math.ceil(value / 1024)} KB` }
function formatDate(value: string) { return new Intl.DateTimeFormat('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) }
