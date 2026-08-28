import { Route, Routes } from 'react-router'
import { AppShell } from './AppShell.tsx'
import { FoundationPage, NotFoundPage } from './FoundationPage.tsx'
import { navigationItems } from './navigation.ts'
import { AccountSecurityPage } from '../auth/AccountSecurityPage.tsx'
import { AuthBoundaryNavigation, ProtectedRoute } from '../auth/AuthContext.tsx'
import { AccessDeniedPage, ForgotPasswordPage, LoginPage, RegisterPage, ResetPasswordPage, SessionExpiredPage, VerifyEmailPage } from '../auth/AuthPages.tsx'
import { StudentListPage } from '../students/StudentListPage.tsx'

const dashboard = navigationItems[0]
const moduleItems = navigationItems.slice(1).filter((item) => item.id !== 'students')

export function AppRoutes() {
  return (
    <>
      <AuthBoundaryNavigation />
      <Routes>
        <Route path="auth/login" element={<LoginPage />} />
        <Route path="auth/register" element={<RegisterPage />} />
        <Route path="auth/verify-email" element={<VerifyEmailPage />} />
        <Route path="auth/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="auth/reset-password" element={<ResetPasswordPage />} />
        <Route path="auth/session-expired" element={<SessionExpiredPage />} />
        <Route path="auth/access-denied" element={<AccessDeniedPage />} />
        <Route element={<ProtectedRoute><AppShell /></ProtectedRoute>}>
          <Route index element={<FoundationPage title={dashboard.label} />} />
          <Route path="students" element={<StudentListPage />} />
          {moduleItems.map((item) => (
            <Route
              key={item.id}
              path={item.path.slice(1)}
              element={<FoundationPage title={item.label} />}
            />
          ))}
          <Route path="account/security" element={<AccountSecurityPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </>
  )
}
