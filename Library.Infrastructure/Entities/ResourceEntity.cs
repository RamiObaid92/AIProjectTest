namespace Library.Infrastructure.Entities;

/// <summary>
/// Represents a generic resource entity that can store any type of object.
/// The actual type-specific data is stored as JSON in the PayloadJson property.
/// </summary>
public class ResourceEntity
{
    /// <summary>
    /// Unique identifier for the resource.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The type key for this resource (e.g., "book", "article", "userProfile").
    /// Used to determine which Type Descriptor applies to this resource.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The UTC timestamp when this resource was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The UTC timestamp when this resource was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Optional identifier of the owner of this resource.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Optional JSON string containing generic metadata (tags, labels, etc.).
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Required JSON string containing the type-specific payload data.
    /// The structure of this JSON is validated against the Type Descriptor for the resource's Type.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Optional denormalized search text for full-text search.
    /// Computed from the payload by the Application layer.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public ResourceEntity()
    {
    }

    /// <summary>
    /// Convenience constructor for creating a new resource entity.
    /// </summary>
    /// <param name="id">Unique identifier for the resource.</param>
    /// <param name="type">The type key for this resource.</param>
    /// <param name="payloadJson">JSON string containing the type-specific payload.</param>
    /// <param name="ownerId">Optional owner identifier.</param>
    /// <param name="metadataJson">Optional JSON string for metadata.</param>
    public ResourceEntity(Guid id, string type, string payloadJson, string? ownerId = null, string? metadataJson = null)
    {
        Id = id;
        Type = type;
        PayloadJson = payloadJson;
        OwnerId = ownerId;
        MetadataJson = metadataJson;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
