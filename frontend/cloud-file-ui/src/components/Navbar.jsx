import { Link, useNavigate } from "react-router-dom"
import { useAuth } from "../context/AuthContext"

// byte'ı KB/MB/GB'ye çevirir
const fmt = b =>
  b < 1048576 ? (b / 1024).toFixed(0) + "KB" :
  b < 1073741824 ? (b / 1048576).toFixed(1) + "MB" :
  (b / 1073741824).toFixed(1) + "GB"

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth()
  const nav = useNavigate()

  // kota yüzdesini hesapla (bar rengi için)
  const pct = user ? Math.round(user.storageUsed / user.storageQuota * 100) : 0

  return (
    <nav className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
      <Link to="/" className="flex items-center gap-2">
        <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center text-white text-sm">☁️</div>
        <span className="font-bold text-gray-900">CloudFile</span>
      </Link>

      <div className="flex items-center gap-4">
        {/* depolama kullanım barı */}
        <div className="hidden md:block text-right">
          <p className="text-xs text-gray-500">
            {fmt(user?.storageUsed || 0)} / {fmt(user?.storageQuota || 0)}
          </p>
          <div className="w-28 bg-gray-200 rounded-full h-1.5 mt-1">
            <div
              className={`h-1.5 rounded-full ${pct > 80 ? "bg-red-500" : "bg-blue-500"}`}
              style={{ width: `${Math.min(pct, 100)}%` }}
            />
          </div>
        </div>

        {isAdmin() && (
          <Link to="/admin" className="text-sm text-purple-600 font-medium hidden md:block">
            👑 Admin
          </Link>
        )}

        <span className="text-sm text-gray-700 font-medium">
          {user?.firstName} {user?.lastName}
        </span>

        <button
          onClick={() => { logout(); nav("/login") }}
          className="text-sm text-red-500 hover:text-red-700"
        >
          Çıkış
        </button>
      </div>
    </nav>
  )
}
