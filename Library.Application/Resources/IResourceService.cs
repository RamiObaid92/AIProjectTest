namespace Library.Application.Resources;

/// <summary>
/// Provides application-level operations for managing resources.
/// Orchestrates validation, persistence, and mapping between DTOs and domain models.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Creates a new resource with the specified data.
    /// </summary>
    /// <param name="dto">The data for the new resource.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The created resource as a DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto is null.</exception>
    /// <exception cref="ResourceValidationException">Thrown when payload validation fails.</exception>
    Task<ResourceDto> CreateResourceAsync(ResourceCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The resource as a DTO, or null if not found.</returns>
    Task<ResourceDto?> GetResourceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing resource with the specified data.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The updated resource as a DTO, or null if the resource was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto is null.</exception>
    /// <exception cref="ResourceValidationException">Thrown when payload validation fails.</exception>
    Task<ResourceDto?> UpdateResourceAsync(Guid id, ResourceUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to delete.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <remarks>
    /// If the resource does not exist, this operation completes without error.
    /// </remarks>
    Task DeleteResourceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries resources based on the specified criteria.
    /// </summary>
    /// <param name="query">The query parameters for filtering, sorting, and pagination.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A list of resources matching the query criteria.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    Task<IReadOnlyList<ResourceDto>> QueryResourcesAsync(ResourceQueryDto query, CancellationToken cancellationToken = default);
}
