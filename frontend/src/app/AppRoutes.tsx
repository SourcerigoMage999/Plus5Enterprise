import { Route, Routes } from 'react-router'
import { AppShell } from './AppShell.tsx'
import { FoundationPage, NotFoundPage } from './FoundationPage.tsx'
import { navigationItems } from './navigation.ts'

const dashboard = navigationItems[0]
const moduleItems = navigationItems.slice(1)

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<FoundationPage title={dashboard.label} />} />
        {moduleItems.map((item) => (
          <Route
            key={item.id}
            path={item.path.slice(1)}
            element={<FoundationPage title={item.label} />}
          />
        ))}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
