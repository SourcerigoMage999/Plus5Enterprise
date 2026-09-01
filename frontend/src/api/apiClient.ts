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

interface CsrfResponse {
  readonly token: string
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

export async function postJson<T>(
  path: string,
  body: object,
  notify = true,
): Promise<T> {
  const csrfResponse = await apiRequest('/auth/csrf', {}, { notify })
  const csrf = (await csrfResponse.json()) as CsrfResponse
  const response = await apiRequest(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': csrf.token,
    },
    body: JSON.stringify(body),
  }, { notify })

  if (response.status === 204) return undefined as T
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
    case 'group_capacity_reached':
      return 'Odabrana grupa je popunjena. Odaberite drugu grupu.'
    case 'group_unavailable':
    case 'group_not_found':
      return 'Odabrana grupa više nije dostupna.'
    case 'program_not_found':
      return 'Odabrani program više nije dostupan.'
    case 'school_grade_not_found':
      return 'Odabrani razred više nije dostupan.'
    case 'group_program_mismatch':
      return 'Odabrana grupa ne pripada odabranom programu.'
    case 'concurrency_conflict':
      return 'Podaci grupe su se promijenili. Provjerite odabir i pokušajte ponovno.'
    default:
      return 'Zahtjev trenutačno nije moguće izvršiti.'
  }
}
