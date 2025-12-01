namespace Library.WebApi.Errors;

/// <summary>
/// Represents a validation error response returned when resource validation fails.
/// Based on the ProblemDetails format with additional validation-specific fields.
/// </summary>
public class ValidationErrorResponse
{
    /// <summary>
    /// Gets or sets a URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; set; } = "https://httpstatuses.com/400";

    /// <summary>
    /// Gets or sets a short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; set; } = "One or more validation errors occurred.";

    /// <summary>
    /// Gets or sets the HTTP status code for this occurrence of the problem.
    /// </summary>
    public int Status { get; set; } = 400;

    /// <summary>
    /// Gets or sets a human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier for correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the resource type key that failed validation.
    /// </summary>
    public string TypeKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of field-level validation errors.
    /// </summary>
    public IReadOnlyList<ValidationFieldError> Errors { get; set; } = [];
}
