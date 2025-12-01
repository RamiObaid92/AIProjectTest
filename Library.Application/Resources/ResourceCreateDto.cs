using System.Text.Json;

namespace Library.Application.Resources;

/// <summary>
/// Represents the request payload for creating a new resource.
/// </summary>
public class ResourceCreateDto
{
    /// <summary>
    /// Gets or sets the type key for this resource (e.g., "book", "article").
    /// This is required and determines which Type Descriptor applies.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional owner identifier for this resource.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the optional metadata for this resource (e.g., tags, labels).
    /// Can be any valid JSON structure.
    /// </summary>
    public JsonElement? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the type-specific payload data for this resource.
    /// This is required and must conform to the schema defined by the Type Descriptor.
    /// </summary>
    public JsonElement Payload { get; set; }
}
