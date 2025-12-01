namespace Library.Domain.Resources;

/// <summary>
/// Defines the contract for resource persistence operations.
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Gets a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The resource if found; otherwise, null.</returns>
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries resources based on the specified criteria.
    /// </summary>
    /// <param name="criteria">The query criteria for filtering and paging.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of resources matching the criteria.</returns>
    Task<IReadOnlyList<Resource>> QueryAsync(ResourceQueryCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new resource to the repository.
    /// </summary>
    /// <param name="resource">The resource to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing resource in the repository.
    /// </summary>
    /// <param name="resource">The resource to update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task UpdateAsync(Resource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to delete.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
