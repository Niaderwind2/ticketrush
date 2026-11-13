// Faux prestataire de paiement, pour la démo et les tests.
// Refuse les montants supérieurs à 1 000 € pour simuler un refus bancaire.
var app = WebApplication.CreateBuilder(args).Build();

app.MapPost("/v1/charges", (ChargeRequest charge) =>
    charge.Amount > 100_000
        ? Results.Json(new { reason = "plafond de la carte dépassé" }, statusCode: 402)
        : Results.Ok(new { id = Guid.NewGuid(), status = "captured", charge.Reference }));

app.MapGet("/health", () => Results.Ok());

app.Run();

record ChargeRequest(int Amount, string Currency, string Reference, string Customer);
