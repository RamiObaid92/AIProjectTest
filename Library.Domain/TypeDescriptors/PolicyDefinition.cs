namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Defines access control policies for a resource type.
/// Specifies which roles are allowed to perform CRUD operations.
/// </summary>
public class PolicyDefinition
{
    /// <summary>
    /// Gets the list of roles allowed to create resources of this type.
    /// </summary>
    public IReadOnlyList<string> AllowedCreateRoles { get; init; } = [];

    /// <summary>
    /// Gets the list of roles allowed to read resources of this type.
    /// </summary>
    public IReadOnlyList<string> AllowedReadRoles { get; init; } = [];

    /// <summary>
    /// Gets the list of roles allowed to update resources of this type.
    /// </summary>
    public IReadOnlyList<string> AllowedUpdateRoles { get; init; } = [];

    /// <summary>
    /// Gets the list of roles allowed to delete resources of this type.
    /// </summary>
    public IReadOnlyList<string> AllowedDeleteRoles { get; init; } = [];
}
