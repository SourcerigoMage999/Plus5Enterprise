import { environment } from '../config/environment.ts'

export class ApiError extends Error {
  public readonly status: number
  public readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

interface ProblemResponse {
  readonly title?: string
  readonly code?: string
}

export const apiEvents = {
  unauthorized: 'plus5:auth-required',
  forbidden: 'plus5:access-denied',
} as const

interface ApiRequestOptions {
  readonly notify?: boolean
  readonly allowUnauthorized?: boolean
}

export async function apiRequest(
  path: string,
  init: RequestInit = {},
  options: ApiRequestOptions = {},
): Promise<Response> {
  const headers = new Headers(init.headers)
  if (!headers.has('Accept')) headers.set('Accept', 'application/json')

  const response = await fetch(`${environment.apiBaseUrl}${path}`, {
    ...init,
    credentials: 'include',
    headers,
  })

  if (options.allowUnauthorized && response.status === 401) return response
  await ensureSuccess(response, options.notify ?? true)
  return response
}

export async function getJson<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  const response = await apiRequest(path, { signal })
  return (await response.json()) as T
}

export async function ensureSuccess(response: Response, notify = true): Promise<void> {
  if (response.ok) return

  if (notify && response.status === 401) {
    window.dispatchEvent(new Event(apiEvents.unauthorized))
  }
  if (response.status === 403) {
    window.dispatchEvent(new Event(apiEvents.forbidden))
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
