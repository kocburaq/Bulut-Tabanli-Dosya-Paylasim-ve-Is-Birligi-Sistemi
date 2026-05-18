import { useState } from "react"
import { Link } from "react-router-dom"
import api from "../api/axios"

export default function ForgotPassword() {
  const [email, setEmail] = useState("")
  const [token, setToken] = useState("")
  const [msg, setMsg] = useState("")
  const [err, setErr] = useState("")
  const [load, setLoad] = useState(false)

  const submit = async e => {
    e.preventDefault(); setErr(""); setMsg(""); setToken("")
    setLoad(true)
    try {
      const r = await api.post("/auth/forgot-password", { email })
      setMsg(r.data.message)
      if (r.data.token) setToken(r.data.token)
    } catch(e) {
      setErr(e.response?.data?.message || "Bir hata oluştu.")
    } finally { setLoad(false) }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-blue-600 rounded-2xl flex items-center justify-center mx-auto mb-4 text-3xl">🔑</div>
          <h1 className="text-2xl font-bold text-gray-900">Şifremi Unuttum</h1>
          <p className="text-gray-500 text-sm mt-1">E-posta adresinizi girin</p>
        </div>

        {err && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">{err}</div>}

        {msg && (
          <div className="bg-green-50 border border-green-200 text-green-700 rounded-lg p-4 text-sm mb-4">
            <p className="font-medium mb-2">{msg}</p>
            {token && (
              <div>
                <p className="text-xs text-gray-500 mb-1">Sıfırlama token'ınız (gerçek projede e-posta ile gelir):</p>
                <div className="bg-white border rounded p-2 font-mono text-xs break-all select-all">{token}</div>
                <Link
                  to={`/reset-password?token=${token}`}
                  className="mt-3 inline-block w-full text-center btn-primary"
                >
                  Şifreyi Sıfırla →
                </Link>
              </div>
            )}
          </div>
        )}

        {!msg && (
          <form onSubmit={submit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">E-posta</label>
              <input
                type="email"
                className="input-field"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="ornek@email.com"
                required
              />
            </div>
            <button type="submit" className="btn-primary w-full" disabled={load}>
              {load ? "Gönderiliyor..." : "Token Oluştur"}
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
