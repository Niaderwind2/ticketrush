namespace TicketRush.Domain;

/// <summary>Règle métier : 6 billets maximum par personne et par concert (lutte contre la revente).</summary>
public static class Quota
{
    public const int MaxTicketsPerCustomer = 6;

    public static void Check(int alreadyHeld, int requested)
    {
        if (requested <= 0)
            throw new ArgumentOutOfRangeException(nameof(requested), "Il faut demander au moins un billet");
        if (alreadyHeld + requested > MaxTicketsPerCustomer)
            throw new QuotaExceededException(MaxTicketsPerCustomer);
    }
}
