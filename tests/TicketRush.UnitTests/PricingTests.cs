using TicketRush.Domain;

namespace TicketRush.UnitTests;

// Lab 2 : le module Pricing n'a qu'un seul test. Il passe, la couverture monte...
// mais que prouve-t-il vraiment ? Lancez Stryker pour le savoir (voir docs/lab-2.html).
public class PricingTests
{
    [Fact]
    public void Calcule_le_prix_d_un_billet_plein_tarif_en_fosse()
    {
        var total = Pricing.ComputeOrder(
            [new OrderLine(SeatCategory.Fosse, Tariff.Plein)],
            new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.True(total.TotalCents > 0);
    }
}
