using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicketRush.IntegrationTests;

/// <summary>
/// En production, l'API tourne sur plusieurs instances derrière un répartiteur de charge.
/// Une règle métier comme le quota doit tenir quelle que soit l'instance qui reçoit la requête.
/// </summary>
public class MultiInstanceTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private HttpClient NewInstance() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:TicketRush", pg.ConnectionString))
            .CreateClient();

    [Fact]
    public async Task Le_quota_de_6_billets_tient_quand_les_requetes_arrivent_sur_deux_instances()
    {
        var instanceA = NewInstance();
        var instanceB = NewInstance();

        // Le répartiteur de charge envoie les requêtes d'Alice tantôt sur A, tantôt sur B.
        for (var seat = 60; seat < 66; seat++)
        {
            var instance = seat % 2 == 0 ? instanceA : instanceB;
            var ok = await instance.PostAsJsonAsync($"/events/{pg.EventId}/reservations",
                new { seatId = seat, customerId = "alice-multi", tariff = "Plein" });
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        }

        var seventh = await instanceB.PostAsJsonAsync($"/events/{pg.EventId}/reservations",
            new { seatId = 66, customerId = "alice-multi", tariff = "Plein" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, seventh.StatusCode);
    }
}
