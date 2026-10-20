// Escenario 2 — Pico de tráfico (spike) (Sprint 6)
//
// Objetivo: un pico repentino de carga eleva la latencia / conexiones y dispara
// las alertas de latencia de Nginx (P95 upstream_response_time) -> Alertmanager
// -> n8n -> Argos propone escalado.
//
// Perfil: 0-1m 10 VUs (baseline) -> 1-2m 100 VUs (spike) -> 2-3m 10 VUs
//         (normalización) -> 3-5m 10 VUs (sostenido).
// Assertions relajadas: p95 < 2000ms, error rate < 5%.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://nginx-proxy';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '1m', target: 10 },  // baseline
    { duration: '1m', target: 100 }, // spike repentino
    { duration: '1m', target: 10 },  // normalización
    { duration: '2m', target: 10 },  // sostenido
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
    errors: ['rate<0.05'],
  },
};

export default function () {
  let res;
  if (Math.random() < 0.6) {
    res = http.get(`${BASE_URL}/api/usuarios?pageNumber=1&pageSize=10`, {
      tags: { name: 'GET /api/usuarios' },
    });
  } else {
    res = http.get(`${BASE_URL}/api/status`, {
      tags: { name: 'GET /api/status' },
    });
  }

  const ok = check(res, {
    'status is 200': (r) => r.status === 200,
  });
  errorRate.add(!ok);

  // Ritmo agresivo durante el pico para maximizar la carga
  sleep(Math.random() * 0.5 + 0.2);
}
