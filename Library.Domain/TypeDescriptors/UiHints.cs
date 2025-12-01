namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Provides hints for UI rendering of a resource type.
/// </summary>
public class UiHints
{
    /// <summary>
    /// Gets the name of the field to use as the title/display name for a resource.
    /// </summary>
    public string? TitleField { get; init; }

    /// <summary>
    /// Gets the list of field names to display in list views.
    /// </summary>
    public IReadOnlyList<string> ListFields { get; init; } = [];
}
