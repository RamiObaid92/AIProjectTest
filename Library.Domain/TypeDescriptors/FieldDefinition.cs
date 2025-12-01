namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Describes a single field in the payload schema of a type descriptor.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the data type of the field.
    /// </summary>
    public FieldDataType DataType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the field is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets the optional maximum length for string fields.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the optional regex pattern for validation.
    /// </summary>
    public string? Pattern { get; init; }
}
