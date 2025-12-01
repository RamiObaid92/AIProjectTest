namespace Library.Domain.Resources;

/// <summary>
/// Represents a generic resource in the library system.
/// A resource can be any type of object (book, article, user profile, etc.)
/// with its type-specific data stored as JSON in the PayloadJson property.
/// </summary>
public class Resource
{
    /// <summary>
    /// Gets the unique identifier for this resource.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type key for this resource (e.g., "book", "article", "userProfile").
    /// Used to determine which Type Descriptor applies to this resource.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this resource was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this resource was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the optional identifier of the owner of this resource.
    /// </summary>
    public string? OwnerId { get; private set; }

    /// <summary>
    /// Gets the optional JSON string containing generic metadata (tags, labels, etc.).
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>
    /// Gets the JSON string containing the type-specific payload data.
    /// The structure of this JSON is validated against the Type Descriptor for the resource's Type.
    /// </summary>
    public string PayloadJson { get; private set; }

    /// <summary>
    /// Gets or sets the denormalized search text for full-text search.
    /// This field is computed by the Application layer from the payload.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource"/> class with all properties.
    /// </summary>
    /// <param name="id">The unique identifier for this resource.</param>
    /// <param name="type">The type key for this resource.</param>
    /// <param name="ownerId">The optional owner identifier.</param>
    /// <param name="metadataJson">The optional JSON string for metadata.</param>
    /// <param name="payloadJson">The JSON string containing the type-specific payload.</param>
    /// <param name="createdAtUtc">The UTC timestamp when this resource was created.</param>
    /// <param name="updatedAtUtc">The UTC timestamp when this resource was last updated.</param>
    public Resource(
        Guid id,
        string type,
        string? ownerId,
        string? metadataJson,
        string payloadJson,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        ValidateType(type);
        ValidatePayloadJson(payloadJson);

        Id = id;
        Type = type;
        OwnerId = ownerId;
        MetadataJson = metadataJson;
        PayloadJson = payloadJson;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Creates a new resource with a generated identifier and current timestamps.
    /// </summary>
    /// <param name="type">The type key for this resource.</param>
    /// <param name="ownerId">The optional owner identifier.</param>
    /// <param name="metadataJson">The optional JSON string for metadata.</param>
    /// <param name="payloadJson">The JSON string containing the type-specific payload.</param>
    /// <param name="utcNow">The current UTC timestamp to use for CreatedAtUtc and UpdatedAtUtc.</param>
    /// <returns>A new <see cref="Resource"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when type or payloadJson is null or empty.</exception>
    public static Resource CreateNew(
        string type,
        string? ownerId,
        string? metadataJson,
        string payloadJson,
        DateTime utcNow)
    {
        return new Resource(
            id: Guid.NewGuid(),
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            createdAtUtc: utcNow,
            updatedAtUtc: utcNow);
    }

    /// <summary>
    /// Updates the payload and metadata for this resource.
    /// </summary>
    /// <param name="payloadJson">The new JSON string containing the type-specific payload.</param>
    /// <param name="metadataJson">The new optional JSON string for metadata.</param>
    /// <param name="utcNow">The current UTC timestamp to use for UpdatedAtUtc.</param>
    /// <exception cref="ArgumentException">Thrown when payloadJson is null or empty.</exception>
    public void UpdatePayload(string payloadJson, string? metadataJson, DateTime utcNow)
    {
        ValidatePayloadJson(payloadJson);

        PayloadJson = payloadJson;
        MetadataJson = metadataJson;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty.", nameof(type));
        }
    }

    private static void ValidatePayloadJson(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("PayloadJson cannot be null or empty.", nameof(payloadJson));
        }
    }
}
