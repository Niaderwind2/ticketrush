// Ouverture des ventes : 300 fans se ruent sur 200 places pendant 30 secondes.
// Certains cliquent deux fois (même clé d'idempotence, envoyée deux fois en parallèle).
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EVENT_ID = 1;
const SEATS = 200;

export const options = {
  scenarios: {
    ouverture: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '5s', target: 300 },
        { duration: '25s', target: 300 },
      ],
    },
  },
  thresholds: {
    // Objectif de service : 95 % des réservations en moins de 500 ms.
    'http_req_duration{name:reservation}': ['p(95)<500'],
  },
};

// Un 409 (place déjà prise) est une réponse normale, pas une erreur.
http.setResponseCallback(http.expectedStatuses(201, 409));

export default function () {
  const seatId = 1 + Math.floor(Math.random() * SEATS);
  const customerId = `fan-${__VU}-${__ITER}`;
  const key = `${customerId}-${seatId}`;
  const request = {
    method: 'POST',
    url: `${BASE_URL}/events/${EVENT_ID}/reservations`,
    body: JSON.stringify({ seatId, customerId, tariff: 'Plein' }),
    params: {
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': key },
      tags: { name: 'reservation' },
    },
  };

  // Un fan sur cinq double-clique.
  const responses = Math.random() < 0.2 ? http.batch([request, request]) : [http.request(request.method, request.url, request.body, request.params)];
  for (const r of responses) {
    check(r, { 'réponse attendue (201 ou 409)': (res) => res.status === 201 || res.status === 409 });
  }
}
