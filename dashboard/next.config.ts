import type { NextConfig } from 'next';
import { PHASE_DEVELOPMENT_SERVER } from 'next/constants';

// Producción: export estático (out/) que sirve el nginx del contenedor, que además proxya /api → rag-api.
// Desarrollo: `next dev` reemplaza ese proxy con un rewrite hacia la API local (DEV_API_TARGET).
export default function config(phase: string): NextConfig {
  if (phase === PHASE_DEVELOPMENT_SERVER) {
    const target = process.env.DEV_API_TARGET || 'http://localhost:8000';
    return {
      async rewrites() {
        return [{ source: '/api/:path*', destination: `${target}/:path*` }];
      },
    };
  }
  return {
    output: 'export',
    images: { unoptimized: true },
  };
}
