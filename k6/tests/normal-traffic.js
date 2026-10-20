// Escenario 1 — Tráfico normal (Sprint 6)
//
// Objetivo: línea base de operación saludable. Verifica que el flujo inteligente
// de Argos NO se dispara (ausencia de falsos positivos).
//
// Perfil: 10 VUs, ramp-up 1m -> steady 3m -> ramp-down 1m (~5m).
// Mix: 60% GET /api/usuarios (paginado), 40% GET /api/status.
// Assertions: p95 < 500ms, error rate < 1%.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://nginx-proxy';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '1m', target: 10 }, // ramp-up
    { duration: '3m', target: 10 }, // steady
    { duration: '1m', target: 0 },  // ramp-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
  },
};

export default function () {
  let res;
  if (Math.random() < 0.6) {
    // 60% — listado paginado de usuarios
    res = http.get(`${BASE_URL}/api/usuarios?pageNumber=1&pageSize=10`, {
      tags: { name: 'GET /api/usuarios' },
    });
  } else {
    // 40% — healthcheck de la API
    res = http.get(`${BASE_URL}/api/status`, {
      tags: { name: 'GET /api/status' },
    });
  }

  const ok = check(res, {
    'status is 200': (r) => r.status === 200,
    'body not empty': (r) => r.body && r.body.length > 0,
  });
  errorRate.add(!ok);

  // Ritmo humano: ~1 request cada 1-2s por VU
  sleep(Math.random() * 1 + 1);
}
