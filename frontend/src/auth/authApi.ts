import { environment } from '../config/environment.ts'

export interface AuthSession {
  readonly email: string
  readonly accountType: 'Teacher'
  readonly expiresAtUtc: string
}

export class ApiError extends Error {
  public readonly status: number
  public readonly code: string

  constructor(
    status: number,
    code: string,
    message: string,
  ) {
    super(message)
    this.status = status
    this.code = code
  }
}

interface ProblemResponse {
  readonly title?: string
  readonly code?: string
}

interface CsrfResponse {
  readonly token: string
}

const authBaseUrl = `${environment.apiBaseUrl}/auth`

export const authEvents = {
  unauthorized: 'plus5:auth-required',
  forbidden: 'plus5:access-denied',
} as const

export async function getSession(notify = true): Promise<AuthSession | null> {
  const response = await fetch(`${authBaseUrl}/session`, {
    credentials: 'include',
    headers: { Accept: 'application/json' },
  })

  if (response.status === 401) {
    if (notify) window.dispatchEvent(new Event(authEvents.unauthorized))
    return null
  }

  await ensureSuccess(response)
  return (await response.json()) as AuthSession
}

export function register(email: string, password: string) {
  return post('/register', { email, password }, false)
}

export function verifyEmail(email: string, token: string) {
  return post('/verify-email', { email, token }, false)
}

export function resendVerification(email: string) {
  return post('/resend-verification', { email }, false)
}

export function login(email: string, password: string) {
  return post('/login', { email, password }, false)
}

export function forgotPassword(email: string) {
  return post('/forgot-password', { email }, false)
}

export function resetPassword(email: string, token: string, newPassword: string) {
  return post('/reset-password', { email, token, newPassword }, false)
}

export function changePassword(currentPassword: string, newPassword: string) {
  return post('/change-password', { currentPassword, newPassword })
}

export function logout() {
  return post('/logout', {})
}

async function post(path: string, body: object, notify = true): Promise<void> {
  const csrfResponse = await fetch(`${authBaseUrl}/csrf`, {
    credentials: 'include',
    headers: { Accept: 'application/json' },
  })
  await ensureSuccess(csrfResponse, notify)
  const csrf = (await csrfResponse.json()) as CsrfResponse

  const response = await fetch(`${authBaseUrl}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': csrf.token,
    },
    body: JSON.stringify(body),
  })
  await ensureSuccess(response, notify)
}

async function ensureSuccess(response: Response, notify = true): Promise<void> {
  if (response.ok) return

  if (notify && response.status === 401) {
    window.dispatchEvent(new Event(authEvents.unauthorized))
  }
  if (response.status === 403) {
    window.dispatchEvent(new Event(authEvents.forbidden))
  }

  let problem: ProblemResponse = {}
  if (response.headers.get('content-type')?.includes('json')) {
    problem = (await response.json()) as ProblemResponse
  }

  throw new ApiError(
    response.status,
    problem.code ?? 'request_failed',
    friendlyMessage(problem.code),
  )
}

function friendlyMessage(code?: string): string {
  switch (code) {
    case 'invalid_credentials':
      return 'E-mail adresa ili lozinka nisu ispravni.'
    case 'email_already_registered':
      return 'Račun s ovom e-mail adresom već postoji.'
    case 'invalid_or_expired_token':
      return 'Jednokratni kod nije valjan ili je istekao.'
    case 'password_policy_failed':
      return 'Lozinka ne zadovoljava navedena sigurnosna pravila.'
    case 'invalid_current_password':
      return 'Trenutačna lozinka nije ispravna.'
    case 'too_many_requests':
      return 'Previše pokušaja. Pričekajte prije ponovnog pokušaja.'
    case 'invalid_csrf_token':
      return 'Sigurnosna potvrda zahtjeva je istekla. Osvježite stranicu.'
    default:
      return 'Zahtjev trenutačno nije moguće izvršiti.'
  }
}
