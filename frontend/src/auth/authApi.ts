import { ApiError, apiEvents, apiRequest, postJson } from '../api/apiClient.ts'

export { ApiError }

export interface AuthSession {
  readonly email: string
  readonly accountType: 'Teacher'
  readonly expiresAtUtc: string
}

export const authEvents = apiEvents

export async function getSession(notify = true): Promise<AuthSession | null> {
  const response = await apiRequest('/auth/session', {}, {
    allowUnauthorized: true,
    notify,
  })

  if (response.status === 401) {
    if (notify) window.dispatchEvent(new Event(apiEvents.unauthorized))
    return null
  }

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
  await postJson<void>(`/auth${path}`, body, notify)
}
