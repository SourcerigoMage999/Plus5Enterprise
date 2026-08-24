import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { authEvents, getSession, logout } from './authApi.ts'
import { AuthContext, useAuth, type AuthState } from './authContextState.ts'

export function AuthProvider({ children }: { readonly children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    status: 'loading',
    session: null,
    expired: false,
  })

  async function refresh() {
    try {
      const session = await getSession(false)
      setState(
        session
          ? { status: 'authenticated', session, expired: false }
          : { status: 'anonymous', session: null, expired: false },
      )
      return session !== null
    } catch {
      setState({ status: 'error', session: null, expired: false })
      return false
    }
  }

  async function signOut() {
    await logout()
    setState({ status: 'anonymous', session: null, expired: false })
  }

  useEffect(() => {
    let active = true
    void getSession(false)
      .then((session) => {
        if (active) {
          setState(
            session
              ? { status: 'authenticated', session, expired: false }
              : { status: 'anonymous', session: null, expired: false },
          )
        }
      })
      .catch(() => {
        if (active) setState({ status: 'error', session: null, expired: false })
      })
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    const handleUnauthorized = () => {
      queueMicrotask(() => setState({ status: 'anonymous', session: null, expired: true }))
    }
    window.addEventListener(authEvents.unauthorized, handleUnauthorized)
    return () => window.removeEventListener(authEvents.unauthorized, handleUnauthorized)
  }, [])

  const value = { state, refresh, signOut }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function ProtectedRoute({ children }: { readonly children: ReactNode }) {
  const { state } = useAuth()
  const location = useLocation()

  if (state.status === 'loading') {
    return <AuthLoadingState />
  }

  if (state.status === 'error') {
    return <AuthUnavailableState />
  }

  if (state.status === 'anonymous') {
    return (
      <Navigate
        replace
        state={{ from: location.pathname }}
        to={state.expired ? '/auth/session-expired' : '/auth/login'}
      />
    )
  }

  return children
}

export function AuthBoundaryNavigation() {
  const navigate = useNavigate()

  useEffect(() => {
    const handleForbidden = () => navigate('/auth/access-denied', { replace: true })
    window.addEventListener(authEvents.forbidden, handleForbidden)
    return () => window.removeEventListener(authEvents.forbidden, handleForbidden)
  }, [navigate])

  return null
}

function AuthLoadingState() {
  return (
    <main className="auth-layout" aria-busy="true">
      <section className="auth-card">
        <p className="auth-eyebrow">PLUS 5</p>
        <h1>Provjera sesije…</h1>
        <p>Sigurno provjeravamo pristup učiteljskoj aplikaciji.</p>
      </section>
    </main>
  )
}

function AuthUnavailableState() {
  const { refresh } = useAuth()

  return (
    <main className="auth-layout">
      <section className="auth-card">
        <p className="auth-eyebrow">PLUS 5</p>
        <h1>Prijavu trenutačno nije moguće provjeriti</h1>
        <p>Provjerite vezu i pokušajte ponovno. Zaštićeni sadržaj nije prikazan.</p>
        <button className="auth-button" type="button" onClick={() => void refresh()}>
          Pokušaj ponovno
        </button>
      </section>
    </main>
  )
}
