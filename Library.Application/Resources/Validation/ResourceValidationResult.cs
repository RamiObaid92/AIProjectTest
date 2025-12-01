namespace Library.Application.Resources.Validation;

/// <summary>
/// Represents the result of validating a resource payload against a type descriptor schema.
/// </summary>
public class ResourceValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors encountered.
    /// Empty if validation succeeded.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The validation errors encountered.</param>
    public ResourceValidationResult(IEnumerable<ValidationError> errors)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A ResourceValidationResult with IsValid = true.</returns>
    public static ResourceValidationResult Success()
    {
        return new ResourceValidationResult([]);
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors that caused the failure.</param>
    /// <returns>A ResourceValidationResult with IsValid = false.</returns>
    public static ResourceValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        return new ResourceValidationResult(errors);
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The validation error that caused the failure.</param>
    /// <returns>A ResourceValidationResult with IsValid = false.</returns>
    public static ResourceValidationResult Failure(ValidationError error)
    {
        return new ResourceValidationResult([error]);
    }
}
