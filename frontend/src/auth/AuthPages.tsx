import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import plus5Logo from '../assets/plus5-logo.png'
import {
  ApiError,
  forgotPassword,
  login,
  register,
  resendVerification,
  resetPassword,
  verifyEmail,
} from './authApi.ts'
import { useAuth } from './authContextState.ts'

const passwordHint = 'Najmanje 12 znakova, veliko i malo slovo, broj i simbol.'

export function LoginPage() {
  const { state, refresh } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const form = useAsyncForm(async () => {
    await login(email, password)
    await refresh()
    const destination = readReturnPath(location.state)
    navigate(destination, { replace: true })
  })

  if (state.status === 'authenticated') return <Navigate replace to="/" />

  return (
    <AuthCard title="Prijava za učitelje" description="Pristupite svom sigurnom PLUS 5 radnom prostoru.">
      <form onSubmit={form.submit} className="auth-form">
        <EmailField value={email} onChange={setEmail} />
        <PasswordField label="Lozinka" value={password} onChange={setPassword} />
        <FormFeedback {...form} />
        <button className="auth-button" disabled={form.pending}>Prijavi se</button>
      </form>
      <div className="auth-links">
        <Link to="/auth/forgot-password">Zaboravljena lozinka?</Link>
        <Link to="/auth/register">Novi Teacher račun</Link>
      </div>
    </AuthCard>
  )
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const form = useAsyncForm(async () => {
    await register(email, password)
    navigate('/auth/verify-email', { state: { email } })
  })

  return (
    <AuthCard title="Otvorite Teacher račun" description="Registracija je namijenjena isključivo učiteljima.">
      <form onSubmit={form.submit} className="auth-form">
        <EmailField value={email} onChange={setEmail} />
        <PasswordField label="Lozinka" value={password} onChange={setPassword} hint={passwordHint} />
        <FormFeedback {...form} />
        <button className="auth-button" disabled={form.pending}>Registriraj se</button>
      </form>
      <div className="auth-links"><Link to="/auth/login">Već imam račun</Link></div>
    </AuthCard>
  )
}

export function VerifyEmailPage() {
  const location = useLocation()
  const [email, setEmail] = useState(readEmail(location.state))
  const [token, setToken] = useState('')
  const [verified, setVerified] = useState(false)
  const form = useAsyncForm(async () => {
    await verifyEmail(email, token)
    setVerified(true)
  })
  const resend = useAsyncForm(() => resendVerification(email))

  return (
    <AuthCard title="Potvrdite e-mail" description="Unesite jednokratni kod poslan na vašu adresu.">
      {verified ? (
        <AuthSuccess>Adresa je potvrđena. <Link to="/auth/login">Nastavite na prijavu.</Link></AuthSuccess>
      ) : (
        <form onSubmit={form.submit} className="auth-form">
          <EmailField value={email} onChange={setEmail} />
          <TokenField value={token} onChange={setToken} />
          <FormFeedback {...form} />
          <button className="auth-button" disabled={form.pending}>Potvrdi e-mail</button>
        </form>
      )}
      {!verified && (
        <form onSubmit={resend.submit} className="auth-inline-action">
          <button disabled={resend.pending}>Pošalji novi kod</button>
          <FormFeedback {...resend} success="Ako račun čeka potvrdu, novi kod je poslan." />
        </form>
      )}
    </AuthCard>
  )
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const form = useAsyncForm(() => forgotPassword(email))

  return (
    <AuthCard title="Zaboravljena lozinka" description="Poslat ćemo upute ako račun s tom adresom postoji.">
      <form onSubmit={form.submit} className="auth-form">
        <EmailField value={email} onChange={setEmail} />
        <FormFeedback {...form} success="Ako račun postoji, upute su poslane." />
        <button className="auth-button" disabled={form.pending}>Pošalji upute</button>
      </form>
      <div className="auth-links"><Link to="/auth/reset-password">Već imam kod</Link></div>
    </AuthCard>
  )
}

export function ResetPasswordPage() {
  const [email, setEmail] = useState('')
  const [token, setToken] = useState('')
  const [password, setPassword] = useState('')
  const form = useAsyncForm(() => resetPassword(email, token, password))

  return (
    <AuthCard title="Postavite novu lozinku" description="Kod se može iskoristiti samo jednom i vrijedi ograničeno vrijeme.">
      <form onSubmit={form.submit} className="auth-form">
        <EmailField value={email} onChange={setEmail} />
        <TokenField value={token} onChange={setToken} />
        <PasswordField label="Nova lozinka" value={password} onChange={setPassword} hint={passwordHint} />
        <FormFeedback {...form} success="Lozinka je promijenjena. Sve stare sesije su odjavljene." />
        <button className="auth-button" disabled={form.pending}>Spremi novu lozinku</button>
      </form>
      <div className="auth-links"><Link to="/auth/login">Povratak na prijavu</Link></div>
    </AuthCard>
  )
}

export function SessionExpiredPage() {
  return <AuthMessagePage title="Sesija je istekla" message="Radi vaše sigurnosti ponovno se prijavite." />
}

export function AccessDeniedPage() {
  return <AuthMessagePage title="Pristup nije dopušten" message="Nemate dopuštenje za traženu akciju." />
}

function AuthMessagePage({ title, message }: { readonly title: string; readonly message: string }) {
  return (
    <AuthCard title={title} description={message}>
      <Link className="auth-button auth-button--link" to="/auth/login">Nastavi na prijavu</Link>
    </AuthCard>
  )
}

function AuthCard({ title, description, children }: { readonly title: string; readonly description: string; readonly children: ReactNode }) {
  return (
    <main className="auth-layout">
      <section className="auth-card">
        <Link className="auth-brand" to="/" aria-label="PLUS 5">
          <img className="auth-brand-logo" src={plus5Logo} alt="PLUS 5" />
        </Link>
        <p className="auth-eyebrow">Učiteljska aplikacija</p>
        <h1>{title}</h1>
        <p className="auth-description">{description}</p>
        {children}
      </section>
    </main>
  )
}

function EmailField({ value, onChange }: FieldProps) {
  return <label>E-mail<input required type="email" autoComplete="email" maxLength={320} value={value} onChange={(event) => onChange(event.target.value)} /></label>
}

function PasswordField({ label, value, onChange, hint }: FieldProps & { readonly label: string; readonly hint?: string }) {
  return <label>{label}<input required type="password" autoComplete="current-password" minLength={12} maxLength={128} value={value} onChange={(event) => onChange(event.target.value)} />{hint && <small>{hint}</small>}</label>
}

function TokenField({ value, onChange }: FieldProps) {
  return <label>Jednokratni kod<input required type="text" autoComplete="one-time-code" minLength={32} maxLength={128} value={value} onChange={(event) => onChange(event.target.value.trim())} /></label>
}

interface FieldProps { readonly value: string; readonly onChange: (value: string) => void }

function AuthSuccess({ children }: { readonly children: ReactNode }) {
  return <p className="auth-feedback auth-feedback--success" role="status">{children}</p>
}

function FormFeedback({ error, submitted, success }: { readonly error: string; readonly submitted: boolean; readonly success?: string; readonly pending?: boolean; readonly submit?: (event: FormEvent) => void }) {
  if (error) return <p className="auth-feedback auth-feedback--error" role="alert">{error}</p>
  if (submitted && success) return <p className="auth-feedback auth-feedback--success" role="status">{success}</p>
  return null
}

function useAsyncForm(action: () => Promise<unknown>) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const [submitted, setSubmitted] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError('')
    setSubmitted(false)
    try {
      await action()
      setSubmitted(true)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Zahtjev trenutačno nije moguće izvršiti.')
    } finally {
      setPending(false)
    }
  }

  return { pending, error, submitted, submit }
}

function readEmail(state: unknown): string {
  return typeof state === 'object' && state !== null && 'email' in state && typeof state.email === 'string' ? state.email : ''
}

function readReturnPath(state: unknown): string {
  if (typeof state !== 'object' || state === null || !('from' in state) || typeof state.from !== 'string') return '/'
  return state.from.startsWith('/') && !state.from.startsWith('//') ? state.from : '/'
}
