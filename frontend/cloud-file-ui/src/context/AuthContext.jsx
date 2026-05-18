import { createContext, useContext, useState } from "react"
import api from "../api/axios"
const Ctx = createContext(null)
export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => { try { return JSON.parse(localStorage.getItem("user")) } catch { return null } })
  const login = async (email, password) => {
    const r = await api.post("/auth/login", { email, password })
    localStorage.setItem("token", r.data.token)
    const me = await api.get("/auth/me")
    localStorage.setItem("user", JSON.stringify(me.data))
    setUser(me.data); return me.data
  }
  const register = async (email, password, firstName, lastName) => {
    const r = await api.post("/auth/register", { email, password, firstName, lastName })
    localStorage.setItem("token", r.data.token)
    const me = await api.get("/auth/me")
    localStorage.setItem("user", JSON.stringify(me.data))
    setUser(me.data); return me.data
  }
  const logout = () => { localStorage.clear(); setUser(null) }
  const isAdmin = () => user?.role === "Admin"
  return <Ctx.Provider value={{ user, login, register, logout, isAdmin }}>{children}</Ctx.Provider>
}
export const useAuth = () => useContext(Ctx)
