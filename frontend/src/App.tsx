import './App.css'
import { BrowserRouter } from 'react-router'
import { AppRoutes } from './app/AppRoutes.tsx'

function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}

export default App
