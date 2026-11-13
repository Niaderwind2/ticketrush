using Npgsql;
using Testcontainers.PostgreSql;
using TicketRush.Api.Infrastructure;

namespace TicketRush.IntegrationTests;

/// <summary>
/// Démarre un vrai PostgreSQL dans un conteneur Docker, une fois pour toute la classe de tests.
/// Il faut Docker sur le poste : c'est ce que vérifie le script check-env.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;
    public int EventId { get; private set; }
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
        await Database.MigrateAsync(DataSource);
        EventId = await Database.SeedDemoEventAsync(DataSource);
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}
