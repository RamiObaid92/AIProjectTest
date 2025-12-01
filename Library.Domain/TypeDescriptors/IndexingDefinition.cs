namespace Library.Domain.TypeDescriptors;

/// <summary>
/// Defines which fields can be used for filtering, sorting, and full-text search.
/// </summary>
public class IndexingDefinition
{
    /// <summary>
    /// Gets the list of field names that can be used as filters.
    /// </summary>
    public IReadOnlyList<string> FilterableFields { get; init; } = [];

    /// <summary>
    /// Gets the list of field names that can be used for sorting.
    /// </summary>
    public IReadOnlyList<string> SortableFields { get; init; } = [];

    /// <summary>
    /// Gets the list of field names that support full-text search.
    /// </summary>
    public IReadOnlyList<string> FullTextFields { get; init; } = [];
}
