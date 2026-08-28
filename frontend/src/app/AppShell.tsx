import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import { findNavigationItem, navigationItems } from './navigation.ts'
import { useAuth } from '../auth/authContextState.ts'

export function AppShell() {
  const location = useLocation()
  const currentPage = findNavigationItem(location.pathname)
  const { state, signOut } = useAuth()
  const navigate = useNavigate()
  const isStudentList = currentPage?.id === 'students'
  const teacherLabel = state.status === 'authenticated'
    ? formatTeacherLabel(state.session.email)
    : 'Teacher'
  const teacherInitials = teacherLabel.split(' ').map((part) => part[0]).join('').slice(0, 2)

  async function handleLogout() {
    await signOut()
    navigate('/auth/login', { replace: true })
  }

  return (
    <>
      <a className="skip-link" href="#main-content">
        Preskoči na glavni sadržaj
      </a>
      <div className="app-shell">
        <aside className="shell-sidebar">
          <NavLink className="brand-link" end to="/" aria-label="PLUS 5 — Radni stol">
            <span className="brand-wordmark">Plus</span>
            <span className="brand-five" aria-hidden="true">
              5
            </span>
          </NavLink>

          <nav className="primary-navigation" aria-label="Glavna navigacija">
            <p className="primary-navigation__label">Učiteljska aplikacija</p>
            <ul className="primary-navigation__list">
              {navigationItems.map((item) => (
                <li key={item.id}>
                  <NavLink
                    className={({ isActive }) =>
                      `primary-navigation__link${isActive ? ' primary-navigation__link--active' : ''}`
                    }
                    end={item.path === '/'}
                    to={item.path}
                  >
                    <NavigationIcon id={item.id} />
                    <span>{item.label}</span>
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>

          <div className="shell-boundary-note">
            <span className="shell-profile-avatar" aria-hidden="true">{teacherInitials}</span>
            <span className="shell-profile-copy">
              <strong>{teacherLabel}</strong>
              <small>Učitelj</small>
            </span>
            <Link className="shell-profile-settings" to="/account/security" aria-label="Sigurnost računa">⌄</Link>
            <button className="shell-profile-logout" type="button" onClick={() => void handleLogout()}>Odjava</button>
          </div>
        </aside>

        <div className={`shell-content${isStudentList ? ' shell-content--students' : ''}`}>
          <header className="shell-header">
            {isStudentList ? (
              <div className="shell-global-actions">
                <button type="button" aria-label="Obavijesti" disabled>
                  <svg aria-hidden="true" viewBox="0 0 24 24">
                    <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
                    <path d="M13.73 21a2 2 0 0 1-3.46 0" />
                  </svg>
                  <small>3</small>
                </button>
                <span className="shell-header-avatar" aria-hidden="true">{teacherInitials}</span>
                <span className="shell-header-profile">
                  <strong>{teacherLabel}</strong>
                  <small>Učitelj</small>
                </span>
                <span aria-hidden="true">⌄</span>
              </div>
            ) : (
              <>
                <div>
                  <p className="shell-header__context">PLUS 5 Enterprise</p>
                  <p className="shell-header__title">{currentPage?.label ?? (location.pathname === '/account/security' ? 'Sigurnost računa' : 'Stranica nije pronađena')}</p>
                </div>
                <span className="foundation-badge">Phase 3.1</span>
              </>
            )}
          </header>

          <main className="shell-main" id="main-content" tabIndex={-1}>
            <Outlet />
          </main>
        </div>
      </div>
    </>
  )
}

function formatTeacherLabel(email: string) {
  const localPart = email.split('@')[0] ?? ''
  const parts = localPart.split(/[._-]+/).filter(Boolean)
  if (parts.length < 2) return email
  return parts.map((part) => `${part[0]?.toLocaleUpperCase('hr') ?? ''}${part.slice(1)}`).join(' ')
}

function NavigationIcon({ id }: { readonly id: string }) {
  const paths: Record<string, string> = {
    dashboard: 'M3 11.5 12 4l9 7.5v8a1.5 1.5 0 0 1-1.5 1.5h-5v-6h-5v6h-5A1.5 1.5 0 0 1 3 19.5z',
    students: 'M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2M9.5 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8M17 11a3 3 0 1 0 0-6M21 21v-2a4 4 0 0 0-3-3.87',
    schedule: 'M6 2v4M18 2v4M3 9h18M5 4h14a2 2 0 0 1 2 2v14H3V6a2 2 0 0 1 2-2',
    materials: 'M4 3h10a2 2 0 0 1 2 2v14H6a2 2 0 0 1-2-2zM16 7h4v14H8',
    'lesson-plans': 'M6 2h9l4 4v16H6zM14 2v5h5M9 12h6M9 16h6',
    board: 'M3 4h18v13H3zM8 21h8M12 17v4M8 9h8',
    homework: 'M8 4V2h8v2M6 4h12v18H6zM9 10h6M9 14h6M9 18h4',
    messages: 'M21 12a8 8 0 0 1-8 8H5l-3 2 1-5a9 9 0 1 1 18-5zM8 12h.01M12 12h.01M16 12h.01',
    reports: 'M4 21V10M9 21V4M14 21v-7M19 21V7M2 21h20',
    finance: 'M3 6h15a3 3 0 0 1 3 3v9H5a2 2 0 0 1-2-2zM3 9h15M16 14h2',
    settings: 'M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4',
  }
  return (
    <svg className="primary-navigation__icon" aria-hidden="true" viewBox="0 0 24 24">
      <path d={paths[id] ?? paths.dashboard} />
    </svg>
  )
}
