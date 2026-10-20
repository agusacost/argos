import { useEffect, useState } from 'react';
import { Users, Plus, Trash2, Edit3, Shield, UserCircle, X, Check, AlertCircle } from 'lucide-react';
import { useApp } from '@/types';
import { listUsers, createUser, updateUser, deleteUser } from '@/api';
import type { UserRecord } from '@/api';

interface UserForm { email: string; name: string; role: string; password: string; }

export function UsersView() {
  const { user: me } = useApp();
  const [users, setUsers] = useState<UserRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<UserForm>({ email: '', name: '', role: 'Operador', password: '' });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    listUsers()
      .then(setUsers)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setForm({ email: '', name: '', role: 'Operador', password: '' });
    setFormError(null);
    setEditingId(null);
    setShowCreate(true);
  };

  const openEdit = (u: UserRecord) => {
    setForm({ email: u.email, name: u.name, role: u.role, password: '' });
    setFormError(null);
    setEditingId(u.id);
    setShowCreate(true);
  };

  const handleSave = async () => {
    setFormError(null);
    setSaving(true);
    try {
      if (editingId) {
        const patch: { name?: string; role?: string; password?: string } = { name: form.name, role: form.role };
        if (form.password) patch.password = form.password;
        await updateUser(editingId, patch);
      } else {
        if (!form.email || !form.password) { setFormError('Email y contraseña son obligatorios'); setSaving(false); return; }
        await createUser(form);
      }
      setShowCreate(false);
      load();
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar este usuario?')) return;
    try {
      await deleteUser(id);
      load();
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : 'Error al eliminar');
    }
  };

  const isAdmin = me?.role === 'Admin';

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-violet-500/10 flex items-center justify-center">
            <Users className="w-5 h-5 text-violet-400" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-white">Gestión de Usuarios</h2>
            <p className="text-xs text-slate-500">{users.length} usuarios · Solo Admin</p>
          </div>
        </div>
        {isAdmin && (
          <button
            onClick={openCreate}
            className="flex items-center gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium px-4 py-2.5 rounded-lg transition-all"
          >
            <Plus className="w-4 h-4" /> Nuevo Usuario
          </button>
        )}
      </div>

      {/* Modal */}
      {showCreate && (
        <>
          <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-40" onClick={() => setShowCreate(false)} />
          <div className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-md bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-2xl animate-fade-in">
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-sm font-semibold text-white">{editingId ? 'Editar Usuario' : 'Nuevo Usuario'}</h3>
              <button onClick={() => setShowCreate(false)} className="text-slate-500 hover:text-white">
                <X className="w-4 h-4" />
              </button>
            </div>
            {formError != null && (
              <div className="mb-4 px-3 py-2.5 rounded-lg bg-red-500/10 border border-red-500/20 text-xs text-red-400 flex items-center gap-2">
                <AlertCircle className="w-3.5 h-3.5 flex-shrink-0" /> {formError}
              </div>
            )}
            <div className="space-y-3">
              {!editingId && (
                <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">Email</label>
                  <input
                    type="email"
                    value={form.email}
                    onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                    className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50"
                    placeholder="usuario@empresa.com"
                  />
                </div>
              )}
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Nombre</label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50"
                  placeholder="Nombre Apellido"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Rol</label>
                <select
                  value={form.role}
                  onChange={(e) => setForm((f) => ({ ...f, role: e.target.value }))}
                  className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50"
                >
                  <option value="Admin">Admin</option>
                  <option value="Operador">Operador</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">
                  Contraseña{editingId ? ' (dejar vacío para no cambiar)' : ''}
                </label>
                <input
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                  className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50"
                  placeholder="••••••••"
                />
              </div>
            </div>
            <div className="flex gap-3 mt-5">
              <button
                onClick={() => setShowCreate(false)}
                className="flex-1 bg-slate-800 hover:bg-slate-700 text-slate-300 text-sm py-2.5 rounded-lg transition-all"
              >
                Cancelar
              </button>
              <button
                onClick={() => { void handleSave(); }}
                disabled={saving}
                className="flex-1 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium py-2.5 rounded-lg transition-all disabled:opacity-60 flex items-center justify-center gap-2"
              >
                {saving
                  ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  : <><Check className="w-4 h-4" /> Guardar</>}
              </button>
            </div>
          </div>
        </>
      )}

      {/* Table */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl overflow-hidden">
        <div className="px-5 py-2.5 grid grid-cols-12 gap-3 text-[10px] font-medium text-slate-600 uppercase tracking-wider border-b border-slate-800/50 bg-slate-950/30">
          <div className="col-span-1">Avatar</div>
          <div className="col-span-4">Usuario</div>
          <div className="col-span-3">Email</div>
          <div className="col-span-2">Rol</div>
          <div className="col-span-2 text-right">Acciones</div>
        </div>

        {loading && <div className="px-5 py-10 text-center text-sm text-slate-500">Cargando usuarios…</div>}
        {!loading && error != null && <div className="px-5 py-10 text-center text-sm text-red-400">Error: {error}</div>}

        {!loading && error == null && users.map((u) => (
          <div key={u.id} className="px-5 py-3.5 grid grid-cols-12 gap-3 items-center border-b border-slate-800/40 last:border-0">
            <div className="col-span-1">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-violet-400 to-purple-600 flex items-center justify-center text-white text-xs font-semibold">
                {u.name ? u.name.split(' ').map((n) => n[0]).join('').slice(0, 2) : <UserCircle className="w-4 h-4" />}
              </div>
            </div>
            <div className="col-span-4">
              <p className="text-sm font-medium text-white">{u.name || '—'}</p>
              {u.created_at && (
                <p className="text-[10px] text-slate-500">Desde {new Date(u.created_at).toLocaleDateString('es-AR')}</p>
              )}
            </div>
            <div className="col-span-3">
              <span className="text-xs text-slate-400 truncate">{u.email}</span>
            </div>
            <div className="col-span-2">
              <span className={`inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-md ${
                u.role === 'Admin'
                  ? 'bg-cyan-500/10 text-cyan-300 border border-cyan-500/20'
                  : 'bg-slate-800 text-slate-400'
              }`}>
                {u.role === 'Admin' && <Shield className="w-2.5 h-2.5" />}
                {u.role}
              </span>
            </div>
            <div className="col-span-2 flex items-center justify-end gap-2">
              {isAdmin && (
                <>
                  <button onClick={() => openEdit(u)} className="text-slate-500 hover:text-cyan-400 transition-colors p-1">
                    <Edit3 className="w-3.5 h-3.5" />
                  </button>
                  {u.id !== me?.id && (
                    <button onClick={() => { void handleDelete(u.id); }} className="text-slate-500 hover:text-red-400 transition-colors p-1">
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  )}
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
