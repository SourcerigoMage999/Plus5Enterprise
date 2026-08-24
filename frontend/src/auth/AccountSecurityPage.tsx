import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, changePassword } from './authApi.ts'

export function AccountSecurityPage() {
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError('')
    try {
      await changePassword(currentPassword, newPassword)
      navigate('/auth/session-expired', { replace: true })
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Lozinku trenutačno nije moguće promijeniti.')
    } finally {
      setPending(false)
    }
  }

  return (
    <section className="foundation-page">
      <div className="foundation-page__card account-security">
        <p className="foundation-page__eyebrow">Sigurnost računa</p>
        <h1>Promjena lozinke</h1>
        <p className="foundation-page__description">Promjena lozinke odjavljuje sve aktivne sesije, uključujući ovu.</p>
        <form className="auth-form" onSubmit={submit}>
          <label>Trenutačna lozinka<input required type="password" autoComplete="current-password" maxLength={128} value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} /></label>
          <label>Nova lozinka<input required type="password" autoComplete="new-password" minLength={12} maxLength={128} value={newPassword} onChange={(event) => setNewPassword(event.target.value)} /><small>Najmanje 12 znakova, veliko i malo slovo, broj i simbol.</small></label>
          {error && <p className="auth-feedback auth-feedback--error" role="alert">{error}</p>}
          <button className="auth-button" disabled={pending}>Promijeni lozinku</button>
        </form>
      </div>
    </section>
  )
}
