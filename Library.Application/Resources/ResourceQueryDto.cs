namespace Library.Application.Resources;

/// <summary>
/// Represents query parameters for listing and filtering resources.
/// </summary>
public class ResourceQueryDto
{
    private const int DefaultPageSize = 50;

    /// <summary>
    /// Gets or sets the optional type filter. When set, only resources of this type are returned.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the optional owner ID filter. When set, only resources with this owner are returned.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum creation date filter (inclusive).
    /// When set, only resources created on or after this UTC date are returned.
    /// </summary>
    public DateTime? CreatedAfterUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum creation date filter (inclusive).
    /// When set, only resources created on or before this UTC date are returned.
    /// </summary>
    public DateTime? CreatedBeforeUtc { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination. Defaults to 50.
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Gets or sets the optional field name to sort by.
    /// Sorting is descriptor-driven; this value will be validated against the Type Descriptor.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort direction. Valid values are "asc" or "desc".
    /// Defaults to ascending if not specified.
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// Gets or sets the optional search text filter.
    /// When set, only resources whose SearchText contains this value are returned.
    /// </summary>
    public string? SearchText { get; set; }
}
