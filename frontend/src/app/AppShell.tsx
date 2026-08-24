import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import { findNavigationItem, navigationItems } from './navigation.ts'
import { useAuth } from '../auth/authContextState.ts'

export function AppShell() {
  const location = useLocation()
  const currentPage = findNavigationItem(location.pathname)
  const { state, signOut } = useAuth()
  const navigate = useNavigate()

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
            <span className="brand-wordmark">PLUS</span>
            <span className="brand-five" aria-hidden="true">
              5
            </span>
          </NavLink>

          <nav className="primary-navigation" aria-label="Glavna navigacija">
            <p className="primary-navigation__label">Učiteljska aplikacija</p>
            <ul className="primary-navigation__list">
              {navigationItems.map((item, index) => (
                <li key={item.id}>
                  <NavLink
                    className={({ isActive }) =>
                      `primary-navigation__link${isActive ? ' primary-navigation__link--active' : ''}`
                    }
                    end={item.path === '/'}
                    to={item.path}
                  >
                    <span className="primary-navigation__index" aria-hidden="true">
                      {String(index + 1).padStart(2, '0')}
                    </span>
                    <span>{item.label}</span>
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>

          <div className="shell-boundary-note">
            <strong>Korisnički profil</strong>
            <span>{state.status === 'authenticated' ? state.session.email : 'Teacher'}</span>
            <Link to="/account/security">Sigurnost računa</Link>
            <button type="button" onClick={() => void handleLogout()}>Odjava</button>
          </div>
        </aside>

        <div className="shell-content">
          <header className="shell-header">
            <div>
              <p className="shell-header__context">PLUS 5 Enterprise</p>
              <p className="shell-header__title">{currentPage?.label ?? (location.pathname === '/account/security' ? 'Sigurnost računa' : 'Stranica nije pronađena')}</p>
            </div>
            <span className="foundation-badge">Foundation 1.6</span>
          </header>

          <main className="shell-main" id="main-content" tabIndex={-1}>
            <Outlet />
          </main>
        </div>
      </div>
    </>
  )
}
