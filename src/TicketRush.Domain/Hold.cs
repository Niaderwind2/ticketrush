namespace TicketRush.Domain;

/// <summary>
/// Une place réservée est bloquée pendant le paiement.
/// Passé ce délai, la réservation expire et la place est remise en vente.
/// </summary>
public static class Hold
{
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(10);

    public static bool IsExpired(DateTimeOffset heldAt, DateTimeOffset now) => now - heldAt > Duration;
}
