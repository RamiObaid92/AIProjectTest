namespace Library.WebApi.Errors;

/// <summary>
/// Represents a single field validation error in the API response.
/// </summary>
public class ValidationFieldError
{
    /// <summary>
    /// Gets or sets the name of the field that failed validation.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code identifying the type of validation failure.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable message describing the validation error.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
