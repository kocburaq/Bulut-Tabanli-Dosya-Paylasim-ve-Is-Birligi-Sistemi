import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { useAuth } from "../context/AuthContext"
export default function Register() {
  const { register } = useAuth(); const nav = useNavigate()
  const [f, setF] = useState({ firstName:"", lastName:"", email:"", password:"" })
  const [err, setErr] = useState(""); const [load, setLoad] = useState(false)
  const submit = async e => {
    e.preventDefault(); setErr("")
    if(f.password.length<6){setErr("Şifre en az 6 karakter olmalı.");return}
    setLoad(true)
    try { await register(f.email, f.password, f.firstName, f.lastName); nav("/") }
    catch(e) { setErr(e.response?.data?.message||"Kayıt başarısız.") }
    finally { setLoad(false) }
  }
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-blue-600 rounded-2xl flex items-center justify-center mx-auto mb-4 text-3xl">☁️</div>
          <h1 className="text-2xl font-bold text-gray-900">CloudFile</h1>
          <p className="text-gray-500 text-sm">Yeni hesap oluşturun</p>
        </div>
        {err && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">{err}</div>}
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div><label className="block text-sm font-medium text-gray-700 mb-1">Ad</label>
              <input className="input-field" value={f.firstName} onChange={e=>setF({...f,firstName:e.target.value})} required /></div>
            <div><label className="block text-sm font-medium text-gray-700 mb-1">Soyad</label>
              <input className="input-field" value={f.lastName} onChange={e=>setF({...f,lastName:e.target.value})} required /></div>
          </div>
          <div><label className="block text-sm font-medium text-gray-700 mb-1">E-posta</label>
            <input type="email" className="input-field" value={f.email} onChange={e=>setF({...f,email:e.target.value})} required /></div>
          <div><label className="block text-sm font-medium text-gray-700 mb-1">Şifre</label>
            <input type="password" className="input-field" value={f.password} onChange={e=>setF({...f,password:e.target.value})} required /></div>
          <button type="submit" className="btn-primary w-full" disabled={load}>{load?"Kayıt yapılıyor...":"Kayıt Ol"}</button>
        </form>
        <p className="text-center text-sm text-gray-500 mt-6">Hesabınız var mı? <Link to="/login" className="text-blue-600 hover:underline font-medium">Giriş Yap</Link></p>
      </div>
    </div>
  )
}
