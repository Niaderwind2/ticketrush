namespace TicketRush.Domain;

/// <summary>Erreurs métier de TicketRush. L'API les traduit en codes HTTP.</summary>
public abstract class DomainException(string message) : Exception(message);

public sealed class SeatAlreadyTakenException(int seatId)
    : DomainException($"La place {seatId} est déjà réservée");

public sealed class QuotaExceededException(int max)
    : DomainException($"Quota dépassé : {max} billets maximum par personne et par concert");

public sealed class UnknownSeatException(int seatId)
    : DomainException($"La place {seatId} n'existe pas pour ce concert");

public sealed class ReservationNotFoundException(Guid id)
    : DomainException($"Réservation {id} introuvable");

public sealed class HoldExpiredException(Guid id)
    : DomainException($"La réservation {id} a expiré : la place a été remise en vente");

public sealed class PaymentRefusedException(string reason)
    : DomainException($"Paiement refusé : {reason}");
