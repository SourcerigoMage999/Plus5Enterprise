import './App.css'
import { BrowserRouter } from 'react-router'
import { AppRoutes } from './app/AppRoutes.tsx'
import { AuthProvider } from './auth/AuthContext.tsx'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
