namespace TicketRush.Domain;

public enum ReservationStatus { Held, Paid, Cancelled }

public sealed record Seat(int Id, int EventId, string Label, SeatCategory Category);

public sealed record Reservation(
    Guid Id,
    int EventId,
    int SeatId,
    string CustomerId,
    Tariff Tariff,
    ReservationStatus Status,
    int PriceCents,
    DateTimeOffset HeldAt,
    string? IdempotencyKey);

public sealed record NewReservation(
    int EventId,
    int SeatId,
    string CustomerId,
    Tariff Tariff,
    int PriceCents,
    string? IdempotencyKey);

/// <summary>
/// Port d'accès aux données. Le service ne connaît que cette interface :
/// on peut lui donner un mock, un fake en mémoire ou la vraie base PostgreSQL.
/// </summary>
public interface IReservationRepository
{
    Task<Seat?> FindSeatAsync(int eventId, int seatId);
    Task<Reservation?> FindActiveBySeatAsync(int seatId);
    Task<Reservation?> FindByIdempotencyKeyAsync(string key);
    Task<Reservation?> FindByIdAsync(Guid id);
    Task<int> CountActiveForCustomerAsync(int eventId, string customerId);
    Task<Reservation> InsertAsync(NewReservation reservation);
    Task UpdateStatusAsync(Guid id, ReservationStatus status);
}

/// <summary>Port vers le prestataire de paiement externe.</summary>
public interface IPaymentGateway
{
    Task ChargeAsync(Guid reservationId, int amountCents, string customerId);
}
