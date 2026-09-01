import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  archiveStudent,
  getStudentCreateOptions,
  getStudentEdit,
  updateStudent,
  type StudentCreateOptions,
  type StudentDeliveryMode,
  type StudentEditGuardian,
  type StudentEditModel,
  type StudentStatus,
} from './studentsApi.ts'
import './StudentEditPage.css'

interface EditForm {
  updatedAtUtc: string
  rowVersion: string
  firstName: string
  lastName: string
  nickname: string
  dateOfBirth: string
  schoolName: string
  gender: string
  email: string
  phone: string
  schoolGradeId: string
  programId: string
  deliveryMode: '' | StudentDeliveryMode
  groupId: string
  status: StudentStatus
  guardians: StudentEditGuardian[]
}

export function StudentEditPage() {
  const { studentId = '' } = useParams()
  const navigate = useNavigate()
  const [form, setForm] = useState<EditForm | null>(null)
  const [options, setOptions] = useState<StudentCreateOptions | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [archiving, setArchiving] = useState(false)
  const [confirmArchive, setConfirmArchive] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getStudentEdit(studentId, controller.signal)
      .then(async (student) => {
        setForm(toForm(student))
        setOptions(await getStudentCreateOptions(student.programId ?? undefined, controller.signal))
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError && requestError.status === 404
          ? 'Učenik ne postoji, arhiviran je ili nije dio vašeg računa.'
          : 'Podatke učenika trenutačno nije moguće učitati.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [studentId])

  function update<K extends keyof EditForm>(key: K, value: EditForm[K]) {
    setForm((current) => current ? { ...current, [key]: value } : current)
  }

  async function changeProgram(programId: string) {
    if (!form) return
    update('programId', programId)
    setForm((current) => current ? {
      ...current,
      programId,
      deliveryMode: programId ? current.deliveryMode || 'individual' : '',
      groupId: '',
    } : current)
    try {
      setOptions(await getStudentCreateOptions(programId || undefined))
    } catch {
      setError('Dostupne grupe trenutačno nije moguće osvježiti.')
    }
  }

  function updateGuardian(index: number, patch: Partial<StudentEditGuardian>) {
    setForm((current) => current ? {
      ...current,
      guardians: current.guardians.map((guardian, guardianIndex) => {
        if ('isPrimary' in patch && patch.isPrimary && guardianIndex !== index) {
          return { ...guardian, isPrimary: false }
        }
        return guardianIndex === index ? { ...guardian, ...patch } : guardian
      }),
    } : current)
  }

  function addGuardian() {
    if (!form || form.guardians.length >= 10) return
    update('guardians', [...form.guardians, {
      id: null, firstName: '', lastName: '', relationship: null,
      email: null, phone: null, isPrimary: form.guardians.length === 0,
    }])
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!form || saving) return
    setSaving(true)
    setError(null)
    try {
      await updateStudent(studentId, {
        rowVersion: form.rowVersion,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        nickname: optional(form.nickname),
        dateOfBirth: optional(form.dateOfBirth),
        schoolName: optional(form.schoolName),
        gender: optional(form.gender),
        email: optional(form.email),
        phone: optional(form.phone),
        schoolGradeId: form.schoolGradeId,
        programId: optional(form.programId),
        deliveryMode: form.deliveryMode || null,
        groupId: form.deliveryMode === 'group' ? optional(form.groupId) : null,
        status: form.status,
        guardians: form.guardians.map((guardian) => ({
          ...guardian,
          firstName: guardian.firstName.trim(),
          lastName: guardian.lastName.trim(),
          relationship: optional(guardian.relationship ?? ''),
          email: optional(guardian.email ?? ''),
          phone: optional(guardian.phone ?? ''),
        })),
      })
      navigate(`/students/${studentId}`, { replace: true, state: { updated: true } })
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Promjene trenutačno nije moguće spremiti.')
    } finally {
      setSaving(false)
    }
  }

  async function archive() {
    if (!form || archiving) return
    setArchiving(true)
    setError(null)
    try {
      await archiveStudent(studentId, form.rowVersion)
      navigate('/students', { replace: true, state: { archived: true } })
    } catch (requestError) {
      setConfirmArchive(false)
      setError(requestError instanceof ApiError ? requestError.message : 'Učenika trenutačno nije moguće arhivirati.')
    } finally {
      setArchiving(false)
    }
  }

  if (loading) return <EditState title="Učitavanje učenika" message="Pripremamo obrazac za uređivanje…" />
  if (!form || !options) return <EditState title="Uređivanje nije dostupno" message={error ?? 'Podaci nisu dostupni.'}><Link to="/students">Povratak na popis</Link></EditState>

  const name = `${form.firstName} ${form.lastName}`.trim()
  return (
    <section className="student-edit-page" aria-labelledby="student-edit-title">
      <nav className="student-edit-breadcrumb" aria-label="Putanja"><Link to="/students">Učenici</Link><span>›</span><Link to={`/students/${studentId}`}>{name}</Link><span>›</span><span>Uredi učenika</span></nav>
      <header className="student-edit-hero">
        <div><h1 id="student-edit-title">2.6 Uredi učenika</h1><p>Pregledaj i ažuriraj podatke o učeniku.</p></div>
        <div className="student-edit-top-actions"><Link to={`/students/${studentId}`}>Odustani</Link><button className="is-danger" onClick={() => setConfirmArchive(true)} type="button">Arhiviraj učenika</button><button disabled={saving} form="student-edit-form" type="submit">{saving ? 'Spremanje…' : 'Spremi promjene'}</button></div>
      </header>

      {error && <div className="student-edit-alert" role="alert">{error}</div>}
      <form id="student-edit-form" className="student-edit-layout" onSubmit={(event) => void submit(event)}>
        <main className="student-edit-main">
          <EditCard className="student-edit-card--basic" icon="●" title="Osnovni podaci">
            <div className="student-edit-grid">
              <Field label="Ime" required><input required maxLength={100} value={form.firstName} onChange={(event) => update('firstName', event.target.value)} /></Field>
              <Field label="Prezime" required><input required maxLength={100} value={form.lastName} onChange={(event) => update('lastName', event.target.value)} /></Field>
              <Field label="Nadimak"><input maxLength={100} value={form.nickname} onChange={(event) => update('nickname', event.target.value)} /></Field>
              <Field label="Datum rođenja"><input type="date" value={form.dateOfBirth} onChange={(event) => update('dateOfBirth', event.target.value)} /></Field>
              <Field label="Spol"><select value={form.gender} onChange={(event) => update('gender', event.target.value)}><option value="">Nije navedeno</option><option>Žensko</option><option>Muško</option><option>Drugo</option></select></Field>
              <Field label="Razred" required><select required value={form.schoolGradeId} onChange={(event) => update('schoolGradeId', event.target.value)}><option value="">Odaberite razred</option>{options.schoolGrades.map((grade) => <option key={grade.id} value={grade.id}>{grade.code ? `${grade.code} — ${grade.name}` : grade.name}</option>)}</select></Field>
              <Field label="Škola"><input maxLength={200} value={form.schoolName} onChange={(event) => update('schoolName', event.target.value)} /></Field>
              <Field label="Status"><select value={form.status} onChange={(event) => update('status', event.target.value as StudentStatus)}><option value="active">Aktivan</option><option value="on_hold">Na čekanju</option><option value="inactive">Neaktivan</option></select></Field>
            </div>
          </EditCard>

          <EditCard className="student-edit-card--program" icon="▣" title="Program i grupa">
            <div className="student-edit-grid">
              <Field label="Program"><select value={form.programId} onChange={(event) => void changeProgram(event.target.value)}><option value="">Bez programa</option>{options.programs.map((program) => <option key={program.id} value={program.id}>{program.name}</option>)}</select></Field>
              <Field label="Način rada"><select disabled={!form.programId} value={form.deliveryMode} onChange={(event) => { update('deliveryMode', event.target.value as EditForm['deliveryMode']); update('groupId', '') }}><option value="">Odaberite</option><option value="individual">Individualno</option><option value="group">Grupno</option></select></Field>
              <Field label="Grupa"><select disabled={form.deliveryMode !== 'group'} required={form.deliveryMode === 'group'} value={form.groupId} onChange={(event) => update('groupId', event.target.value)}><option value="">Odaberite grupu</option>{options.groups.filter((group) => group.activeMemberCount < group.capacity || group.id === form.groupId).map((group) => <option key={group.id} value={group.id}>{group.name} ({group.activeMemberCount}/{group.capacity})</option>)}</select></Field>
            </div>
          </EditCard>

          <EditCard className="student-edit-card--contacts" icon="♙" title="Kontakti i roditelji/skrbnici" action={<button disabled={form.guardians.length >= 10} onClick={addGuardian} type="button">+ Dodaj kontakt</button>}>
            <div className="student-edit-grid"><Field label="E-mail učenika"><input type="email" maxLength={320} value={form.email} onChange={(event) => update('email', event.target.value)} /></Field><Field label="Telefon učenika"><input type="tel" maxLength={32} value={form.phone} onChange={(event) => update('phone', event.target.value)} /></Field></div>
            {form.guardians.length === 0 && <p className="student-edit-empty">Nema dodanih kontakata. Novi kontakt možete dodati iznad.</p>}
            <div className="student-edit-guardians">{form.guardians.map((guardian, index) => <article key={guardian.id ?? `new-${index}`}><header><strong>Kontakt {index + 1}</strong><label><input checked={guardian.isPrimary} name="primary-guardian" type="radio" onChange={() => updateGuardian(index, { isPrimary: true })} /> Primarni kontakt</label></header><div className="student-edit-grid"><Field label="Ime" required><input required maxLength={100} value={guardian.firstName} onChange={(event) => updateGuardian(index, { firstName: event.target.value })} /></Field><Field label="Prezime" required><input required maxLength={100} value={guardian.lastName} onChange={(event) => updateGuardian(index, { lastName: event.target.value })} /></Field><Field label="Odnos"><input maxLength={100} value={guardian.relationship ?? ''} onChange={(event) => updateGuardian(index, { relationship: event.target.value })} /></Field><Field label="E-mail"><input type="email" maxLength={320} value={guardian.email ?? ''} onChange={(event) => updateGuardian(index, { email: event.target.value })} /></Field><Field label="Telefon"><input type="tel" maxLength={32} value={guardian.phone ?? ''} onChange={(event) => updateGuardian(index, { phone: event.target.value })} /></Field></div></article>)}</div>
          </EditCard>

          <FutureCard className="student-edit-card--additional" title="Dodatne informacije" text="Bilješke nastavnika i dodatni profilni podaci bit će dostupni nakon zaključavanja pravila privatnosti i ovlasti." />
          <FutureCard className="student-edit-card--progress" title="Napredak i postavke" text="Procjenu znanja ne uređuje nastavnik na ovom administrativnom ekranu. Prikaz dolazi s Knowledge/Evidence modelom." />
          <FutureCard className="student-edit-card--privacy" title="Privatnost i vidljivost" text="Postavke vidljivosti pojavit će se tek kada stvarna privacy i permissions funkcionalnost bude implementirana." />
          <footer className="student-edit-footer"><div className="student-edit-note"><span aria-hidden="true">ⓘ</span><p><strong>Napomena</strong> Promjene administrativnih podataka odmah utječu na povezane module. Povijest promjena i detaljni audit dolaze u zasebnoj fazi.</p></div><div><span>Posljednje ažuriranje: {formatTimestamp(form.updatedAtUtc)}</span><Link to={`/students/${studentId}`}>Odustani</Link><button disabled={saving} type="submit">Spremi promjene</button></div></footer>
        </main>
      </form>

      {confirmArchive && <div className="student-edit-modal-backdrop" role="presentation"><section aria-labelledby="archive-title" aria-modal="true" className="student-edit-modal" role="dialog"><span aria-hidden="true">!</span><h2 id="archive-title">Arhivirati učenika?</h2><p>{name} više se neće prikazivati među aktivnim učenicima. Podaci se ne brišu.</p><div><button disabled={archiving} onClick={() => setConfirmArchive(false)} type="button">Odustani</button><button className="is-danger" disabled={archiving} onClick={() => void archive()} type="button">{archiving ? 'Arhiviranje…' : 'Arhiviraj učenika'}</button></div></section></div>}
    </section>
  )
}

function toForm(student: StudentEditModel): EditForm { return { ...student, nickname: student.nickname ?? '', dateOfBirth: student.dateOfBirth ?? '', schoolName: student.schoolName ?? '', gender: student.gender ?? '', email: student.email ?? '', phone: student.phone ?? '', programId: student.programId ?? '', deliveryMode: student.deliveryMode ?? '', groupId: student.groupId ?? '', guardians: student.guardians.map((guardian) => ({ ...guardian })) } }
function optional(value: string) { return value.trim() || null }
function formatTimestamp(value: string) { return new Intl.DateTimeFormat('hr-HR', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Zagreb' }).format(new Date(value)) }
function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: ReactNode }) { return <label className="student-edit-field"><span>{label}{required && <b> *</b>}</span>{children}</label> }
function EditCard({ className = '', icon, title, action, children }: { readonly className?: string; readonly icon: string; readonly title: string; readonly action?: ReactNode; readonly children: ReactNode }) { return <section className={`student-edit-card ${className}`}><header><span aria-hidden="true">{icon}</span><h2>{title}</h2>{action}</header>{children}</section> }
function FutureCard({ className, title, text }: { readonly className: string; readonly title: string; readonly text: string }) { return <section className={`student-edit-card student-edit-future ${className}`}><header><span aria-hidden="true">○</span><h2>{title}</h2><small>Sljedeća faza</small></header><p>{text}</p></section> }
function EditState({ title, message, children }: { readonly title: string; readonly message: string; readonly children?: ReactNode }) { return <section className="student-edit-state"><span>5</span><h1>{title}</h1><p>{message}</p>{children}</section> }
