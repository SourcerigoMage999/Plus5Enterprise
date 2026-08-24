import { createContext, useContext } from 'react'
import type { AuthSession } from './authApi.ts'

export type AuthState =
  | { readonly status: 'loading'; readonly session: null; readonly expired: false }
  | { readonly status: 'anonymous'; readonly session: null; readonly expired: boolean }
  | { readonly status: 'authenticated'; readonly session: AuthSession; readonly expired: false }
  | { readonly status: 'error'; readonly session: null; readonly expired: false }

export interface AuthContextValue {
  readonly state: AuthState
  readonly refresh: () => Promise<boolean>
  readonly signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used within AuthProvider.')
  return value
}
