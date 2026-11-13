using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TicketRush.Api.Infrastructure;
using TicketRush.Domain;

namespace TicketRush.IntegrationTests;

/// <summary>
/// Lab 1 : la double vente. Vingt clients cliquent en même temps sur la même place.
/// Une seule réservation doit réussir, toutes les autres doivent être refusées.
///
/// Un bug de concurrence ne se montre pas à chaque exécution. Pour que le test soit fiable,
/// on libère tous les clients au même instant (une « barrière ») et on répète l'expérience sur 10 places.
/// </summary>
public class ConcurrencyTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private ReservationService CreateService() => new(
        new PgReservationRepository(pg.DataSource),
        Substitute.For<IPaymentGateway>(),
        new FakeTimeProvider(DateTimeOffset.UtcNow));

    /// <summary>Lance toutes les actions exactement en même temps et attend qu'elles se terminent.</summary>
    private static async Task<Task<T>[]> RunSimultaneously<T>(int count, Func<int, Task<T>> action)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(1, count)
            .Select(i => Task.Run(async () => { await start.Task; return await action(i); }))
            .ToArray();
        start.SetResult();
        await Task.WhenAll(tasks.Select(t => t.ContinueWith(_ => { })));
        return tasks;
    }

    [Theory]
    [InlineData(100)] [InlineData(101)] [InlineData(102)] [InlineData(103)] [InlineData(104)]
    [InlineData(105)] [InlineData(106)] [InlineData(107)] [InlineData(108)] [InlineData(109)]
    public async Task Vingt_clients_sur_la_meme_place_une_seule_reservation_reussit(int seatId)
    {
        var service = CreateService();

        var attempts = await RunSimultaneously(20, i =>
            service.ReserveAsync(new ReserveCommand(pg.EventId, seatId, $"client-{i}", Tariff.Plein)));

        Assert.Equal(1, attempts.Count(t => t.IsCompletedSuccessfully));
        Assert.Equal(19, attempts.Count(t => t.Exception?.InnerException is SeatAlreadyTakenException));
    }

    [Theory]
    [InlineData(110)] [InlineData(111)] [InlineData(112)] [InlineData(113)] [InlineData(114)]
    public async Task Un_double_clic_simultane_ne_cree_qu_une_reservation(int seatId)
    {
        var service = CreateService();
        var command = new ReserveCommand(pg.EventId, seatId, "alice", Tariff.Plein, $"double-clic-{seatId}");

        var clicks = await RunSimultaneously(5, _ => service.ReserveAsync(command));

        Assert.All(clicks, t => Assert.True(t.IsCompletedSuccessfully));
        Assert.Single(clicks.Select(t => t.Result.Id).Distinct());
    }
}
