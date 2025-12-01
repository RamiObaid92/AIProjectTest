using Library.Domain.Resources;
using Library.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// This method registers the following services:
    /// <list type="bullet">
    ///   <item><description><see cref="IResourceRepository"/> as <see cref="ResourceRepository"/> (scoped)</description></item>
    /// </list>
    /// Note: The <see cref="Persistence.LibraryDbContext"/> is expected to be registered separately
    /// (typically in the WebApi project's Program.cs).
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IResourceRepository, ResourceRepository>();

        return services;
    }
}
