using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicketRush.IntegrationTests;

/// <summary>Tests de l'API HTTP, branchée sur le PostgreSQL du conteneur.</summary>
public class ReservationApiTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private HttpClient CreateClient() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:TicketRush", pg.ConnectionString))
            .CreateClient();

    [Fact]
    public async Task Reserver_une_place_libre_renvoie_201()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync($"/events/{pg.EventId}/reservations",
            new { seatId = 20, customerId = "alice", tariff = "Plein" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Reserver_une_place_deja_prise_renvoie_409()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync($"/events/{pg.EventId}/reservations",
            new { seatId = 21, customerId = "alice", tariff = "Plein" });

        var response = await client.PostAsJsonAsync($"/events/{pg.EventId}/reservations",
            new { seatId = 21, customerId = "bob", tariff = "Plein" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
