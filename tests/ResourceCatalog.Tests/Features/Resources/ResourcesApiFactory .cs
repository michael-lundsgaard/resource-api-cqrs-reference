using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ResourceCatalog.Api.Data;
using Respawn;
using Testcontainers.PostgreSql;

namespace ResourceCatalog.Tests.Features.Resources
{
    public class ResourcesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string PostgresImage = "postgres:16-alpine";
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImage)
                .WithDatabase("testdb")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

        private Respawner _respawner = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseNpgsql(_postgres.GetConnectionString()));
            });
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            // Run migrations once, on startup
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            // Create the Respawner after the schema exists
            await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
            await conn.OpenAsync();

            _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                // Respawn will skip these tables when resetting
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        }

        public new async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task ResetDatabaseAsync()
        {
            await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
        }
    }
}