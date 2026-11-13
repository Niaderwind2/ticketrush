-- Schéma TicketRush. Exécuté au démarrage de l'application et par les tests d'intégration.
CREATE TABLE IF NOT EXISTS events (
  id          SERIAL PRIMARY KEY,
  name        TEXT NOT NULL,
  starts_at   TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS seats (
  id          SERIAL PRIMARY KEY,
  event_id    INT NOT NULL REFERENCES events(id),
  label       TEXT NOT NULL,
  category    TEXT NOT NULL CHECK (category IN ('Fosse', 'Cat1', 'Cat2'))
);

CREATE TABLE IF NOT EXISTS reservations (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  event_id         INT NOT NULL REFERENCES events(id),
  seat_id          INT NOT NULL REFERENCES seats(id),
  customer_id      TEXT NOT NULL,
  tariff           TEXT NOT NULL,
  status           TEXT NOT NULL CHECK (status IN ('Held', 'Paid', 'Cancelled')),
  price_cents      INT NOT NULL,
  held_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  idempotency_key  TEXT
);
