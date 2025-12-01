namespace Library.Domain.Resources;

/// <summary>
/// Represents query criteria for filtering and paging resources.
/// </summary>
public class ResourceQueryCriteria
{
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
    /// Gets or sets the number of resources to skip for paging. Null means no skipping.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of resources to return. Null means no limit.
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// Gets or sets the optional search text filter.
    /// When set, only resources whose SearchText contains this value are returned.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceQueryCriteria"/> class.
    /// </summary>
    public ResourceQueryCriteria()
    {
    }
}
