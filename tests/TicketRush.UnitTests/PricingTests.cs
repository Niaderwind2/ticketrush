using TicketRush.Domain;

namespace TicketRush.UnitTests;

// Lab 2 : cette suite a été écrite par un collègue pressé (avec l'aide d'un assistant).
// Elle passe au vert et couvre 100 % des lignes de Pricing.cs.
// Question : si quelqu'un casse une règle de prix, est-ce qu'un de ces tests échoue ?
// Lancez Stryker pour le savoir (voir docs/lab-2.html).
public class PricingTests
{
    private static readonly DateTimeOffset Concert = new(2026, 12, 20, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LongtempsAvant = Concert.AddDays(-60);
    private static readonly DateTimeOffset PeuAvant = Concert.AddDays(-5);

    // --- Prix de base ---------------------------------------------------------

    [Fact]
    public void Le_prix_fosse_est_positif() =>
        Assert.True(Pricing.BasePriceCents(SeatCategory.Fosse) > 0);

    [Fact]
    public void Le_prix_cat1_est_positif() =>
        Assert.True(Pricing.BasePriceCents(SeatCategory.Cat1) > 0);

    [Fact]
    public void Le_prix_cat2_est_positif() =>
        Assert.True(Pricing.BasePriceCents(SeatCategory.Cat2) > 0);

    [Fact]
    public void Une_categorie_inconnue_est_refusee() =>
        Assert.ThrowsAny<Exception>(() => Pricing.BasePriceCents((SeatCategory)42));

    // --- Tarifs ---------------------------------------------------------------

    [Fact]
    public void Le_tarif_plein_donne_un_prix()
    {
        var prix = Pricing.TicketPriceCents(new OrderLine(SeatCategory.Cat1, Tariff.Plein));
        Assert.True(prix > 0);
    }

    [Fact]
    public void Le_tarif_reduit_coute_moins_cher_que_le_plein()
    {
        var plein = Pricing.TicketPriceCents(new OrderLine(SeatCategory.Cat1, Tariff.Plein));
        var reduit = Pricing.TicketPriceCents(new OrderLine(SeatCategory.Cat1, Tariff.Reduit));
        Assert.True(reduit <= plein);
    }

    [Fact]
    public void Le_tarif_enfant_donne_un_prix()
    {
        var prix = Pricing.TicketPriceCents(new OrderLine(SeatCategory.Cat2, Tariff.Enfant));
        Assert.True(prix > 0);
    }

    [Fact]
    public void Un_tarif_inconnu_est_refuse() =>
        Assert.ThrowsAny<Exception>(() => Pricing.TicketPriceCents(new OrderLine(SeatCategory.Cat1, (Tariff)42)));

    [Fact]
    public void Le_tarif_enfant_est_interdit_en_fosse() =>
        Assert.ThrowsAny<Exception>(() => Pricing.TicketPriceCents(new OrderLine(SeatCategory.Fosse, Tariff.Enfant)));

    // --- Early bird -----------------------------------------------------------

    [Fact]
    public void Un_achat_deux_mois_avant_est_early_bird() =>
        Assert.True(Pricing.IsEarlyBird(LongtempsAvant, Concert));

    [Fact]
    public void Un_achat_la_veille_n_est_pas_early_bird() =>
        Assert.False(Pricing.IsEarlyBird(Concert.AddDays(-1), Concert));

    // --- Frais de service -----------------------------------------------------

    [Fact]
    public void Un_billet_a_des_frais() =>
        Assert.True(Pricing.ServiceFeesCents(1) > 0);

    [Fact]
    public void Beaucoup_de_billets_ont_des_frais() =>
        Assert.True(Pricing.ServiceFeesCents(10) > 0);

    // --- Commande complète ----------------------------------------------------

    [Fact]
    public void Une_commande_simple_a_un_total_positif()
    {
        var total = Pricing.ComputeOrder([new OrderLine(SeatCategory.Fosse, Tariff.Plein)], PeuAvant, Concert);
        Assert.True(total.TotalCents > 0);
    }

    [Fact]
    public void Une_commande_early_bird_a_une_remise()
    {
        var total = Pricing.ComputeOrder([new OrderLine(SeatCategory.Cat1, Tariff.Plein)], LongtempsAvant, Concert);
        Assert.True(total.DiscountCents > 0);
    }

    [Fact]
    public void Une_commande_tardive_n_a_pas_de_remise()
    {
        var total = Pricing.ComputeOrder([new OrderLine(SeatCategory.Cat1, Tariff.Plein)], PeuAvant, Concert);
        Assert.Equal(0, total.DiscountCents);
    }

    [Fact]
    public void Une_commande_de_plusieurs_billets_coute_plus_cher_qu_un_seul()
    {
        var un = Pricing.ComputeOrder([new OrderLine(SeatCategory.Cat2, Tariff.Plein)], PeuAvant, Concert);
        var trois = Pricing.ComputeOrder(
            [new OrderLine(SeatCategory.Cat2, Tariff.Plein), new OrderLine(SeatCategory.Cat2, Tariff.Plein), new OrderLine(SeatCategory.Cat2, Tariff.Plein)],
            PeuAvant, Concert);
        Assert.True(trois.TotalCents > un.TotalCents);
    }

    [Fact]
    public void Le_total_contient_les_frais()
    {
        var total = Pricing.ComputeOrder([new OrderLine(SeatCategory.Fosse, Tariff.Plein)], PeuAvant, Concert);
        Assert.True(total.ServiceFeesCents > 0);
    }

    [Fact]
    public void Une_commande_vide_est_refusee() =>
        Assert.ThrowsAny<Exception>(() => Pricing.ComputeOrder([], PeuAvant, Concert));

    [Fact]
    public void Un_achat_apres_le_concert_est_refuse() =>
        Assert.ThrowsAny<Exception>(() =>
            Pricing.ComputeOrder([new OrderLine(SeatCategory.Fosse, Tariff.Plein)], Concert.AddDays(1), Concert));
}
