using Library.Domain.Resources;
using Library.Infrastructure.Entities;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IResourceRepository"/>.
/// Provides persistence operations for resources using SQLite.
/// </summary>
public class ResourceRepository : IResourceRepository
{
    private readonly LibraryDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context to use for persistence operations.</param>
    public ResourceRepository(LibraryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Resource>> QueryAsync(ResourceQueryCriteria criteria, CancellationToken cancellationToken = default)
    {
        IQueryable<ResourceEntity> query = _dbContext.Resources.AsQueryable();

        // Apply Type filter
        if (!string.IsNullOrEmpty(criteria.Type))
        {
            query = query.Where(r => r.Type == criteria.Type);
        }

        // Apply OwnerId filter
        if (!string.IsNullOrEmpty(criteria.OwnerId))
        {
            query = query.Where(r => r.OwnerId == criteria.OwnerId);
        }

        // Apply CreatedAfterUtc filter
        if (criteria.CreatedAfterUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= criteria.CreatedAfterUtc.Value);
        }

        // Apply CreatedBeforeUtc filter
        if (criteria.CreatedBeforeUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= criteria.CreatedBeforeUtc.Value);
        }

        // Apply SearchText filter (case-insensitive contains search)
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var search = criteria.SearchText.Trim().ToLower();
            query = query.Where(r => r.SearchText != null && r.SearchText.ToLower().Contains(search));
        }

        // Apply ordering by CreatedAt ascending
        query = query.OrderBy(r => r.CreatedAt);

        // Apply paging
        if (criteria.Skip.HasValue)
        {
            query = query.Skip(criteria.Skip.Value);
        }

        if (criteria.Take.HasValue)
        {
            query = query.Take(criteria.Take.Value);
        }

        var entities = await query.ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(resource);

        await _dbContext.Resources.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the resource does not exist in the database, this method does nothing (no-op).
    /// Only mutable fields (Type, OwnerId, MetadataJson, PayloadJson, UpdatedAt) are updated.
    /// CreatedAt is preserved from the original entity.
    /// </remarks>
    public async Task UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == resource.Id, cancellationToken);

        if (entity is null)
        {
            // Resource not found - no-op
            return;
        }

        // Update mutable fields only (preserve CreatedAt)
        entity.Type = resource.Type;
        entity.OwnerId = resource.OwnerId;
        entity.MetadataJson = resource.MetadataJson;
        entity.PayloadJson = resource.PayloadJson;
        entity.UpdatedAt = resource.UpdatedAtUtc;
        entity.SearchText = resource.SearchText;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the resource does not exist in the database, this method does nothing (no-op).
    /// </remarks>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            // Resource not found - no-op
            return;
        }

        _dbContext.Resources.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Maps a <see cref="ResourceEntity"/> to a <see cref="Resource"/> domain model.
    /// </summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped domain model.</returns>
    private static Resource ToDomain(ResourceEntity entity)
    {
        var resource = new Resource(
            id: entity.Id,
            type: entity.Type,
            ownerId: entity.OwnerId,
            metadataJson: entity.MetadataJson,
            payloadJson: entity.PayloadJson,
            createdAtUtc: entity.CreatedAt,
            updatedAtUtc: entity.UpdatedAt);

        resource.SearchText = entity.SearchText;

        return resource;
    }

    /// <summary>
    /// Maps a <see cref="Resource"/> domain model to a <see cref="ResourceEntity"/>.
    /// </summary>
    /// <param name="resource">The domain model to map.</param>
    /// <returns>The mapped entity.</returns>
    private static ResourceEntity ToEntity(Resource resource)
    {
        return new ResourceEntity
        {
            Id = resource.Id,
            Type = resource.Type,
            OwnerId = resource.OwnerId,
            MetadataJson = resource.MetadataJson,
            PayloadJson = resource.PayloadJson,
            CreatedAt = resource.CreatedAtUtc,
            UpdatedAt = resource.UpdatedAtUtc,
            SearchText = resource.SearchText
        };
    }
}
