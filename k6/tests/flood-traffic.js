// Escenario 4 — HTTP flood (DDoS L7) (Sprint 6)
//
// Objetivo: un volumen sostenido de requests GET (legítimos en su forma, no hay
// paths/headers maliciosos) por encima del límite de nginx (zona "api": 30r/s por
// IP, burst 50/100) para validar que el rate limit CORTA el flood antes de que
// llegue a la API/SQL Server, respondiendo 429 (no 503) y que la detección lo
// identifica: NginxSuspiciousIPActivity (>200 req/min por IP) y
// NginxErrorLogSpike ("limiting requests" en error.log).
//
// A diferencia de "attack-pattern" (paths inexistentes, UA vacío, IP spoofeada),
// aquí todo el tráfico pega al mismo endpoint real con headers normales: lo único
// que delata el flood es el VOLUMEN, no el contenido del request.
//
// Limitación conocida: todo el tráfico sale de un solo contenedor k6 → una sola
// IP de origen. Esto simula un DoS de una fuente, no un DDoS distribuido (que
// requeriría múltiples IPs y no puede probarse con un solo runner).
//
// Perfil: 0-30s 20 req/s (baseline, bajo el límite) -> 30s-2m30s 80 req/s
//         (flood, muy por encima del límite de 30r/s) -> 2m30s-3m 20 req/s
//         (normalización).
// Assertions: se ESPERA una alta tasa de 429 durante el flood; no es un fallo
//             del test. Solo falla si el servidor no responde (timeout/5xx).

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://nginx-proxy';

const serverErrors = new Rate('server_errors');
const throttled = new Counter('throttled_429');

export const options = {
  scenarios: {
    flood: {
      executor: 'ramping-arrival-rate',
      startRate: 20,
      timeUnit: '1s',
      preAllocatedVUs: 120,
      maxVUs: 200,
      stages: [
        { target: 20, duration: '30s' },  // baseline, bajo el límite (30r/s)
        { target: 80, duration: '2m' },   // flood sostenido, muy por encima del límite
        { target: 20, duration: '30s' },  // normalización
      ],
    },
  },
  thresholds: {
    // No fallar el test por 429 (es el resultado esperado del rate limit).
    // Solo nos importa que el backend no caiga con 5xx/timeouts.
    server_errors: ['rate<0.05'],
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/usuarios?pageNumber=1&pageSize=10`, {
    tags: { name: 'flood /api/usuarios' },
  });

  if (res.status === 429) throttled.add(1);

  const ok = check(res, {
    'no es 5xx / timeout': (r) => r.status === 0 ? false : r.status < 500,
  });
  serverErrors.add(!ok);

  sleep(0.05);
}
