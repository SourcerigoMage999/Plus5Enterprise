import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/apiClient.ts'
import {
  createStudent,
  getStudentCreateOptions,
  type StudentCreateOptions,
  type StudentDeliveryMode,
  type StudentStatus,
} from './studentsApi.ts'
import './StudentCreatePage.css'

interface FormState {
  firstName: string
  lastName: string
  schoolGradeId: string
  schoolName: string
  dateOfBirth: string
  gender: string
  email: string
  phone: string
  programId: string
  deliveryMode: '' | StudentDeliveryMode
  groupId: string
  status: StudentStatus
  guardianFirstName: string
  guardianLastName: string
  guardianEmail: string
  guardianPhone: string
}

const initialForm: FormState = {
  firstName: '', lastName: '', schoolGradeId: '', schoolName: '', dateOfBirth: '',
  gender: '', email: '', phone: '', programId: '', deliveryMode: '', groupId: '',
  status: 'active', guardianFirstName: '', guardianLastName: '', guardianEmail: '', guardianPhone: '',
}

export function StudentCreatePage() {
  const navigate = useNavigate()
  const [form, setForm] = useState(initialForm)
  const [options, setOptions] = useState<StudentCreateOptions | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    getStudentCreateOptions(form.programId || undefined, controller.signal)
      .then(setOptions)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError
          ? requestError.message
          : 'Podatke obrasca trenutačno nije moguće učitati.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [form.programId])

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  function changeProgram(programId: string) {
    setLoading(true)
    setForm((current) => ({
      ...current,
      programId,
      deliveryMode: programId ? current.deliveryMode || 'individual' : '',
      groupId: '',
    }))
    setError(null)
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return

    const guardianStarted = Boolean(
      form.guardianFirstName.trim() || form.guardianLastName.trim()
      || form.guardianEmail.trim() || form.guardianPhone.trim(),
    )
    if (guardianStarted && (!form.guardianFirstName.trim() || !form.guardianLastName.trim())) {
      setError('Za skrbnika unesite i ime i prezime.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      const result = await createStudent({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        schoolGradeId: form.schoolGradeId,
        schoolName: optional(form.schoolName),
        dateOfBirth: optional(form.dateOfBirth),
        gender: optional(form.gender),
        email: optional(form.email),
        phone: optional(form.phone),
        programId: optional(form.programId),
        deliveryMode: form.deliveryMode || null,
        groupId: form.deliveryMode === 'group' ? optional(form.groupId) : null,
        status: form.status,
        guardian: guardianStarted ? {
          firstName: form.guardianFirstName.trim(),
          lastName: form.guardianLastName.trim(),
          email: optional(form.guardianEmail),
          phone: optional(form.guardianPhone),
        } : null,
      })
      navigate(`/students/${result.id}`, { replace: true, state: { created: true } })
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Učenika trenutačno nije moguće spremiti.')
    } finally {
      setSaving(false)
    }
  }

  const selectedProgram = options?.programs.find((item) => item.id === form.programId)
  const selectedGroup = options?.groups.find((item) => item.id === form.groupId)
  const selectedGrade = options?.schoolGrades.find((item) => item.id === form.schoolGradeId)
  const noGrades = !loading && options?.schoolGrades.length === 0

  return (
    <section className="student-create-page" aria-labelledby="student-create-title">
      <nav className="student-create-breadcrumb" aria-label="Putanja">
        <Link to="/students">Učenici</Link><span aria-hidden="true">›</span><span>Novi učenik</span>
      </nav>
      <header className="student-create-hero">
        <div>
          <h1 id="student-create-title">Novi učenik</h1>
          <p>Unesite osnovne podatke i organizaciju nastave za novog učenika.</p>
        </div>
      </header>

      {error && <div className="student-create-alert" role="alert">{error}</div>}
      {noGrades && (
        <div className="student-create-alert" role="alert">
          Nije konfiguriran nijedan školski razred. Dodavanje učenika bit će dostupno nakon unosa kataloga razreda.
        </div>
      )}

      <form className="student-create-layout" onSubmit={(event) => void submit(event)}>
        <div className="student-create-form-card">
          <FormSection icon="●" title="Osnovni podaci">
            <div className="student-create-grid student-create-grid--two">
              <Field label="Ime" required><input autoComplete="given-name" maxLength={100} required value={form.firstName} onChange={(e) => update('firstName', e.target.value)} /></Field>
              <Field label="Prezime" required><input autoComplete="family-name" maxLength={100} required value={form.lastName} onChange={(e) => update('lastName', e.target.value)} /></Field>
              <Field label="Datum rođenja"><input type="date" value={form.dateOfBirth} onChange={(e) => update('dateOfBirth', e.target.value)} /></Field>
              <Field label="Spol"><select value={form.gender} onChange={(e) => update('gender', e.target.value)}><option value="">Odaberite</option><option>Žensko</option><option>Muško</option><option>Drugo</option></select></Field>
              <Field label="Razred" required><select required disabled={loading || noGrades} value={form.schoolGradeId} onChange={(e) => update('schoolGradeId', e.target.value)}><option value="">Odaberite razred</option>{options?.schoolGrades.map((grade) => <option key={grade.id} value={grade.id}>{grade.code ? `${grade.code} — ${grade.name}` : grade.name}</option>)}</select></Field>
              <Field label="Škola"><input maxLength={200} placeholder="Naziv škole" value={form.schoolName} onChange={(e) => update('schoolName', e.target.value)} /></Field>
            </div>
          </FormSection>

          <FormSection icon="✉" title="Kontakt podaci">
            <div className="student-create-grid student-create-grid--two">
              <Field label="E-mail učenika"><input autoComplete="email" maxLength={320} type="email" placeholder="ucenik@email.com" value={form.email} onChange={(e) => update('email', e.target.value)} /></Field>
              <Field label="Telefon učenika"><input autoComplete="tel" maxLength={32} type="tel" placeholder="+385 ..." value={form.phone} onChange={(e) => update('phone', e.target.value)} /></Field>
            </div>
          </FormSection>

          <FormSection icon="♙" title="Roditelj / skrbnik" optional>
            <div className="student-create-grid student-create-grid--two">
              <Field label="Ime"><input maxLength={100} value={form.guardianFirstName} onChange={(e) => update('guardianFirstName', e.target.value)} /></Field>
              <Field label="Prezime"><input maxLength={100} value={form.guardianLastName} onChange={(e) => update('guardianLastName', e.target.value)} /></Field>
              <Field label="E-mail"><input maxLength={320} type="email" value={form.guardianEmail} onChange={(e) => update('guardianEmail', e.target.value)} /></Field>
              <Field label="Telefon"><input maxLength={32} type="tel" value={form.guardianPhone} onChange={(e) => update('guardianPhone', e.target.value)} /></Field>
            </div>
          </FormSection>

          <FormSection icon="▣" title="Program i organizacija nastave" optional>
            <div className="student-create-grid student-create-grid--two">
              <Field label="Program"><select disabled={loading} value={form.programId} onChange={(e) => changeProgram(e.target.value)}><option value="">Bez programa</option>{options?.programs.map((program) => <option key={program.id} value={program.id}>{program.name}</option>)}</select></Field>
              <Field label="Način rada"><select disabled={!form.programId} value={form.deliveryMode} onChange={(e) => { update('deliveryMode', e.target.value as FormState['deliveryMode']); update('groupId', '') }}><option value="">Odaberite</option><option value="individual">Individualno</option><option value="group">Grupa</option></select></Field>
              {form.deliveryMode === 'group' && <Field label="Grupa" required><select required disabled={loading} value={form.groupId} onChange={(e) => update('groupId', e.target.value)}><option value="">Odaberite grupu</option>{options?.groups.filter((group) => group.activeMemberCount < group.capacity).map((group) => <option key={group.id} value={group.id}>{group.name} ({group.activeMemberCount}/{group.capacity})</option>)}</select></Field>}
            </div>
          </FormSection>

          <FormSection icon="✓" title="Status učenika">
            <div className="student-status-options">
              {([['active', 'Aktivan', 'Učenik redovito pohađa nastavu'], ['on_hold', 'Na čekanju', 'Upis je privremeno pauziran'], ['inactive', 'Neaktivan', 'Učenik trenutačno ne pohađa nastavu']] as const).map(([value, label, description]) => (
                <label key={value} className={`student-status-option student-status-option--${value}`}>
                  <input checked={form.status === value} name="status" type="radio" value={value} onChange={() => update('status', value)} />
                  <span><strong>{label}</strong><small>{description}</small></span>
                </label>
              ))}
            </div>
          </FormSection>

          <footer className="student-create-actions">
            <Link className="student-create-cancel" to="/students">Odustani</Link>
            <button className="student-create-save" disabled={saving || loading || noGrades} type="submit">{saving ? 'Spremanje…' : 'Spremi učenika'}</button>
          </footer>
        </div>

        <aside className="student-create-aside" aria-label="Sažetak učenika">
          <SummaryCard title="Programi i grupe">
            <SummaryRow label="Program" value={selectedProgram?.name ?? 'Nije odabran'} />
            <SummaryRow label="Način rada" value={form.deliveryMode === 'group' ? 'Grupa' : form.deliveryMode === 'individual' ? 'Individualno' : 'Nije odabran'} />
            <SummaryRow label="Grupa" value={selectedGroup?.name ?? 'Nije odabrana'} />
          </SummaryCard>
          <SummaryCard title="Sažetak">
            <SummaryRow label="Učenik" value={`${form.firstName} ${form.lastName}`.trim() || 'Novi učenik'} />
            <SummaryRow label="Razred" value={selectedGrade?.code ?? selectedGrade?.name ?? 'Nije odabran'} />
            <SummaryRow label="Status" value={form.status === 'active' ? 'Aktivan' : form.status === 'on_hold' ? 'Na čekanju' : 'Neaktivan'} accent />
          </SummaryCard>
          <p className="student-create-aside-note">Obavezna su samo polja označena zvjezdicom. Program možete dodijeliti i kasnije.</p>
        </aside>
      </form>
    </section>
  )
}

export function StudentCreatedBoundaryPage() {
  const { studentId } = useParams()
  return (
    <section className="student-created-boundary">
      <span aria-hidden="true">✓</span>
      <h1>Učenik je uspješno spremljen</h1>
      <p>Dosje učenika bit će dostupan u sljedećoj podfazi.</p>
      <small>Identifikator: {studentId}</small>
      <Link to="/students">Povratak na popis učenika</Link>
    </section>
  )
}

function optional(value: string) { return value.trim() || null }

function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: React.ReactNode }) {
  return <label className="student-create-field"><span>{label}{required && <b aria-hidden="true"> *</b>}</span>{children}</label>
}

function FormSection({ icon, title, optional: isOptional, children }: { readonly icon: string; readonly title: string; readonly optional?: boolean; readonly children: React.ReactNode }) {
  return <section className="student-create-section"><header><span aria-hidden="true">{icon}</span><h2>{title}</h2>{isOptional && <small>Opcionalno</small>}</header>{children}</section>
}

function SummaryCard({ title, children }: { readonly title: string; readonly children: React.ReactNode }) {
  return <section className="student-create-summary"><h2>{title}</h2>{children}</section>
}

function SummaryRow({ label, value, accent }: { readonly label: string; readonly value: string; readonly accent?: boolean }) {
  return <div className="student-create-summary-row"><span>{label}</span><strong className={accent ? 'is-accent' : ''}>{value}</strong></div>
}
