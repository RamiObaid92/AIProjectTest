namespace Library.WebApi.Errors;

/// <summary>
/// Represents a general API error response based on the ProblemDetails format.
/// Used for non-validation errors such as server errors.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Gets or sets a URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; set; } = "https://httpstatuses.com/500";

    /// <summary>
    /// Gets or sets a short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; set; } = "An error occurred.";

    /// <summary>
    /// Gets or sets the HTTP status code for this occurrence of the problem.
    /// </summary>
    public int Status { get; set; } = 500;

    /// <summary>
    /// Gets or sets a human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier for correlation.
    /// </summary>
    public string? TraceId { get; set; }
}
