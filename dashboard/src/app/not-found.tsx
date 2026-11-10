import Link from 'next/link';

export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-24 text-center">
      <p className="text-5xl font-bold text-cyan-400">404</p>
      <h1 className="text-lg font-semibold text-white">Página no encontrada</h1>
      <p className="text-sm text-slate-400">La ruta que buscás no existe en el panel de Argos.</p>
      <Link href="/" className="text-sm font-medium text-cyan-300 hover:text-cyan-200">
        Volver al Dashboard
      </Link>
    </div>
  );
}
