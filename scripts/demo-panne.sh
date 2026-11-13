#!/usr/bin/env bash
# Démo d'ouverture du cours : la CI est verte, et pourtant...
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Démarrage de TicketRush (base, API, paiement)"
docker compose up -d --build --wait db payment api >/dev/null

echo "==> Remise en vente de toutes les places"
curl -fsS -X POST http://localhost:8080/admin/reset

echo "==> Ouverture des ventes : 300 fans, 200 places, 30 secondes"
docker compose --profile charge run --rm k6 || true

echo
echo "==> Ce que les tests n'ont pas vu :"
curl -fsS http://localhost:8080/admin/stats
echo
