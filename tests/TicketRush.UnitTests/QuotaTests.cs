using TicketRush.Domain;

namespace TicketRush.UnitTests;

public class QuotaTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 1)]
    [InlineData(0, 6)]
    public void Accepte_une_demande_dans_le_quota(int alreadyHeld, int requested)
    {
        var exception = Record.Exception(() => Quota.Check(alreadyHeld, requested));
        Assert.Null(exception);
    }

    [Fact]
    public void Refuse_le_septieme_billet()
    {
        Assert.Throws<QuotaExceededException>(() => Quota.Check(6, 1));
    }

    [Fact]
    public void Refuse_une_demande_vide()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Quota.Check(0, 0));
    }
}
