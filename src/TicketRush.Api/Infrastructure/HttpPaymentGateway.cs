using System.Net.Http.Json;
using TicketRush.Domain;

namespace TicketRush.Api.Infrastructure;

/// <summary>Client HTTP du prestataire de paiement externe.</summary>
public sealed class HttpPaymentGateway(HttpClient http) : IPaymentGateway
{
    public async Task ChargeAsync(Guid reservationId, int amountCents, string customerId)
    {
        var response = await http.PostAsJsonAsync("/v1/charges", new
        {
            amount = amountCents,
            currency = "EUR",
            reference = reservationId,
            customer = customerId,
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<RefusalBody>().ConfigureAwait(false);
            throw new PaymentRefusedException(body?.Reason ?? $"HTTP {(int)response.StatusCode}");
        }
    }

    private sealed record RefusalBody(string? Reason);
}
