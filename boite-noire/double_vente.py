"""Piste « boîte noire » du lab 1, en Python : prouver la double vente par l'API HTTP.

    pip install requests
    python boite-noire/double_vente.py

Attendu : exactement une réservation acceptée par place.
"""
import concurrent.futures
import threading

import requests

BASE_URL = "http://localhost:8080"
CLIENTS = 20


def reserve(seat_id: int, customer: str, start: threading.Barrier) -> int:
    start.wait()  # tout le monde part au même instant
    r = requests.post(
        f"{BASE_URL}/events/1/reservations",
        json={"seatId": seat_id, "customerId": customer, "tariff": "Plein"},
        timeout=10,
    )
    return r.status_code


def test_une_seule_reservation_par_place(seat_id: int) -> bool:
    start = threading.Barrier(CLIENTS)
    with concurrent.futures.ThreadPoolExecutor(CLIENTS) as pool:
        codes = list(pool.map(lambda i: reserve(seat_id, f"py-{seat_id}-{i}", start), range(CLIENTS)))
    created = codes.count(201)
    print(f"place {seat_id} : {created} réservation(s) acceptée(s) sur {CLIENTS} -> {'OK' if created == 1 else 'ÉCHEC'}")
    return created == 1


if __name__ == "__main__":
    results = [test_une_seule_reservation_par_place(seat) for seat in range(170, 180)]
    raise SystemExit(0 if all(results) else 1)
