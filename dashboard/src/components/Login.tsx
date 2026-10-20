import { useState } from 'react';
import { Shield, Mail, Lock, Eye, EyeOff, ArrowRight, Activity, Cpu, GitBranch, Zap } from 'lucide-react';
import { useApp } from '@/types';

export function Login() {
  const { login } = useApp();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) return;
    setLoading(true);
    setError(null);
    try {
      await login(email, password);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Credenciales inválidas');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#0a0e14] grid-pattern flex items-center justify-center p-4 relative overflow-hidden">
      <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-cyan-500/10 rounded-full blur-[120px]" />
      <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-blue-500/8 rounded-full blur-[120px]" />

      <div className="absolute top-20 left-20 text-cyan-500/20 animate-pulse-dot">
        <Activity className="w-8 h-8" />
      </div>
      <div className="absolute bottom-32 left-32 text-blue-500/20 animate-pulse-dot" style={{ animationDelay: '0.5s' }}>
        <Cpu className="w-10 h-10" />
      </div>
      <div className="absolute top-40 right-24 text-cyan-500/20 animate-pulse-dot" style={{ animationDelay: '1s' }}>
        <GitBranch className="w-7 h-7" />
      </div>
      <div className="absolute bottom-20 right-32 text-blue-500/20 animate-pulse-dot" style={{ animationDelay: '1.5s' }}>
        <Zap className="w-9 h-9" />
      </div>

      <div className="relative z-10 w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <img
            src="/argos-logo.png"
            alt="Argos"
            className="w-20 h-20 mx-auto mb-4 rounded-2xl object-contain shadow-lg shadow-cyan-500/30"
          />
          <h1 className="text-3xl font-bold text-white tracking-tight">
            Argos <span className="text-cyan-400 glow-text">AIOps</span>
          </h1>
          <p className="text-slate-400 mt-2 text-sm">Plataforma inteligente de operaciones</p>
        </div>

        <div className="bg-slate-900/60 backdrop-blur-xl border border-slate-800 rounded-2xl p-8 shadow-2xl">
          <h2 className="text-lg font-semibold text-white mb-1">Iniciar Sesión</h2>
          <p className="text-sm text-slate-500 mb-6">Accedé a tu panel de operaciones</p>

          {error && (
            <div className="mb-4 px-4 py-3 rounded-lg bg-red-500/10 border border-red-500/20 text-sm text-red-400">
              {error}
            </div>
          )}

          <form onSubmit={(e) => { void handleSubmit(e); }} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Correo Electrónico</label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                  className="w-full bg-slate-950/50 border border-slate-800 rounded-lg pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/30 transition-all"
                  placeholder="tu@empresa.com"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Contraseña</label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                  className="w-full bg-slate-950/50 border border-slate-800 rounded-lg pl-10 pr-10 py-2.5 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/30 transition-all"
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 transition-colors"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading || !email || !password}
              className="w-full bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white font-medium py-2.5 rounded-lg transition-all flex items-center justify-center gap-2 shadow-lg shadow-cyan-500/20 disabled:opacity-60 group"
            >
              {loading ? (
                <span className="flex items-center gap-2">
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Conectando...
                </span>
              ) : (
                <>
                  Iniciar Sesión
                  <ArrowRight className="w-4 h-4 group-hover:translate-x-0.5 transition-transform" />
                </>
              )}
            </button>
          </form>

          <div className="flex items-center justify-center gap-2 mt-6 pt-6 border-t border-slate-800/60">
            <Shield className="w-3.5 h-3.5 text-cyan-500/70" />
            <p className="text-xs text-slate-500">Autenticación JWT · Argos RAG API</p>
          </div>
        </div>

        <p className="text-center text-xs text-slate-600 mt-6">
          © 2026 Argos AIOps · Powered by AI
        </p>
      </div>
    </div>
  );
}
