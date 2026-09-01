export interface NavigationItem {
  readonly id: string
  readonly label: string
  readonly path: string
}

export const navigationItems = [
  { id: 'dashboard', label: 'Radni stol', path: '/' },
  { id: 'students', label: 'Učenici', path: '/students' },
  { id: 'schedule', label: 'Raspored', path: '/schedule' },
  { id: 'materials', label: 'Materijali', path: '/materials' },
  { id: 'lesson-plans', label: 'Priprema sata', path: '/lesson-plans' },
  { id: 'board', label: 'PLUS 5 Ploča', path: '/board' },
  { id: 'homework', label: 'Domaće zadaće', path: '/homework' },
  { id: 'messages', label: 'Poruke', path: '/messages' },
  { id: 'reports', label: 'Izvještaji', path: '/reports' },
  { id: 'finance', label: 'Financije', path: '/finance' },
  { id: 'settings', label: 'Postavke', path: '/settings' },
] as const satisfies readonly NavigationItem[]

export function findNavigationItem(pathname: string): NavigationItem | undefined {
  const normalizedPath = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname
  return navigationItems.find((item) =>
    item.path === normalizedPath
      || (item.path !== '/' && normalizedPath.startsWith(`${item.path}/`)),
  )
}
