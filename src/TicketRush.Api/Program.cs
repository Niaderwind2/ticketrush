using System.Text.Json.Serialization;
using Npgsql;
using TicketRush.Api.Infrastructure;
using TicketRush.Domain;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TicketRush")
    ?? "Host=localhost;Port=5432;Username=ticketrush;Password=ticketrush;Database=ticketrush";
var paymentUrl = builder.Configuration["Payment:BaseUrl"] ?? "http://localhost:5100";

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IReservationRepository, PgReservationRepository>();
builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>(c => c.BaseAddress = new Uri(paymentUrl));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReservationService>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

var db = app.Services.GetRequiredService<NpgsqlDataSource>();
await Database.MigrateAsync(db);
await Database.SeedDemoEventAsync(db);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/events/{eventId:int}/reservations", async (
    int eventId,
    ReserveRequest body,
    HttpRequest request,
    ReservationService service) =>
{
    var key = request.Headers["Idempotency-Key"].FirstOrDefault();
    try
    {
        var reservation = await service.ReserveAsync(
            new ReserveCommand(eventId, body.SeatId, body.CustomerId, body.Tariff, key));
        return Results.Created($"/reservations/{reservation.Id}", reservation);
    }
    catch (SeatAlreadyTakenException e) { return Results.Conflict(new { error = e.Message }); }
    catch (UnknownSeatException e) { return Results.NotFound(new { error = e.Message }); }
    catch (QuotaExceededException e) { return Results.UnprocessableEntity(new { error = e.Message }); }
    catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
});

app.MapPost("/reservations/{id:guid}/pay", async (Guid id, ReservationService service) =>
{
    try
    {
        return Results.Ok(await service.PayAsync(id));
    }
    catch (ReservationNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
    catch (HoldExpiredException e) { return Results.Conflict(new { error = e.Message }); }
    catch (PaymentRefusedException e) { return Results.Json(new { error = e.Message }, statusCode: 402); }
});

// Points d'entrée réservés à la démo en cours : à ne jamais exposer en production.
app.MapGet("/admin/stats", () => Database.StatsAsync(db));
app.MapPost("/admin/reset", async () => { await Database.ResetReservationsAsync(db); return Results.NoContent(); });

app.Run();

public sealed record ReserveRequest(int SeatId, string CustomerId, Tariff Tariff);

// Rend la classe Program visible pour les tests d'API (WebApplicationFactory).
public partial class Program;
