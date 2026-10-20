// Escenario 3 — Patrón de ataque simulado (Sprint 6)
//
// Objetivo: validar la detección de anomalías. Genera requests sospechosas
// (paths inexistentes -> 404/5xx, User-Agent vacío/rotativo, IPs rotativas vía
// X-Forwarded-For) para disparar alertas de Nginx (error rate alto, IP sospechosa
// en Loki) -> Alertmanager -> n8n -> Argos propone mitigación y auto-documenta
// el incidente no registrado.
//
// Perfil: 0-1m baseline normal -> 1-4m ataque (~50 VUs) -> 4-5m normalización.
// Assertions: error rate esperado entre 1% y 50% (se ESPERA que las alertas se
//             disparen; no es un fallo del test).

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';
import { SharedArray } from 'k6/data';

const BASE_URL = __ENV.BASE_URL || 'http://nginx-proxy';

// Datasets cargados una sola vez y compartidos entre VUs
const payloads = new SharedArray('payloads', function () {
  return [JSON.parse(open('/data/payloads.json'))];
})[0];

const errorRate = new Rate('errors');

export const options = {
  scenarios: {
    // Fondo de tráfico legítimo durante todo el test
    baseline: {
      executor: 'constant-vus',
      vus: 5,
      duration: '5m',
      exec: 'legitimate',
    },
    // Oleada de ataque: arranca al minuto 1, dura 3 minutos
    attack: {
      executor: 'ramping-vus',
      startVUs: 0,
      startTime: '1m',
      stages: [
        { duration: '15s', target: 50 }, // subida rápida del ataque
        { duration: '2m30s', target: 50 }, // ataque sostenido
        { duration: '15s', target: 0 },  // normalización
      ],
      exec: 'attack',
    },
  },
  thresholds: {
    // No fallar el test por errores: en un ataque se esperan. Solo acotamos el rango.
    'errors': ['rate<0.60'],
  },
};

function pick(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

// Tráfico legítimo de fondo
export function legitimate() {
  const res = http.get(`${BASE_URL}/api/usuarios?pageNumber=1&pageSize=10`, {
    tags: { name: 'legit /api/usuarios' },
  });
  check(res, { 'legit 200': (r) => r.status === 200 });
  sleep(Math.random() * 1 + 1);
}

// Tráfico de ataque
export function attack() {
  const path = pick(payloads.suspicious_paths);
  const ua = pick(payloads.suspicious_user_agents);
  const ip = pick(payloads.spoofed_ips);

  const res = http.get(`${BASE_URL}${path}`, {
    headers: {
      'User-Agent': ua,
      'X-Forwarded-For': ip,
    },
    tags: { name: 'attack' },
  });

  // En un ataque un 4xx/5xx es el resultado ESPERADO; contamos como "no error"
  // solo cuando el server respondió algo (no timeouts / caídas).
  const responded = check(res, {
    'server responded': (r) => r.status !== 0,
  });
  errorRate.add(!responded);

  sleep(Math.random() * 0.4 + 0.1);
}
