# Piste « boîte noire »

Pour celles et ceux qui ne veulent pas travailler en C#. L'application tourne dans Docker,
et on la teste uniquement par son API HTTP, dans le langage de son choix.

```bash
docker compose up -d --build --wait     # l'API répond sur http://localhost:8080
```

| Fichier | Langage | Ce qu'il prouve |
|---|---|---|
| `../k6/double-vente.js` | JavaScript (k6, rien à installer) | 20 clients simultanés sur une place : une seule réservation doit passer |
| `double_vente.py` | Python | La même chose, avec des threads |

Vous pouvez écrire la même chose en Java, en Go, en Bash avec `curl &`... Le principe compte plus que l'outil :
**lancer les requêtes au même instant, puis compter les réponses 201**.

API utile :

| Méthode | URL | Corps | Réponses |
|---|---|---|---|
| `POST` | `/events/1/reservations` (en-tête facultatif `Idempotency-Key`) | `{"seatId": 12, "customerId": "alice", "tariff": "Plein"}` | 201, 409 place prise, 422 quota, 404 place inconnue |
| `POST` | `/reservations/{id}/pay` | — | 200, 409 expirée, 402 refusé |
| `GET` | `/admin/stats` | — | compteurs de la démo |
| `POST` | `/admin/reset` | — | remet toutes les places en vente |
