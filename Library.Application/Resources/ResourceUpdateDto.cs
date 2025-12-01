using System.Text.Json;

namespace Library.Application.Resources;

/// <summary>
/// Represents the request payload for updating an existing resource.
/// </summary>
/// <remarks>
/// The resource Type is immutable and cannot be changed via update.
/// Only the Payload and Metadata can be modified.
/// </remarks>
public class ResourceUpdateDto
{
    /// <summary>
    /// Gets or sets the new type-specific payload data for this resource.
    /// This is required and must conform to the schema defined by the Type Descriptor.
    /// </summary>
    public JsonElement Payload { get; set; }

    /// <summary>
    /// Gets or sets the optional new metadata for this resource (e.g., tags, labels).
    /// Can be any valid JSON structure. Set to null to clear metadata.
    /// </summary>
    public JsonElement? Metadata { get; set; }
}
