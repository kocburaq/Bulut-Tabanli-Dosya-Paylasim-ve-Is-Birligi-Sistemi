import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"
import { AuthProvider, useAuth } from "./context/AuthContext"
import Login from "./pages/Login"
import Register from "./pages/Register"
import Dashboard from "./pages/Dashboard"
import AdminPanel from "./pages/AdminPanel"
import ForgotPassword from "./pages/ForgotPassword"
import ResetPassword from "./pages/ResetPassword"
const Priv = ({c}) => { const {user}=useAuth(); return user ? c : <Navigate to="/login" replace /> }
const Pub  = ({c}) => { const {user}=useAuth(); return user ? <Navigate to="/" replace /> : c }
const Adm  = ({c}) => { const {user,isAdmin}=useAuth(); if(!user) return <Navigate to="/login" replace />; if(!isAdmin()) return <Navigate to="/" replace />; return c }
export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login"           element={<Pub  c={<Login />} />} />
          <Route path="/register"        element={<Pub  c={<Register />} />} />
          <Route path="/forgot-password" element={<Pub  c={<ForgotPassword />} />} />
          <Route path="/reset-password"  element={<Pub  c={<ResetPassword />} />} />
          <Route path="/"                element={<Priv c={<Dashboard />} />} />
          <Route path="/admin"           element={<Adm  c={<AdminPanel />} />} />
          <Route path="*"                element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
