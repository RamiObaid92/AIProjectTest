using System.Text.Json;

namespace Library.Application.Resources;

/// <summary>
/// Represents a resource as returned to API clients.
/// This is the "view" of the resource over the wire.
/// </summary>
public class ResourceDto
{
    /// <summary>
    /// Gets or sets the unique identifier for this resource.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type key for this resource (e.g., "book", "article").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this resource was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this resource was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional owner identifier for this resource.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the optional metadata for this resource (e.g., tags, labels).
    /// </summary>
    public JsonElement? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the type-specific payload data for this resource.
    /// </summary>
    public JsonElement Payload { get; set; }
}
