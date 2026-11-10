// Una ruta de Next.js por vista. Los ids son los que usaba la SPA (Sidebar, Header, contexto).
export const VIEW_ROUTES = {
  dashboard: '/',
  incidents: '/incidentes',
  knowledge: '/conocimiento',
  metrics: '/metricas',
  loadtests: '/pruebas-carga',
  users: '/usuarios',
  settings: '/configuracion',
} as const;

export type ViewId = keyof typeof VIEW_ROUTES;

export function routeFor(view: string): string {
  return VIEW_ROUTES[view as ViewId] ?? '/';
}

export function viewFor(pathname: string | null): ViewId | null {
  const path = (pathname ?? '/').replace(/\.html$/, '').replace(/\/+$/, '') || '/';
  const hit = (Object.keys(VIEW_ROUTES) as ViewId[]).find((v) => VIEW_ROUTES[v] === path);
  return hit ?? null;
}
