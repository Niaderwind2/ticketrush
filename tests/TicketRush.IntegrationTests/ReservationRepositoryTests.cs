using TicketRush.Api.Infrastructure;
using TicketRush.Domain;

namespace TicketRush.IntegrationTests;

/// <summary>Le repository testé contre une vraie base PostgreSQL (Testcontainers).</summary>
public class ReservationRepositoryTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private readonly PgReservationRepository _repo = new(pg.DataSource);

    [Fact]
    public async Task Enregistre_puis_relit_une_reservation()
    {
        var created = await _repo.InsertAsync(new NewReservation(pg.EventId, 10, "alice", Tariff.Reduit, 5200, "cle-10"));

        var read = await _repo.FindByIdAsync(created.Id);

        Assert.NotNull(read);
        Assert.Equal(ReservationStatus.Held, read.Status);
        Assert.Equal("alice", read.CustomerId);
        Assert.Equal(created.Id, (await _repo.FindByIdempotencyKeyAsync("cle-10"))?.Id);
    }

    [Fact]
    public async Task Trouve_la_reservation_active_d_une_place()
    {
        var created = await _repo.InsertAsync(new NewReservation(pg.EventId, 11, "bob", Tariff.Plein, 6500, null));

        var active = await _repo.FindActiveBySeatAsync(11);

        Assert.Equal(created.Id, active?.Id);
    }

    [Fact]
    public async Task Une_reservation_annulee_n_est_plus_active()
    {
        var created = await _repo.InsertAsync(new NewReservation(pg.EventId, 12, "carol", Tariff.Plein, 6500, null));
        await _repo.UpdateStatusAsync(created.Id, ReservationStatus.Cancelled);

        Assert.Null(await _repo.FindActiveBySeatAsync(12));
    }
}
