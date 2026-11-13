using Npgsql;

namespace TicketRush.Api.Infrastructure;

public static class Database
{
    public static async Task MigrateAsync(NpgsqlDataSource db)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "schema.sql"));
        await using var cmd = db.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Crée un concert de démonstration avec <paramref name="seatCount"/> places, si la base est vide.</summary>
    public static async Task<int> SeedDemoEventAsync(NpgsqlDataSource db, int seatCount = 200)
    {
        await using (var existing = db.CreateCommand("SELECT id FROM events ORDER BY id LIMIT 1"))
        {
            if (await existing.ExecuteScalarAsync() is int id) return id;
        }

        await using var insertEvent = db.CreateCommand(
            "INSERT INTO events (name, starts_at) VALUES ($1, $2) RETURNING id");
        insertEvent.Parameters.AddWithValue("Les Rouennais Volants - Zénith de Rouen");
        insertEvent.Parameters.AddWithValue(DateTimeOffset.UtcNow.AddDays(45));
        var eventId = (int)(await insertEvent.ExecuteScalarAsync())!;

        await using var insertSeats = db.CreateCommand("""
            INSERT INTO seats (event_id, label, category)
            SELECT $1,
                   CASE WHEN g <= $2 / 2 THEN 'F-' || g ELSE 'T-' || g END,
                   CASE WHEN g <= $2 / 2 THEN 'Fosse' WHEN g <= $2 * 3 / 4 THEN 'Cat1' ELSE 'Cat2' END
            FROM generate_series(1, $2) AS g
            """);
        insertSeats.Parameters.AddWithValue(eventId);
        insertSeats.Parameters.AddWithValue(seatCount);
        await insertSeats.ExecuteNonQueryAsync();
        return eventId;
    }

    /// <summary>Remet toutes les places en vente (utilisé par la démo).</summary>
    public static async Task ResetReservationsAsync(NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand("TRUNCATE reservations");
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Indicateurs de la démo : ce que la CI verte n'a pas vu.</summary>
    public static async Task<object> StatsAsync(NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand("""
            SELECT
              (SELECT count(*) FROM reservations WHERE status IN ('Held','Paid'))::int AS actives,
              (SELECT count(*) FROM (SELECT seat_id FROM reservations WHERE status IN ('Held','Paid')
                                     GROUP BY seat_id HAVING count(*) > 1) s)::int AS places_vendues_plusieurs_fois,
              (SELECT coalesce(sum(n - 1), 0) FROM (SELECT count(*) AS n FROM reservations
                                     WHERE idempotency_key IS NOT NULL
                                     GROUP BY idempotency_key HAVING count(*) > 1) d)::int AS reservations_en_double
            """);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new
        {
            reservationsActives = reader.GetInt32(0),
            placesVenduesPlusieursFois = reader.GetInt32(1),
            reservationsEnDoubleParDoubleClic = reader.GetInt32(2),
        };
    }
}
