using TicketRush.Domain;

namespace TicketRush.UnitTests;

/// <summary>
/// Lab 2 (solution) : des tests qui vérifient les valeurs exactes et les frontières.
/// Ce sont les frontières (30 jours, plafond de 6 €, arrondis) qui tuent les mutants.
/// </summary>
public class PricingTests
{
    private static readonly DateTimeOffset Concert = new(2026, 12, 31, 20, 0, 0, TimeSpan.Zero);

    private static PriceBreakdown Order(DateTimeOffset purchase, params OrderLine[] lines) =>
        Pricing.ComputeOrder(lines, purchase, Concert);

    [Theory]
    [InlineData(SeatCategory.Fosse, Tariff.Plein, 6500)]
    [InlineData(SeatCategory.Cat1, Tariff.Plein, 8900)]
    [InlineData(SeatCategory.Cat2, Tariff.Plein, 4900)]
    [InlineData(SeatCategory.Fosse, Tariff.Reduit, 5200)]
    [InlineData(SeatCategory.Cat1, Tariff.Reduit, 7120)]
    [InlineData(SeatCategory.Cat1, Tariff.Enfant, 4450)]
    [InlineData(SeatCategory.Cat2, Tariff.Enfant, 2450)]
    public void Prix_d_un_billet_selon_categorie_et_tarif(SeatCategory category, Tariff tariff, int expectedCents)
    {
        Assert.Equal(expectedCents, Pricing.TicketPriceCents(new OrderLine(category, tariff)));
    }

    [Fact]
    public void Le_tarif_enfant_est_interdit_en_fosse()
    {
        Assert.Throws<ArgumentException>(() => Pricing.TicketPriceCents(new OrderLine(SeatCategory.Fosse, Tariff.Enfant)));
    }

    [Theory]
    [InlineData(1, 150)]
    [InlineData(3, 450)]
    [InlineData(4, 600)] // pile au plafond
    [InlineData(5, 600)] // plafonné
    public void Frais_de_service_plafonnes_a_6_euros(int tickets, int expectedCents)
    {
        Assert.Equal(expectedCents, Pricing.ServiceFeesCents(tickets));
    }

    [Theory]
    [InlineData(30, true)]   // frontière : pile 30 jours avant
    [InlineData(31, true)]
    [InlineData(29, false)]
    public void Early_bird_a_partir_de_30_jours_avant_le_concert(int daysBefore, bool expected)
    {
        Assert.Equal(expected, Pricing.IsEarlyBird(Concert.AddDays(-daysBefore), Concert));
    }

    [Fact]
    public void Early_bird_juste_avant_la_frontiere_ne_s_applique_pas()
    {
        Assert.False(Pricing.IsEarlyBird(Concert.AddDays(-30).AddMinutes(1), Concert));
    }

    [Fact]
    public void Commande_sans_remise_detail_exact()
    {
        var price = Order(Concert.AddDays(-10),
            new OrderLine(SeatCategory.Cat1, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Reduit));

        Assert.Equal(new PriceBreakdown(SubtotalCents: 12820, DiscountCents: 0, ServiceFeesCents: 300, TotalCents: 13120), price);
    }

    [Fact]
    public void Commande_early_bird_la_remise_ne_porte_pas_sur_les_frais()
    {
        var price = Order(Concert.AddDays(-45),
            new OrderLine(SeatCategory.Cat1, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Reduit));

        Assert.Equal(new PriceBreakdown(SubtotalCents: 12820, DiscountCents: 1282, ServiceFeesCents: 300, TotalCents: 11838), price);
    }

    [Fact]
    public void La_remise_early_bird_est_arrondie_au_centime_le_plus_proche()
    {
        // Sous-total choisi pour que 10 % tombe sur un demi-centime : 5 x 4900 + 2450 = 26950, remise 2695.
        var price = Order(Concert.AddDays(-40),
            new OrderLine(SeatCategory.Cat2, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Plein),
            new OrderLine(SeatCategory.Cat2, Tariff.Enfant));

        Assert.Equal(26950, price.SubtotalCents);
        Assert.Equal(2695, price.DiscountCents);
        Assert.Equal(600, price.ServiceFeesCents);
        Assert.Equal(26950 - 2695 + 600, price.TotalCents);
    }

    [Fact]
    public void Une_commande_vide_est_refusee()
    {
        Assert.Throws<ArgumentException>(() => Pricing.ComputeOrder([], Concert.AddDays(-1), Concert));
    }

    [Fact]
    public void Impossible_d_acheter_apres_le_concert()
    {
        Assert.Throws<ArgumentException>(() => Order(Concert.AddMinutes(1), new OrderLine(SeatCategory.Cat2, Tariff.Plein)));
    }

    [Fact]
    public void On_peut_acheter_jusqu_a_l_heure_du_concert()
    {
        var price = Order(Concert, new OrderLine(SeatCategory.Cat2, Tariff.Plein));
        Assert.Equal(4900 + 150, price.TotalCents);
    }
}
