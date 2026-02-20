using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;
using ResourceCatalog.Api.Infrastructure;

namespace ResourceCatalog.Api
{
    /// <summary>
    /// Extension methods for configuring services in the DI container
    /// Keeps Program.cs clean and organized
    /// </summary>
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Configures PostgreSQL database with connection string from configuration
        /// </summary>
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }

        /// <summary>
        /// Configures MediatR with validation pipeline behavior
        /// </summary>
        public static IServiceCollection AddMediator(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<Program>();
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssemblyContaining<Program>();

            return services;
        }
    }
}