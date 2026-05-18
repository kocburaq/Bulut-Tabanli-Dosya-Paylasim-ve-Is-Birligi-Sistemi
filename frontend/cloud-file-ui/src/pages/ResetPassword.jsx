import { useState, useEffect } from "react"
import { Link, useSearchParams, useNavigate } from "react-router-dom"
import api from "../api/axios"

export default function ResetPassword() {
  const [searchParams] = useSearchParams()
  const nav = useNavigate()
  const [token] = useState(searchParams.get("token") || "")
  const [password, setPassword] = useState("")
  const [confirm, setConfirm] = useState("")
  const [err, setErr] = useState("")
  const [msg, setMsg] = useState("")
  const [load, setLoad] = useState(false)

  const submit = async e => {
    e.preventDefault(); setErr("")
    if (password !== confirm) { setErr("Şifreler eşleşmiyor."); return }
    if (password.length < 6) { setErr("Şifre en az 6 karakter olmalı."); return }
    setLoad(true)
    try {
      const r = await api.post("/auth/reset-password", { token, newPassword: password })
      setMsg(r.data.message)
      setTimeout(() => nav("/login"), 2000)
    } catch(e) {
      setErr(e.response?.data?.message || "Bir hata oluştu.")
    } finally { setLoad(false) }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-blue-600 rounded-2xl flex items-center justify-center mx-auto mb-4 text-3xl">🔒</div>
          <h1 className="text-2xl font-bold text-gray-900">Yeni Şifre Belirle</h1>
          <p className="text-gray-500 text-sm mt-1">Güvenli bir şifre oluşturun</p>
        </div>

        {err && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">{err}</div>}

        {msg ? (
          <div className="bg-green-50 border border-green-200 text-green-700 rounded-lg p-4 text-sm text-center">
            <p className="text-2xl mb-2">✅</p>
            <p className="font-medium">{msg}</p>
            <p className="text-xs mt-1">Giriş sayfasına yönlendiriliyorsunuz...</p>
          </div>
        ) : (
          <form onSubmit={submit} className="space-y-4">
            {!token && (
              <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 rounded-lg p-3 text-sm mb-2">
                Token bulunamadı. Lütfen şifre sıfırlama bağlantısını tekrar kullanın.
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Yeni Şifre</label>
              <input
                type="password"
                className="input-field"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="En az 6 karakter"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Şifre Tekrar</label>
              <input
                type="password"
                className="input-field"
                value={confirm}
                onChange={e => setConfirm(e.target.value)}
                placeholder="Şifrenizi tekrar girin"
                required
              />
            </div>
            <button type="submit" className="btn-primary w-full" disabled={load || !token}>
              {load ? "Kaydediliyor..." : "Şifremi Sıfırla"}
            </button>
          </form>
        )}

        <p className="text-center text-sm text-gray-500 mt-6">
          <Link to="/login" className="text-blue-600 hover:underline font-medium">← Giriş Sayfasına Dön</Link>
        </p>
      </div>
    </div>
  )
}
