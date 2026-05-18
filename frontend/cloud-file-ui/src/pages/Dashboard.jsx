import { useState, useEffect, useCallback } from "react"
import Navbar from "../components/Navbar"
import UploadModal from "../components/UploadModal"
import api from "../api/axios"

// boyut formatlama - byte'ı okunabilir hale getiriyor
const fmt = b =>
  b < 1024 ? b + "B" :
  b < 1048576 ? (b / 1024).toFixed(1) + "KB" :
  b < 1073741824 ? (b / 1048576).toFixed(1) + "MB" :
  (b / 1073741824).toFixed(2) + "GB"

const fmtD = d => new Date(d).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" })

// dosya tipine göre emoji göster
const icon = ct =>
  !ct ? "📄" :
  ct.startsWith("image") ? "🖼️" :
  ct.startsWith("video") ? "🎬" :
  ct.startsWith("audio") ? "🎵" :
  ct.includes("pdf") ? "📕" : "📄"

export default function Dashboard() {
  const [folders, setFolders] = useState([])
  const [files, setFiles] = useState([])
  const [cur, setCur] = useState(null)            // şu anki klasör id
  const [bc, setBc] = useState([{ id: null, name: "Ana Dizin" }])  // breadcrumb
  const [search, setSearch] = useState("")
  const [loading, setLoading] = useState(false)
  const [showUp, setShowUp] = useState(false)
  const [showNF, setShowNF] = useState(false)     // yeni klasör formu
  const [nfName, setNfName] = useState("")
  const [err, setErr] = useState("")

  const load = useCallback(async () => {
    setLoading(true)
    setErr("")
    try {
      const [f, fi] = await Promise.all([
        api.get("/folders", { params: { parentId: cur } }),
        api.get("/files", { params: { folderId: cur, search: search || undefined } })
      ])
      setFolders(f.data)
      setFiles(fi.data)
    } catch {
      setErr("Yüklenemedi.")
    } finally {
      setLoading(false)
    }
  }, [cur, search])

  useEffect(() => { load() }, [load])

  // klasör açılınca breadcrumb güncelleniyor
  const openF = f => {
    setCur(f.id)
    setBc(p => [...p, { id: f.id, name: f.name }])
    setSearch("")
  }

  const goBC = i => {
    setCur(bc[i].id)
    setBc(p => p.slice(0, i + 1))
    setSearch("")
  }

  const mkFolder = async () => {
    if (!nfName.trim()) return
    try {
      await api.post("/folders", { name: nfName.trim(), parentFolderId: cur })
      setNfName("")
      setShowNF(false)
      load()
    } catch (e) {
      setErr(e.response?.data?.message || "Hata")
    }
  }

  const delFolder = async id => {
    if (!confirm("Klasörü sil?")) return
    try { await api.delete(`/folders/${id}`); load() }
    catch { setErr("Silinemedi.") }
  }

  const delFile = async id => {
    if (!confirm("Dosyayı sil?")) return
    try { await api.delete(`/files/${id}`); load() }
    catch { setErr("Silinemedi.") }
  }

  const download = async f => {
    try {
      const r = await api.get(`/files/${f.id}/download`, { responseType: "blob" })
      const u = URL.createObjectURL(new Blob([r.data]))
      const a = document.createElement("a")
      a.href = u
      a.download = f.originalFileName
      a.click()
      URL.revokeObjectURL(u)
    } catch {
      setErr("İndirilemedi.")
    }
  }

  // paylaşım linki oluştur ve panoya kopyala
  const share = async id => {
    try {
      const r = await api.post("/share", { fileItemId: id, allowDownload: true })
      const u = `${window.location.origin}/api/share/${r.data.token}/download`
      await navigator.clipboard.writeText(u)
      alert("Link kopyalandı!")
    } catch {
      setErr("Link oluşturulamadı.")
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <div className="max-w-7xl mx-auto px-4 py-6">

        {/* breadcrumb navigasyon */}
        <div className="flex items-center gap-1 text-sm mb-4 flex-wrap">
          {bc.map((c, i) => (
            <span key={i} className="flex items-center gap-1">
              {i > 0 && <span className="text-gray-400">/</span>}
              <button
                onClick={() => goBC(i)}
                className={i === bc.length - 1 ? "font-semibold text-gray-800" : "text-blue-600 hover:underline"}
              >
                {c.name}
              </button>
            </span>
          ))}
        </div>

        <div className="flex flex-col sm:flex-row gap-3 mb-6">
          <input
            className="input-field flex-1"
            placeholder="🔍 Dosya ara..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
          <div className="flex gap-2">
            <button onClick={() => setShowNF(true)} className="btn-secondary">📁 Klasör</button>
            <button onClick={() => setShowUp(true)} className="btn-primary">⬆️ Yükle</button>
          </div>
        </div>

        {/* yeni klasör oluşturma kutusu */}
        {showNF && (
          <div className="card mb-4 flex gap-3">
            <input
              className="input-field flex-1"
              placeholder="Klasör adı..."
              value={nfName}
              onChange={e => setNfName(e.target.value)}
              onKeyDown={e => e.key === "Enter" && mkFolder()}
              autoFocus
            />
            <button onClick={mkFolder} className="btn-primary">Oluştur</button>
            <button onClick={() => setShowNF(false)} className="btn-secondary">İptal</button>
          </div>
        )}

        {err && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">
            {err}
            <button onClick={() => setErr("")} className="ml-2 font-bold">×</button>
          </div>
        )}

        {loading ? (
          <div className="flex justify-center h-48 items-center">
            <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : folders.length === 0 && files.length === 0 ? (
          <div className="text-center py-20 text-gray-400">
            <p className="text-6xl mb-4">📂</p>
            <p className="text-lg font-medium">Bu klasör boş</p>
          </div>
        ) : (
          <div className="space-y-6">

            {/* klasörler */}
            {folders.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-3">
                  Klasörler ({folders.length})
                </h3>
                <div className="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-6 gap-3">
                  {folders.map(f => (
                    <div
                      key={f.id}
                      className="card hover:shadow-md cursor-pointer group relative"
                      onDoubleClick={() => openF(f)}
                    >
                      <div className="text-3xl mb-1">📁</div>
                      <p className="text-sm font-medium text-gray-800 truncate">{f.name}</p>
                      <p className="text-xs text-gray-400">{f.fileCount} dosya</p>
                      <div className="absolute top-2 right-2 hidden group-hover:flex gap-1">
                        <button onClick={e => { e.stopPropagation(); openF(f) }} className="p-1 bg-blue-100 rounded text-xs">→</button>
                        <button onClick={e => { e.stopPropagation(); delFolder(f.id) }} className="p-1 bg-red-100 rounded text-xs">🗑</button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* dosya tablosu */}
            {files.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-3">
                  Dosyalar ({files.length})
                </h3>
                <div className="bg-white rounded-xl border border-gray-100 overflow-hidden">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-gray-50 border-b">
                        <th className="text-left px-4 py-3 text-gray-500 font-medium">Ad</th>
                        <th className="text-left px-4 py-3 text-gray-500 font-medium hidden md:table-cell">Boyut</th>
                        <th className="text-left px-4 py-3 text-gray-500 font-medium hidden md:table-cell">Ver.</th>
                        <th className="text-left px-4 py-3 text-gray-500 font-medium hidden lg:table-cell">Tarih</th>
                        <th className="px-4 py-3"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-50">
                      {files.map(f => (
                        <tr key={f.id} className="hover:bg-gray-50 group">
                          <td className="px-4 py-3">
                            <div className="flex items-center gap-2">
                              <span className="text-xl">{icon(f.contentType)}</span>
                              <span className="font-medium text-gray-800 truncate max-w-xs">{f.originalFileName}</span>
                            </div>
                          </td>
                          <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{fmt(f.size)}</td>
                          <td className="px-4 py-3 hidden md:table-cell">
                            <span className="bg-blue-100 text-blue-700 text-xs px-2 py-0.5 rounded-full">v{f.currentVersion}</span>
                          </td>
                          <td className="px-4 py-3 text-gray-400 text-xs hidden lg:table-cell">{fmtD(f.createdAt)}</td>
                          <td className="px-4 py-3">
                            <div className="flex gap-1 justify-end opacity-0 group-hover:opacity-100">
                              <button onClick={() => download(f)} className="p-1.5 hover:bg-blue-50 rounded text-sm" title="İndir">⬇️</button>
                              <button onClick={() => share(f.id)} className="p-1.5 hover:bg-green-50 rounded text-sm" title="Paylaş">🔗</button>
                              <button onClick={() => delFile(f.id)} className="p-1.5 hover:bg-red-50 rounded text-sm" title="Sil">🗑️</button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {showUp && <UploadModal folderId={cur} onClose={() => setShowUp(false)} onSuccess={load} />}
    </div>
  )
}
