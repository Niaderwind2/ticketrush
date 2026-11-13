using TicketRush.Domain;

namespace TicketRush.UnitTests;

public class HoldTests
{
    private static readonly DateTimeOffset HeldAt = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Une_place_bloquee_depuis_5_minutes_n_a_pas_expire()
    {
        Assert.False(Hold.IsExpired(HeldAt, HeldAt.AddMinutes(5)));
    }

    [Fact]
    public void Une_place_bloquee_depuis_11_minutes_a_expire()
    {
        Assert.True(Hold.IsExpired(HeldAt, HeldAt.AddMinutes(11)));
    }
}
