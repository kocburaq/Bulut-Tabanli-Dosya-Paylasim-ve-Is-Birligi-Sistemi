import { useState, useRef } from "react"
import api from "../api/axios"
export default function UploadModal({ folderId, onClose, onSuccess }) {
  const [file, setFile] = useState(null); const [comment, setComment] = useState("")
  const [loading, setLoading] = useState(false); const [progress, setProgress] = useState(0)
  const [err, setErr] = useState(""); const ref = useRef()
  const upload = async () => {
    if (!file) return; setLoading(true); setErr("")
    try {
      const fd = new FormData(); fd.append("file", file)
      if (folderId) fd.append("folderId", folderId)
      if (comment) fd.append("versionComment", comment)
      await api.post("/files/upload", fd, { headers:{"Content-Type":"multipart/form-data"}, onUploadProgress: e=>setProgress(Math.round(e.loaded*100/e.total)) })
      onSuccess(); onClose()
    } catch(e) { setErr(e.response?.data?.message||"Hata") } finally { setLoading(false) }
  }
  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex justify-between items-center mb-5">
          <h2 className="text-lg font-semibold">Dosya Yükle</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>
        <div className="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center cursor-pointer hover:border-blue-400 hover:bg-blue-50"
          onClick={()=>ref.current.click()} onDragOver={e=>e.preventDefault()} onDrop={e=>{e.preventDefault();setFile(e.dataTransfer.files[0])}}>
          <input ref={ref} type="file" className="hidden" onChange={e=>setFile(e.target.files[0])} />
          {file ? <div><p className="text-4xl mb-2">✅</p><p className="font-medium">{file.name}</p><p className="text-sm text-gray-500">{(file.size/1048576).toFixed(2)} MB</p></div>
            : <div><p className="text-4xl mb-2">☁️</p><p className="text-gray-600 font-medium">Sürükle veya tıkla</p><p className="text-sm text-gray-400">Maks. 100 MB</p></div>}
        </div>
        <input className="input-field mt-3" placeholder="Versiyon notu (opsiyonel)" value={comment} onChange={e=>setComment(e.target.value)} />
        {loading && <div className="mt-3"><div className="flex justify-between text-sm mb-1"><span>Yükleniyor...</span><span>{progress}%</span></div>
          <div className="w-full bg-gray-200 rounded-full h-2"><div className="bg-blue-500 h-2 rounded-full" style={{width:`${progress}%`}}/></div></div>}
        {err && <p className="text-red-500 text-sm mt-2">{err}</p>}
        <div className="flex gap-3 mt-4">
          <button onClick={onClose} className="btn-secondary flex-1">İptal</button>
          <button onClick={upload} disabled={!file||loading} className="btn-primary flex-1">{loading?"Yükleniyor...":"Yükle"}</button>
        </div>
      </div>
    </div>
  )
}
