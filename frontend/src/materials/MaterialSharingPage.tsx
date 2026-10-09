import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { Link, useParams } from 'react-router'
import { z } from 'zod'
import { ApiError } from '../api/apiClient.ts'
import { GroupUnsavedGuard } from '../groups/GroupUnsavedGuard.tsx'
import { getMaterialSharing, saveMaterialSharing, type MaterialSharingWorkspace } from './materialsApi.ts'
import './MaterialSharingPage.css'

const grantSchema = z.object({
  email: z.string().trim().email('Unesite valjanu e-mail adresu.').max(320),
  access: z.enum(['view', 'use']),
})

const sharingSchema = z.object({
  visibility: z.enum(['private', 'shared']),
  grants: z.array(grantSchema),
}).superRefine((value, context) => {
  if (value.visibility === 'private' && value.grants.length > 0) {
    context.addIssue({ code: 'custom', path: ['grants'], message: 'Privatni materijal ne može imati aktivna dijeljenja.' })
  }
  const emails = new Set<string>()
  value.grants.forEach((grant, index) => {
    const normalized = grant.email.trim().toLocaleLowerCase('en-US')
    if (emails.has(normalized)) {
      context.addIssue({ code: 'custom', path: ['grants', index, 'email'], message: 'Ovaj je Teacher već dodan.' })
    }
    emails.add(normalized)
  })
})

type SharingForm = z.infer<typeof sharingSchema>

export function MaterialSharingPage() {
  const { materialId = '' } = useParams()
  const [workspace, setWorkspace] = useState<MaterialSharingWorkspace | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)
  const saved = useRef(false)
  const form = useForm<SharingForm>({
    resolver: zodResolver(sharingSchema),
    defaultValues: { visibility: 'private', grants: [] },
  })
  const grants = useFieldArray({ control: form.control, name: 'grants' })
  const visibility = useWatch({ control: form.control, name: 'visibility' })

  useEffect(() => {
    const controller = new AbortController()
    getMaterialSharing(materialId, controller.signal)
      .then((result) => {
        setWorkspace(result)
        form.reset({
          visibility: result.visibility,
          grants: result.grants.map(grant => ({ email: grant.email, access: grant.access })),
        })
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof ApiError && requestError.status === 404
          ? 'Materijal nije pronađen ili samo vlasnik može upravljati dijeljenjem.'
          : 'Postavke dijeljenja trenutačno nije moguće učitati.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [form, materialId, revision])

  async function submit(values: SharingForm) {
    if (!workspace) return
    setSaving(true)
    setError(null)
    setSavedMessage(null)
    try {
      await saveMaterialSharing(materialId, {
        expectedRowVersion: workspace.rowVersion,
        visibility: values.visibility,
        grants: values.grants.map(grant => ({
          recipientEmail: grant.email.trim(),
          access: grant.access,
        })),
      })
      const refreshed = await getMaterialSharing(materialId)
      setWorkspace(refreshed)
      form.reset({
        visibility: refreshed.visibility,
        grants: refreshed.grants.map(grant => ({ email: grant.email, access: grant.access })),
      })
      setSavedMessage('Postavke dijeljenja su spremljene.')
    } catch (requestError) {
      setError(requestError instanceof ApiError
        ? requestError.message
        : 'Postavke dijeljenja trenutačno nije moguće spremiti.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <SharingState message="Učitavanje postavki dijeljenja…" />
  if (!workspace) return <SharingState error message={error ?? 'Materijal nije dostupan.'} onRetry={() => {
    setError(null)
    setLoading(true)
    setRevision(value => value + 1)
  }} />

  function setVisibility(next: 'private' | 'shared') {
    form.setValue('visibility', next, { shouldDirty: true, shouldValidate: true })
    if (next === 'private') grants.replace([])
    setSavedMessage(null)
  }

  return <section className="material-sharing" aria-labelledby="material-sharing-title">
    <GroupUnsavedGuard dirty={form.formState.isDirty} saving={saving} saved={saved} />
    <nav className="material-sharing__breadcrumb" aria-label="Putanja"><Link to="/materials">Materijali</Link><span>›</span><Link to={`/materials/${materialId}`}>{workspace.title}</Link><span>›</span><strong>Dijeljenje</strong></nav>
    <header className="material-sharing__heading">
      <div><span className="material-sharing__eyebrow">UPRAVLJANJE PRISTUPOM · VLASNIK MATERIJALA</span><h1 id="material-sharing-title">Dijeljenje materijala</h1><p>Odredite tko može pregledati ili koristiti „{workspace.title}”.</p></div>
      <Link to={`/materials/${materialId}`}>← Povratak na detalj</Link>
    </header>

    <form onSubmit={form.handleSubmit(submit)}>
      <div className="material-sharing__layout">
        <main>
          <section className="material-sharing__card">
            <div className="material-sharing__section-heading"><div><span>1</span><h2>Vidljivost</h2></div><strong className={`material-sharing__status material-sharing__status--${visibility}`}>{visibility === 'shared' ? 'Dijeljeno' : 'Privatno'}</strong></div>
            <div className="material-sharing__visibility" role="radiogroup" aria-label="Vidljivost materijala">
              <label className={visibility === 'private' ? 'is-selected' : ''}>
                <input checked={visibility === 'private'} onChange={() => setVisibility('private')} type="radio" value="private" />
                <span aria-hidden="true">◆</span><strong>Privatno</strong><small>Materijal je dostupan samo vama. Spremanje uklanja sve postojeće grantove.</small>
              </label>
              <label className={visibility === 'shared' ? 'is-selected' : ''}>
                <input checked={visibility === 'shared'} onChange={() => setVisibility('shared')} type="radio" value="shared" />
                <span aria-hidden="true">◎</span><strong>Dijeljeno</strong><small>Pristup imaju samo Teacher računi koje eksplicitno dodate ispod.</small>
              </label>
            </div>
          </section>

          <section className="material-sharing__card">
            <div className="material-sharing__section-heading"><div><span>2</span><h2>Teacher grantovi</h2></div><button disabled={visibility === 'private'} onClick={() => grants.append({ email: '', access: 'view' })} type="button">＋ Dodaj učitelja</button></div>
            <p className="material-sharing__privacy">Unesite točnu e-mail adresu aktivnog PLUS 5 Teacher računa. Aplikacija ne izlaže imenik drugih korisnika.</p>
            {visibility === 'private' && <div className="material-sharing__empty"><strong>Privatni materijal nema grantove.</strong><p>Odaberite „Dijeljeno” kako biste dodali drugog učitelja.</p></div>}
            {visibility === 'shared' && grants.fields.length === 0 && <div className="material-sharing__empty"><strong>Još nitko nema pristup.</strong><p>Dodajte Teacher račun ili ostavite materijal bez aktivnih grantova.</p></div>}
            {visibility === 'shared' && grants.fields.length > 0 && <div className="material-sharing__grants">
              {grants.fields.map((field, index) => <article key={field.id}>
                <label><span>E-mail učitelja</span><input autoComplete="off" maxLength={320} placeholder="ucitelj@example.com" type="email" {...form.register(`grants.${index}.email`)} />{form.formState.errors.grants?.[index]?.email && <small role="alert">{form.formState.errors.grants[index]?.email?.message}</small>}</label>
                <label><span>Razina pristupa</span><select {...form.register(`grants.${index}.access`)}><option value="view">Pregled</option><option value="use">Korištenje</option></select></label>
                <button aria-label={`Ukloni dijeljenje ${index + 1}`} onClick={() => grants.remove(index)} type="button">Ukloni</button>
              </article>)}
            </div>}
            {typeof form.formState.errors.grants?.message === 'string' && <p className="material-sharing__form-error" role="alert">{form.formState.errors.grants.message}</p>}
          </section>
        </main>

        <aside>
          <section className="material-sharing__card material-sharing__rules"><h2>Razine pristupa</h2><dl><div><dt>Pregled</dt><dd>Metadata i Clean sadržaj mogu se pregledati. Nema uređivanja ili ponovnog dijeljenja.</dd></div><div><dt>Korištenje</dt><dd>Uključuje pregled i buduću uporabu u vlastitom Lesson workflowu ili eksplicitno dupliciranje.</dd></div></dl></section>
          <section className="material-sharing__card material-sharing__boundary"><h2>Granica dijeljenja</h2><p>Grant ne prenosi vlasništvo i ne daje pristup učenicima, grupama, Evidenceu, readinessu ili povijesti sati.</p></section>
        </aside>
      </div>

      <footer className="material-sharing__actions">
        <div aria-live="polite">{error && <span role="alert">{error}</span>}{savedMessage && <strong>{savedMessage}</strong>}</div>
        <Link to={`/materials/${materialId}`}>Odustani</Link>
        <button disabled={saving || !form.formState.isDirty} type="submit">{saving ? 'Spremanje…' : visibility === 'private' ? 'Spremi kao privatno' : 'Spremi dijeljenje'}</button>
      </footer>
    </form>
  </section>
}

function SharingState({ message, error = false, onRetry }: { readonly message: string; readonly error?: boolean; readonly onRetry?: () => void }) {
  return <section className={`material-sharing-state${error ? ' material-sharing-state--error' : ''}`} role={error ? 'alert' : 'status'}><strong>{message}</strong>{onRetry && <button onClick={onRetry} type="button">Pokušaj ponovno</button>}<Link to="/materials">Povratak na materijale</Link></section>
}
