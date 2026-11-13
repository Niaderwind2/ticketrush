using Npgsql;
using TicketRush.Domain;

namespace TicketRush.Api.Infrastructure;

/// <summary>Implémentation PostgreSQL du port <see cref="IReservationRepository"/>, en SQL direct.</summary>
public sealed class PgReservationRepository(NpgsqlDataSource db) : IReservationRepository
{
    private const string ActiveStatuses = "('Held', 'Paid')";

    public async Task<Seat?> FindSeatAsync(int eventId, int seatId)
    {
        await using var cmd = db.CreateCommand("SELECT id, event_id, label, category FROM seats WHERE id = $1 AND event_id = $2");
        cmd.Parameters.AddWithValue(seatId);
        cmd.Parameters.AddWithValue(eventId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Seat(r.GetInt32(0), r.GetInt32(1), r.GetString(2), Enum.Parse<SeatCategory>(r.GetString(3)));
    }

    public Task<Reservation?> FindActiveBySeatAsync(int seatId) =>
        QuerySingleAsync($"SELECT * FROM reservations WHERE seat_id = $1 AND status IN {ActiveStatuses} LIMIT 1", seatId);

    public Task<Reservation?> FindByIdempotencyKeyAsync(string key) =>
        QuerySingleAsync("SELECT * FROM reservations WHERE idempotency_key = $1 LIMIT 1", key);

    public Task<Reservation?> FindByIdAsync(Guid id) =>
        QuerySingleAsync("SELECT * FROM reservations WHERE id = $1", id);

    public async Task<int> CountActiveForCustomerAsync(int eventId, string customerId)
    {
        await using var cmd = db.CreateCommand(
            $"SELECT count(*)::int FROM reservations WHERE event_id = $1 AND customer_id = $2 AND status IN {ActiveStatuses}");
        cmd.Parameters.AddWithValue(eventId);
        cmd.Parameters.AddWithValue(customerId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<Reservation> InsertAsync(NewReservation n)
    {
        await using var cmd = db.CreateCommand("""
            INSERT INTO reservations (event_id, seat_id, customer_id, tariff, status, price_cents, idempotency_key)
            VALUES ($1, $2, $3, $4, 'Held', $5, $6)
            RETURNING *
            """);
        cmd.Parameters.AddWithValue(n.EventId);
        cmd.Parameters.AddWithValue(n.SeatId);
        cmd.Parameters.AddWithValue(n.CustomerId);
        cmd.Parameters.AddWithValue(n.Tariff.ToString());
        cmd.Parameters.AddWithValue(n.PriceCents);
        cmd.Parameters.AddWithValue((object?)n.IdempotencyKey ?? DBNull.Value);
        try
        {
            await using var r = await cmd.ExecuteReaderAsync();
            await r.ReadAsync();
            return Map(r);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Deux requêtes simultanées : la base a laissé passer la première et bloqué la seconde.
            // Si c'est un double clic, la première requête a déjà créé la réservation : on la renvoie.
            if (n.IdempotencyKey is not null
                && await FindByIdempotencyKeyAsync(n.IdempotencyKey) is { } sameRequest)
                return sameRequest;
            throw new SeatAlreadyTakenException(n.SeatId);
        }
    }

    public async Task UpdateStatusAsync(Guid id, ReservationStatus status)
    {
        await using var cmd = db.CreateCommand("UPDATE reservations SET status = $2 WHERE id = $1");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(status.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<Reservation?> QuerySingleAsync(string sql, object param)
    {
        await using var cmd = db.CreateCommand(sql);
        cmd.Parameters.AddWithValue(param);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    private static Reservation Map(NpgsqlDataReader r) => new(
        r.GetGuid(r.GetOrdinal("id")),
        r.GetInt32(r.GetOrdinal("event_id")),
        r.GetInt32(r.GetOrdinal("seat_id")),
        r.GetString(r.GetOrdinal("customer_id")),
        Enum.Parse<Tariff>(r.GetString(r.GetOrdinal("tariff"))),
        Enum.Parse<ReservationStatus>(r.GetString(r.GetOrdinal("status"))),
        r.GetInt32(r.GetOrdinal("price_cents")),
        r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("held_at")),
        r.IsDBNull(r.GetOrdinal("idempotency_key")) ? null : r.GetString(r.GetOrdinal("idempotency_key")));
}
