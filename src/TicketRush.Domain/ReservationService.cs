namespace TicketRush.Domain;

public sealed record ReserveCommand(int EventId, int SeatId, string CustomerId, Tariff Tariff, string? IdempotencyKey = null);

public sealed class ReservationService(IReservationRepository repo, IPaymentGateway payments, TimeProvider clock)
{
    public async Task<Reservation> ReserveAsync(ReserveCommand cmd)
    {
        // Un double clic renvoie la même requête : on rend la réservation déjà créée.
        if (cmd.IdempotencyKey is not null)
        {
            var existing = await repo.FindByIdempotencyKeyAsync(cmd.IdempotencyKey);
            if (existing is not null) return existing;
        }

        var seat = await repo.FindSeatAsync(cmd.EventId, cmd.SeatId)
                   ?? throw new UnknownSeatException(cmd.SeatId);

        var alreadyHeld = await repo.CountActiveForCustomerAsync(cmd.EventId, cmd.CustomerId);
        Quota.Check(alreadyHeld, 1);

        var current = await repo.FindActiveBySeatAsync(cmd.SeatId);
        // Le premier clic d'un double clic a pu créer la réservation entre-temps.
        if (current is not null && cmd.IdempotencyKey is not null && current.IdempotencyKey == cmd.IdempotencyKey)
            return current;
        if (current is not null && !IsExpired(current))
            throw new SeatAlreadyTakenException(cmd.SeatId);
        if (current is not null)
            await repo.UpdateStatusAsync(current.Id, ReservationStatus.Cancelled);

        return await repo.InsertAsync(new NewReservation(
            cmd.EventId,
            cmd.SeatId,
            cmd.CustomerId,
            cmd.Tariff,
            Pricing.TicketPriceCents(new OrderLine(seat.Category, cmd.Tariff)),
            cmd.IdempotencyKey));
    }

    public async Task<Reservation> PayAsync(Guid reservationId)
    {
        var reservation = await repo.FindByIdAsync(reservationId)
                          ?? throw new ReservationNotFoundException(reservationId);
        if (reservation.Status == ReservationStatus.Paid) return reservation;
        if (reservation.Status == ReservationStatus.Cancelled || IsExpired(reservation))
            throw new HoldExpiredException(reservationId);

        await payments.ChargeAsync(reservationId, reservation.PriceCents, reservation.CustomerId);
        await repo.UpdateStatusAsync(reservationId, ReservationStatus.Paid);
        return reservation with { Status = ReservationStatus.Paid };
    }

    private bool IsExpired(Reservation r) =>
        r.Status == ReservationStatus.Held && Hold.IsExpired(r.HeldAt, clock.GetUtcNow());
}
