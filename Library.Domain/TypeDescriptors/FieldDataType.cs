namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Represents the basic data types supported in type descriptor field definitions.
/// </summary>
public enum FieldDataType
{
    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// An integer value.
    /// </summary>
    Int,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Bool,

    /// <summary>
    /// A date and time value.
    /// </summary>
    DateTime,

    /// <summary>
    /// A decimal value.
    /// </summary>
    Decimal
}
