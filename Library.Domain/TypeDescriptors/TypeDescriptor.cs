namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Represents a type descriptor that defines the schema, validation rules,
/// indexing capabilities, access policies, and UI hints for a resource type.
/// </summary>
public class TypeDescriptor
{
    /// <summary>
    /// Gets the unique key identifying this type (e.g., "book", "article").
    /// </summary>
    public string TypeKey { get; init; }

    /// <summary>
    /// Gets the human-readable display name for this type.
    /// </summary>
    public string DisplayName { get; init; }

    /// <summary>
    /// Gets the schema version for this type descriptor.
    /// Used for versioning and migration purposes.
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// Gets the list of field definitions that make up the payload schema.
    /// </summary>
    public IReadOnlyList<FieldDefinition> Fields { get; init; }

    /// <summary>
    /// Gets the optional indexing definition specifying filterable, sortable, and full-text fields.
    /// </summary>
    public IndexingDefinition? Indexing { get; init; }

    /// <summary>
    /// Gets the optional policy definition specifying access control rules.
    /// </summary>
    public PolicyDefinition? Policy { get; init; }

    /// <summary>
    /// Gets the optional UI hints for rendering resources of this type.
    /// </summary>
    public UiHints? UiHints { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeDescriptor"/> class.
    /// </summary>
    /// <param name="typeKey">The unique key identifying this type.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="schemaVersion">The schema version number.</param>
    /// <param name="fields">The list of field definitions.</param>
    /// <param name="indexing">Optional indexing definition.</param>
    /// <param name="policy">Optional policy definition.</param>
    /// <param name="uiHints">Optional UI hints.</param>
    /// <exception cref="ArgumentException">Thrown when typeKey is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when fields is null.</exception>
    public TypeDescriptor(
        string typeKey,
        string displayName,
        int schemaVersion,
        IReadOnlyList<FieldDefinition> fields,
        IndexingDefinition? indexing = null,
        PolicyDefinition? policy = null,
        UiHints? uiHints = null)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            throw new ArgumentException("TypeKey cannot be null or empty.", nameof(typeKey));
        }

        TypeKey = typeKey;
        DisplayName = displayName ?? typeKey;
        SchemaVersion = schemaVersion;
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Indexing = indexing;
        Policy = policy;
        UiHints = uiHints;
    }

    /// <summary>
    /// Parameterless constructor for configuration binding.
    /// </summary>
    public TypeDescriptor()
    {
        TypeKey = string.Empty;
        DisplayName = string.Empty;
        SchemaVersion = 1;
        Fields = [];
    }
}
