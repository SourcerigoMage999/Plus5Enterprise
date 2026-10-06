import { zodResolver } from '@hookform/resolvers/zod'
import { Check, ChevronLeft, ChevronRight, FileUp, ShieldCheck, X } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { z } from 'zod'
import { ApiError } from '../api/apiClient.ts'
import { GroupUnsavedGuard } from '../groups/GroupUnsavedGuard.tsx'
import {
  getMaterialImportOptions,
  importMaterial,
  type MaterialImportMetadata,
  type MaterialImportOptions,
} from './materialsApi.ts'
import './MaterialImportPage.css'

const acceptedExtensions = ['.pdf', '.docx', '.pptx', '.mp4', '.zip'] as const
const maxBytes: Record<string, number> = {
  '.pdf': 50 * 1024 * 1024,
  '.docx': 25 * 1024 * 1024,
  '.pptx': 100 * 1024 * 1024,
  '.mp4': 250 * 1024 * 1024,
  '.zip': 100 * 1024 * 1024,
}

const schema = z.object({
  title: z.string().trim().min(1, 'Upišite naziv materijala.').max(200),
  materialTypeCode: z.string().trim().min(1, 'Odaberite vrstu materijala.').max(64),
  description: z.string().max(2000),
  subject: z.string().max(160),
  languageCode: z.string().max(32),
  visibility: z.enum(['private', 'shared']),
  programId: z.string(),
  schoolGradeId: z.string(),
  proficiencyLevelId: z.string(),
  learningGoal: z.string().max(2000),
  tags: z.string().max(500),
  knowledgeComponentIds: z.array(z.string()),
  curriculumOutcomeIds: z.array(z.string()),
})

type FormValues = z.infer<typeof schema>

const steps = ['Datoteka', 'Osnovni podaci', 'Cilj i mapiranje', 'Pregled'] as const

export function MaterialImportPage() {
  const navigate = useNavigate()
  const saved = useRef(false)
  const [step, setStep] = useState(0)
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState('')
  const [options, setOptions] = useState<MaterialImportOptions | null>(null)
  const [loadError, setLoadError] = useState('')
  const [revision, setRevision] = useState(0)
  const [submitError, setSubmitError] = useState('')
  const [saving, setSaving] = useState(false)
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '', materialTypeCode: '', description: '', subject: '', languageCode: 'EN',
      visibility: 'private', programId: '', schoolGradeId: '', proficiencyLevelId: '',
      learningGoal: '', tags: '', knowledgeComponentIds: [], curriculumOutcomeIds: [],
    },
  })
  // React Hook Form intentionally exposes a subscription-based watch API for wizard previews.
  // oxlint-disable-next-line react/incompatible-library
  const values = form.watch()

  useEffect(() => {
    const controller = new AbortController()
    setLoadError('')
    getMaterialImportOptions(controller.signal)
      .then(setOptions)
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setLoadError(error instanceof ApiError ? error.message : 'Podatke za uvoz trenutačno nije moguće učitati.')
      })
    return () => controller.abort()
  }, [revision])

  const selectedComponents = useMemo(() => options?.knowledgeComponents.filter(
    (item) => values.knowledgeComponentIds.includes(item.id),
  ) ?? [], [options, values.knowledgeComponentIds])
  const selectedOutcomes = useMemo(() => options?.curriculumOutcomes.filter(
    (item) => values.curriculumOutcomeIds.includes(item.id),
  ) ?? [], [options, values.curriculumOutcomeIds])
  const dirty = Boolean(file || form.formState.isDirty)

  function selectFile(nextFile: File | null) {
    setFileError('')
    if (!nextFile) { setFile(null); return }
    const extension = extensionOf(nextFile.name)
    if (!acceptedExtensions.includes(extension as (typeof acceptedExtensions)[number])) {
      setFileError('Dopušteni su PDF, DOCX, PPTX, MP4 i ZIP.')
      return
    }
    if (nextFile.size <= 0 || nextFile.size > maxBytes[extension]) {
      setFileError(`Datoteka prelazi dopuštenu veličinu za ${extension.slice(1).toUpperCase()} format.`)
      return
    }
    setFile(nextFile)
    if (!form.getValues('title')) form.setValue('title', nextFile.name.replace(/\.[^.]+$/, ''), { shouldDirty: true })
  }

  async function next() {
    setSubmitError('')
    if (step === 0) {
      if (!file) { setFileError('Odaberite datoteku za uvoz.'); return }
    }
    if (step === 1 && !await form.trigger([
      'title', 'materialTypeCode', 'description', 'subject', 'languageCode', 'visibility',
    ])) return
    setStep((current) => Math.min(current + 1, steps.length - 1))
  }

  async function submit(data: FormValues) {
    if (!file || saving) return
    setSaving(true)
    setSubmitError('')
    try {
      const metadata: MaterialImportMetadata = {
        title: data.title.trim(),
        materialTypeCode: data.materialTypeCode,
        description: nullable(data.description),
        subject: nullable(data.subject),
        languageCode: nullable(data.languageCode),
        visibility: data.visibility === 'private' ? 1 : 2,
        programId: nullable(data.programId),
        schoolGradeId: nullable(data.schoolGradeId),
        proficiencyLevelId: nullable(data.proficiencyLevelId),
        learningGoal: nullable(data.learningGoal),
        tags: data.tags.split(',').map((tag) => tag.trim()).filter(Boolean),
        knowledgeComponentIds: data.knowledgeComponentIds,
        curriculumOutcomeIds: data.curriculumOutcomeIds,
      }
      const result = await importMaterial(file, metadata)
      saved.current = true
      navigate(`/materials/${result.materialId}`, { replace: true, state: { imported: true } })
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'Materijal trenutačno nije moguće uvesti.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="material-import" aria-labelledby="material-import-title">
      <GroupUnsavedGuard dirty={dirty} saved={saved} saving={saving} />
      <nav className="material-import__breadcrumb" aria-label="Putanja">
        <Link to="/materials">Materijali</Link><span aria-hidden="true">›</span><span>Uvoz materijala</span>
      </nav>
      <header className="material-import__hero">
        <div><h1 id="material-import-title">4.4 Uvoz vlastitog materijala</h1><p>Dodajte datoteku i potvrdite podatke prije objave u biblioteku.</p></div>
        <Link className="ds-action ds-action--secondary" to="/materials"><X size={18} /> Odustani</Link>
      </header>

      <ol className="material-import__steps" aria-label="Koraci uvoza">
        {steps.map((label, index) => <li aria-current={index === step ? 'step' : undefined} className={index < step ? 'is-complete' : ''} key={label}>
          <span>{index < step ? <Check size={16} /> : index + 1}</span><strong>{label}</strong>
        </li>)}
      </ol>

      {loadError && <div className="material-import__notice" role="alert">{loadError} <button type="button" onClick={() => setRevision((value) => value + 1)}>Pokušaj ponovno</button></div>}
      {submitError && <div className="material-import__notice" role="alert">{submitError}</div>}

      <form onSubmit={form.handleSubmit(submit)}>
        <fieldset disabled={saving || !options}>
          {step === 0 && <FileStep file={file} error={fileError} onSelect={selectFile} />}
          {step === 1 && <MetadataStep form={form} options={options} />}
          {step === 2 && <MappingStep form={form} options={options} />}
          {step === 3 && file && <ReviewStep file={file} values={values} options={options} components={selectedComponents} outcomes={selectedOutcomes} />}
        </fieldset>
        <footer className="material-import__actions">
          {step > 0 ? <button className="ds-action ds-action--secondary" type="button" onClick={() => setStep((current) => current - 1)}><ChevronLeft size={18} /> Natrag</button> : <span />}
          {step < 3
            ? <button className="ds-action" type="button" onClick={(event) => { event.preventDefault(); void next() }}>Nastavi <ChevronRight size={18} /></button>
            : <button className="ds-action" disabled={saving || !file} type="submit"><ShieldCheck size={18} /> {saving ? 'Sigurnosna provjera…' : 'Potvrdi i uvezi'}</button>}
        </footer>
      </form>
      <p className="material-import__security"><ShieldCheck size={17} /> Datoteka postaje dostupna tek nakon provjere formata, strukture i malware skeniranja.</p>
    </section>
  )
}

function FileStep({ file, error, onSelect }: { readonly file: File | null; readonly error: string; readonly onSelect: (file: File | null) => void }) {
  return <section className="material-import__panel"><h2>Odaberite datoteku</h2><p>Podržani su PDF, DOCX, PPTX, MP4 i ZIP.</p>
    <label className="material-import__drop" onDragOver={(event) => event.preventDefault()} onDrop={(event) => { event.preventDefault(); onSelect(event.dataTransfer.files[0] ?? null) }}>
      <FileUp size={42} /><strong>{file ? file.name : 'Povucite datoteku ovdje'}</strong><span>{file ? readableBytes(file.size) : 'ili kliknite za odabir s računala'}</span>
      <input accept=".pdf,.docx,.pptx,.mp4,.zip" type="file" onChange={(event) => onSelect(event.target.files?.[0] ?? null)} />
    </label>
    <ul className="material-import__limits"><li>PDF do 50 MB</li><li>DOCX do 25 MB</li><li>PPTX i ZIP do 100 MB</li><li>MP4 do 250 MB</li></ul>
    {error && <p className="material-import__field-error" role="alert">{error}</p>}
  </section>
}

function MetadataStep({ form, options }: { readonly form: ReturnType<typeof useForm<FormValues>>; readonly options: MaterialImportOptions | null }) {
  const errors = form.formState.errors
  return <section className="material-import__panel"><h2>Osnovni podaci</h2><div className="material-import__grid">
    <Field label="Naziv" required error={errors.title?.message}><input {...form.register('title')} maxLength={200} /></Field>
    <Field label="Vrsta materijala" required error={errors.materialTypeCode?.message}><select {...form.register('materialTypeCode')}><option value="">Odaberite vrstu</option>{options?.materialTypeCodes.map((code) => <option key={code} value={code}>{typeLabel(code)}</option>)}</select></Field>
    <Field label="Predmet"><input {...form.register('subject')} maxLength={160} placeholder="npr. Engleski jezik" /></Field>
    <Field label="Jezik"><select {...form.register('languageCode')}><option value="">Nije određeno</option>{options?.languageCodes.map((code) => <option key={code} value={code}>{code}</option>)}</select></Field>
    <Field label="Program"><select {...form.register('programId')}><option value="">Bez programa</option>{options?.programs.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
    <Field label="Razred"><select {...form.register('schoolGradeId')}><option value="">Bez razreda</option>{options?.schoolGrades.map((item) => <option key={item.id} value={item.id}>{item.code ?? item.name}</option>)}</select></Field>
    <Field label="CEFR / razina"><select {...form.register('proficiencyLevelId')}><option value="">Bez razine</option>{options?.proficiencyLevels.map((item) => <option key={item.id} value={item.id}>{item.code ?? item.name}</option>)}</select></Field>
    <Field label="Vidljivost"><select {...form.register('visibility')}><option value="private">Privatno</option><option value="shared">Dijeljeno (grantovi se uređuju naknadno)</option></select></Field>
    <Field label="Opis" wide><textarea {...form.register('description')} maxLength={2000} rows={4} /></Field>
    <Field label="Oznake" wide><input {...form.register('tags')} maxLength={500} placeholder="gramatika, vježba, A2" /><small>Odvojite oznake zarezom.</small></Field>
  </div></section>
}

function MappingStep({ form, options }: { readonly form: ReturnType<typeof useForm<FormValues>>; readonly options: MaterialImportOptions | null }) {
  return <section className="material-import__panel"><h2>Cilj i pedagoško mapiranje</h2><p>Mapiranje je opcionalno i veže se uz ovu verziju materijala.</p>
    <Field label="Cilj učenja"><textarea {...form.register('learningGoal')} maxLength={2000} rows={4} /></Field>
    <ChoiceGroup title="Knowledge Components" empty="Nema objavljenih Knowledge Components u katalogu." items={(options?.knowledgeComponents ?? []).map((item) => ({ id: item.id, label: item.name, detail: `${item.knowledgeAreaName} · ${item.knowledgeModelCode} ${item.knowledgeModelVersion}` }))} selected={form.watch('knowledgeComponentIds')} onChange={(ids) => form.setValue('knowledgeComponentIds', ids, { shouldDirty: true })} />
    <ChoiceGroup title="Ishodi kurikuluma" empty="Nema ishoda u katalogu." items={(options?.curriculumOutcomes ?? []).map((item) => ({ id: item.id, label: item.officialCode ?? item.title, detail: `${item.curriculumCode} ${item.curriculumVersion} · ${item.title}` }))} selected={form.watch('curriculumOutcomeIds')} onChange={(ids) => form.setValue('curriculumOutcomeIds', ids, { shouldDirty: true })} />
  </section>
}

function ReviewStep({ file, values, options, components, outcomes }: { readonly file: File; readonly values: FormValues; readonly options: MaterialImportOptions | null; readonly components: readonly { name: string }[]; readonly outcomes: readonly { title: string; officialCode: string | null }[] }) {
  return <section className="material-import__panel"><h2>Pregled prije uvoza</h2><div className="material-import__review">
    <Review label="Datoteka" value={`${file.name} · ${readableBytes(file.size)}`} />
    <Review label="Naziv" value={values.title} />
    <Review label="Vrsta" value={typeLabel(values.materialTypeCode)} />
    <Review label="Program" value={options?.programs.find((item) => item.id === values.programId)?.name ?? 'Bez programa'} />
    <Review label="Razred / razina" value={[options?.schoolGrades.find((item) => item.id === values.schoolGradeId)?.code, options?.proficiencyLevels.find((item) => item.id === values.proficiencyLevelId)?.code].filter(Boolean).join(' · ') || 'Nije određeno'} />
    <Review label="Vidljivost" value={values.visibility === 'private' ? 'Privatno' : 'Dijeljeno'} />
    <Review label="Cilj učenja" value={values.learningGoal || 'Nije unesen'} />
    <Review label="Knowledge Components" value={components.map((item) => item.name).join(', ') || 'Bez mapiranja'} />
    <Review label="Ishodi" value={outcomes.map((item) => item.officialCode ?? item.title).join(', ') || 'Bez mapiranja'} />
  </div><div className="material-import__confirm"><ShieldCheck size={24} /><div><strong>Siguran uvoz</strong><p>Server provjerava stvarni format, checksum i sadržaj u karanteni. Materijal se aktivira samo ako je rezultat Clean.</p></div></div></section>
}

function Field({ label, required, error, wide, children }: { readonly label: string; readonly required?: boolean; readonly error?: string; readonly wide?: boolean; readonly children: React.ReactNode }) {
  return <label className={wide ? 'material-import__field material-import__field--wide' : 'material-import__field'}><span>{label}{required && ' *'}</span>{children}{error && <small className="material-import__field-error">{error}</small>}</label>
}

function ChoiceGroup({ title, empty, items, selected, onChange }: { readonly title: string; readonly empty: string; readonly items: readonly { id: string; label: string; detail: string }[]; readonly selected: readonly string[]; readonly onChange: (ids: string[]) => void }) {
  return <section className="material-import__choices"><h3>{title}</h3>{items.length === 0 ? <p>{empty}</p> : <div>{items.map((item) => <label key={item.id}><input type="checkbox" checked={selected.includes(item.id)} onChange={(event) => onChange(event.target.checked ? [...selected, item.id] : selected.filter((id) => id !== item.id))} /><span><strong>{item.label}</strong><small>{item.detail}</small></span></label>)}</div>}</section>
}

function Review({ label, value }: { readonly label: string; readonly value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function nullable(value: string) { return value.trim() || null }
function extensionOf(name: string) { const index = name.lastIndexOf('.'); return index < 0 ? '' : name.slice(index).toLowerCase() }
function readableBytes(bytes: number) { return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.ceil(bytes / 1024)} KB` }
function typeLabel(code: string) { return ({ PRESENTATION: 'Prezentacija', WORKSHEET: 'Radni list', CONVERSATION_CARDS: 'Kartice za razgovor', INTERACTIVE_EXERCISE: 'Interaktivna vježba', VIDEO: 'Video', AUDIO: 'Audio', QUIZ: 'Kviz', IMAGE: 'Slika', POSTER: 'Plakat', MAP: 'Mapa', OTHER: 'Ostalo' } as Record<string, string>)[code] ?? code }
