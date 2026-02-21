using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResourceCatalog.Api.Data;
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

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }
    }
}