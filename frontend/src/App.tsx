import './App.css'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { AppRoutes } from './app/AppRoutes.tsx'
import { AuthProvider } from './auth/AuthContext.tsx'

const router = createBrowserRouter([{ path: '*', element: <AuthProvider><AppRoutes /></AuthProvider> }])

function App() { return <RouterProvider router={router} /> }

export default App
