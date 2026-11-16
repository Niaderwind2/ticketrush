namespace TicketRush.Domain;

public enum SeatCategory { Fosse, Cat1, Cat2 }

public enum Tariff { Plein, Reduit, Enfant }

public sealed record OrderLine(SeatCategory Category, Tariff Tariff);

public sealed record PriceBreakdown(int SubtotalCents, int DiscountCents, int ServiceFeesCents, int TotalCents);

/// <summary>
/// Calcul du prix d'une commande. Tous les montants sont en centimes d'euro.
///
/// Règles métier :
/// - prix de base selon la catégorie de la place ;
/// - tarif réduit (étudiant, demandeur d'emploi) : -20 % ;
/// - tarif enfant (moins de 12 ans) : -50 %, interdit en fosse ;
/// - frais de service : 1,50 € par billet, plafonnés à 6 € par commande ;
/// - « early bird » : -10 % sur le sous-total si l'achat a lieu au moins 30 jours avant le concert ;
/// - les frais de service ne sont jamais remisés.
/// </summary>
public static class Pricing
{
    public const int ServiceFeePerTicketCents = 150;
    public const int ServiceFeeCapCents = 600;
    public const int EarlyBirdDays = 30;
    public const decimal EarlyBirdRate = 0.10m;

    public static int BasePriceCents(SeatCategory category) => category switch
    {
        SeatCategory.Fosse => 6500,
        SeatCategory.Cat1 => 8900,
        SeatCategory.Cat2 => 4900,
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    private static decimal TariffRate(Tariff tariff) => tariff switch
    {
        Tariff.Plein => 1.0m,
        Tariff.Reduit => 0.8m,
        Tariff.Enfant => 0.5m,
        _ => throw new ArgumentOutOfRangeException(nameof(tariff)),
    };

    public static int TicketPriceCents(OrderLine line)
    {
        if (line.Tariff == Tariff.Enfant && line.Category == SeatCategory.Fosse)
            throw new ArgumentException("Le tarif enfant est interdit en fosse");
        return (int)Math.Round(BasePriceCents(line.Category) * TariffRate(line.Tariff), MidpointRounding.AwayFromZero);
    }

    public static bool IsEarlyBird(DateTimeOffset purchase, DateTimeOffset eventDate) =>
        (eventDate - purchase).TotalDays >= EarlyBirdDays;

    public static int ServiceFeesCents(int ticketCount) =>
        Math.Min(ticketCount * ServiceFeePerTicketCents, ServiceFeeCapCents);

    public static PriceBreakdown ComputeOrder(IReadOnlyList<OrderLine> lines, DateTimeOffset purchase, DateTimeOffset eventDate)
    {
        if (lines.Count == 0)
            throw new ArgumentException("Une commande contient au moins un billet");
        if (purchase > eventDate)
            throw new ArgumentException("Impossible d'acheter un billet après le concert");

        var subtotal = lines.Sum(TicketPriceCents);
        var discount = IsEarlyBird(purchase, eventDate)
            ? (int)Math.Round(subtotal * EarlyBirdRate, MidpointRounding.AwayFromZero)
            : 0;
        var fees = ServiceFeesCents(lines.Count);
        return new PriceBreakdown(subtotal, discount, fees, subtotal - discount + fees);
    }
}
