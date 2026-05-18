import { useState, useEffect } from "react"
import { Link } from "react-router-dom"
import Navbar from "../components/Navbar"
import api from "../api/axios"
const fmt=b=>b<1048576?(b/1024).toFixed(0)+"KB":b<1073741824?(b/1048576).toFixed(1)+"MB":(b/1073741824).toFixed(1)+"GB"
const rc=r=>({Admin:"bg-purple-100 text-purple-700",Manager:"bg-blue-100 text-blue-700",User:"bg-gray-100 text-gray-600"}[r]||"bg-gray-100")
export default function AdminPanel() {
  const [stats,setStats]=useState(null); const [users,setUsers]=useState([])
  const [search,setSearch]=useState(""); const [loading,setLoading]=useState(true)
  const [err,setErr]=useState(""); const [edit,setEdit]=useState(null)
  const [role,setRole]=useState(""); const [gb,setGb]=useState("")
  const load=async()=>{
    setLoading(true)
    try{const[s,u]=await Promise.all([api.get("/admin/stats"),api.get("/admin/users",{params:{search:search||undefined}})]);setStats(s.data);setUsers(u.data)}
    catch{setErr("Yüklenemedi.")} finally{setLoading(false)}
  }
  useEffect(()=>{load()},[search])
  const updRole=async id=>{try{await api.put(`/admin/users/${id}/role`,{role});setEdit(null);load()}catch(e){setErr(e.response?.data?.message||"Hata")}}
  const updQuota=async id=>{const b=parseFloat(gb)*1073741824;if(isNaN(b)){setErr("Geçersiz değer");return};try{await api.put(`/admin/users/${id}/quota`,{quotaBytes:Math.round(b)});setEdit(null);load()}catch(e){setErr(e.response?.data?.message||"Hata")}}
  const toggle=async id=>{try{await api.put(`/admin/users/${id}/toggle-active`);load()}catch{setErr("Hata")}}
  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar/>
      <div className="max-w-7xl mx-auto px-4 py-6">
        <div className="flex items-center justify-between mb-6">
          <div><h1 className="text-2xl font-bold text-gray-900">👑 Admin Paneli</h1><p className="text-gray-500 text-sm">Sistem yönetimi</p></div>
          <Link to="/" className="btn-secondary text-sm">← Dosyalarıma Dön</Link>
        </div>
        {err&&<div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">{err}<button onClick={()=>setErr("")} className="ml-2 font-bold">×</button></div>}
        {stats&&<div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          {[{l:"Toplam Kullanıcı",v:stats.totalUsers,i:"👥"},{l:"Toplam Dosya",v:stats.totalFiles,i:"📄"},{l:"Toplam Klasör",v:stats.totalFolders,i:"📁"},{l:"Kullanılan Alan",v:fmt(stats.totalStorageUsedBytes),i:"💾"}].map(s=>
            <div key={s.l} className="card"><p className="text-2xl mb-1">{s.i}</p><p className="text-2xl font-bold">{s.v}</p><p className="text-sm text-gray-500">{s.l}</p></div>)}
        </div>}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-gray-800">Kullanıcılar</h2>
            <input className="input-field w-56" placeholder="Ara..." value={search} onChange={e=>setSearch(e.target.value)}/>
          </div>
          {loading?<div className="flex justify-center py-10"><div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"/></div>
          :<table className="w-full text-sm">
            <thead><tr className="bg-gray-50 border-b">
              {["Kullanıcı","Rol","Depolama","Durum","İşlem"].map(h=><th key={h} className="text-left px-4 py-3 text-gray-500 font-medium">{h}</th>)}
            </tr></thead>
            <tbody className="divide-y divide-gray-50">
              {users.map(u=>{const p=Math.round(u.storageUsed/u.storageQuota*100); return(
                <tr key={u.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3"><p className="font-medium">{u.firstName} {u.lastName}</p><p className="text-xs text-gray-400">{u.email}</p></td>
                  <td className="px-4 py-3">{edit?.id===u.id&&edit.m==="role"
                    ?<div className="flex gap-1"><select className="border rounded px-1 py-1 text-xs" value={role} onChange={e=>setRole(e.target.value)}><option>User</option><option>Manager</option><option>Admin</option></select>
                      <button onClick={()=>updRole(u.id)} className="px-2 bg-blue-600 text-white rounded text-xs">✓</button>
                      <button onClick={()=>setEdit(null)} className="px-2 bg-gray-200 rounded text-xs">✕</button></div>
                    :<button onClick={()=>{setEdit({id:u.id,m:"role"});setRole(u.role)}}><span className={`px-2 py-1 rounded-full text-xs font-medium ${rc(u.role)}`}>{u.role}</span></button>}
                  </td>
                  <td className="px-4 py-3">{edit?.id===u.id&&edit.m==="quota"
                    ?<div className="flex gap-1 items-center"><input className="border rounded px-1 py-1 text-xs w-16" type="number" value={gb} onChange={e=>setGb(e.target.value)}/><span className="text-xs">GB</span>
                      <button onClick={()=>updQuota(u.id)} className="px-2 bg-blue-600 text-white rounded text-xs">✓</button>
                      <button onClick={()=>setEdit(null)} className="px-2 bg-gray-200 rounded text-xs">✕</button></div>
                    :<button className="text-left" onClick={()=>{setEdit({id:u.id,m:"quota"});setGb((u.storageQuota/1073741824).toFixed(1))}}>
                      <p className="text-xs text-gray-600">{fmt(u.storageUsed)} / {fmt(u.storageQuota)}</p>
                      <div className="w-24 bg-gray-200 rounded-full h-1.5 mt-1"><div className={`h-1.5 rounded-full ${p>80?"bg-red-500":"bg-blue-500"}`} style={{width:`${Math.min(p,100)}%`}}/></div>
                    </button>}
                  </td>
                  <td className="px-4 py-3"><span className={`px-2 py-1 rounded-full text-xs font-medium ${u.isActive?"bg-green-100 text-green-700":"bg-red-100 text-red-600"}`}>{u.isActive?"Aktif":"Pasif"}</span></td>
                  <td className="px-4 py-3"><button onClick={()=>toggle(u.id)} className={`text-xs px-3 py-1 rounded-lg font-medium ${u.isActive?"bg-red-50 text-red-600 hover:bg-red-100":"bg-green-50 text-green-600 hover:bg-green-100"}`}>{u.isActive?"Pasife Al":"Aktive Et"}</button></td>
                </tr>
              )})}
            </tbody>
          </table>}
        </div>
      </div>
    </div>
  )
}
