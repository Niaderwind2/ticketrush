using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TicketRush.Domain;

namespace TicketRush.UnitTests;

/// <summary>
/// Tests du service de réservation, avec des mocks (NSubstitute) à la place de la base et du paiement.
/// Ils passent tous. Question du lab 1 : qu'est-ce que ces tests ne peuvent PAS voir ?
/// </summary>
public class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly Seat SeatF1 = new(1, 1, "F-1", SeatCategory.Fosse);

    private readonly IReservationRepository _repo = Substitute.For<IReservationRepository>();
    private readonly IPaymentGateway _payments = Substitute.For<IPaymentGateway>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService(_repo, _payments, _clock);
        _repo.FindSeatAsync(1, 1).Returns(SeatF1);
        _repo.InsertAsync(Arg.Any<NewReservation>())
             .Returns(call => ToReservation(call.Arg<NewReservation>(), ReservationStatus.Held, Now));
    }

    [Fact]
    public async Task Reserve_une_place_libre()
    {
        var reservation = await _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein));

        Assert.Equal(ReservationStatus.Held, reservation.Status);
        Assert.Equal(6500, reservation.PriceCents);
        await _repo.Received(1).InsertAsync(Arg.Any<NewReservation>());
    }

    [Fact]
    public async Task Refuse_une_place_deja_reservee()
    {
        _repo.FindActiveBySeatAsync(1).Returns(Existing("bob", ReservationStatus.Held, Now.AddMinutes(-2)));

        await Assert.ThrowsAsync<SeatAlreadyTakenException>(
            () => _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein)));
        await _repo.DidNotReceive().InsertAsync(Arg.Any<NewReservation>());
    }

    [Fact]
    public async Task Remet_en_vente_une_place_dont_le_blocage_a_expire()
    {
        var expired = Existing("bob", ReservationStatus.Held, Now.AddMinutes(-15));
        _repo.FindActiveBySeatAsync(1).Returns(expired);

        await _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein));

        await _repo.Received(1).UpdateStatusAsync(expired.Id, ReservationStatus.Cancelled);
        await _repo.Received(1).InsertAsync(Arg.Any<NewReservation>());
    }

    [Fact]
    public async Task Un_double_clic_renvoie_la_meme_reservation()
    {
        var first = Existing("alice", ReservationStatus.Held, Now) with { IdempotencyKey = "clic-1" };
        _repo.FindByIdempotencyKeyAsync("clic-1").Returns(first);

        var second = await _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein, "clic-1"));

        Assert.Equal(first.Id, second.Id);
        await _repo.DidNotReceive().InsertAsync(Arg.Any<NewReservation>());
    }

    [Fact]
    public async Task Un_double_clic_arrive_pendant_la_creation_renvoie_la_reservation_du_premier_clic()
    {
        var first = Existing("alice", ReservationStatus.Held, Now) with { IdempotencyKey = "clic-2" };
        _repo.FindActiveBySeatAsync(1).Returns(first);

        var second = await _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein, "clic-2"));

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Refuse_au_dela_du_quota()
    {
        _repo.CountActiveForCustomerAsync(1, "alice").Returns(6);

        await Assert.ThrowsAsync<QuotaExceededException>(
            () => _service.ReserveAsync(new ReserveCommand(1, 1, "alice", Tariff.Plein)));
    }

    [Fact]
    public async Task Refuse_une_place_inconnue()
    {
        await Assert.ThrowsAsync<UnknownSeatException>(
            () => _service.ReserveAsync(new ReserveCommand(1, 999, "alice", Tariff.Plein)));
    }

    [Fact]
    public async Task Le_paiement_confirme_la_reservation()
    {
        var held = Existing("alice", ReservationStatus.Held, Now.AddMinutes(-3));
        _repo.FindByIdAsync(held.Id).Returns(held);

        var paid = await _service.PayAsync(held.Id);

        Assert.Equal(ReservationStatus.Paid, paid.Status);
        await _payments.Received(1).ChargeAsync(held.Id, held.PriceCents, "alice");
        await _repo.Received(1).UpdateStatusAsync(held.Id, ReservationStatus.Paid);
    }

    [Fact]
    public async Task Refuse_de_payer_une_reservation_expiree()
    {
        var expired = Existing("alice", ReservationStatus.Held, Now.AddMinutes(-30));
        _repo.FindByIdAsync(expired.Id).Returns(expired);

        await Assert.ThrowsAsync<HoldExpiredException>(() => _service.PayAsync(expired.Id));
        await _payments.DidNotReceiveWithAnyArgs().ChargeAsync(default, default, default!);
    }

    private static Reservation Existing(string customer, ReservationStatus status, DateTimeOffset heldAt) =>
        new(Guid.NewGuid(), 1, 1, customer, Tariff.Plein, status, 6500, heldAt, null);

    private static Reservation ToReservation(NewReservation n, ReservationStatus status, DateTimeOffset heldAt) =>
        new(Guid.NewGuid(), n.EventId, n.SeatId, n.CustomerId, n.Tariff, status, n.PriceCents, heldAt, n.IdempotencyKey);
}
