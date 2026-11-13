// Piste « boîte noire » du lab 1 : prouver la double vente sans lire une ligne de C#.
// 20 clients réservent la même place au même instant (http.batch envoie les requêtes en parallèle).
// Attendu : exactement 1 réponse 201, et 19 réponses 409.
//
//   docker compose up -d --build --wait
//   docker compose --profile charge run --rm k6 run /scripts/double-vente.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = { vus: 1, iterations: 10, thresholds: { checks: ['rate==1'] } };

http.setResponseCallback(http.expectedStatuses(201, 409));

export default function () {
  const seatId = 150 + __ITER; // une place différente à chaque essai
  const requests = Array.from({ length: 20 }, (_, i) => ({
    method: 'POST',
    url: `${BASE_URL}/events/1/reservations`,
    body: JSON.stringify({ seatId, customerId: `client-${__ITER}-${i}`, tariff: 'Plein' }),
    params: { headers: { 'Content-Type': 'application/json' } },
  }));

  const responses = http.batch(requests);
  const created = responses.filter((r) => r.status === 201).length;

  check(created, { [`place ${seatId} : une seule réservation acceptée`]: (n) => n === 1 });
}
