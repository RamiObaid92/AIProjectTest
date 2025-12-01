using Library.Application.Resources.Validation;

namespace Library.Application.Resources;

/// <summary>
/// Exception thrown when resource payload validation fails.
/// </summary>
public class ResourceValidationException : Exception
{
    /// <summary>
    /// Gets the type key of the resource that failed validation.
    /// </summary>
    public string TypeKey { get; }

    /// <summary>
    /// Gets the collection of validation errors that caused the failure.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationException"/> class.
    /// </summary>
    /// <param name="typeKey">The type key of the resource that failed validation.</param>
    /// <param name="errors">The validation errors.</param>
    public ResourceValidationException(string typeKey, IEnumerable<ValidationError> errors)
        : base(BuildMessage(typeKey, errors))
    {
        TypeKey = typeKey;
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="typeKey">The type key of the resource that failed validation.</param>
    /// <param name="errors">The validation errors.</param>
    /// <param name="innerException">The inner exception.</param>
    public ResourceValidationException(string typeKey, IEnumerable<ValidationError> errors, Exception innerException)
        : base(BuildMessage(typeKey, errors), innerException)
    {
        TypeKey = typeKey;
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Builds a descriptive error message from the type key and validation errors.
    /// </summary>
    private static string BuildMessage(string typeKey, IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToList();
        var errorCount = errorList.Count;

        if (errorCount == 0)
        {
            return $"Validation failed for resource type '{typeKey}'.";
        }

        if (errorCount == 1)
        {
            var error = errorList[0];
            return $"Validation failed for resource type '{typeKey}': {error.Message}";
        }

        var errorSummary = string.Join("; ", errorList.Take(3).Select(e => e.Message));
        var suffix = errorCount > 3 ? $" (and {errorCount - 3} more errors)" : string.Empty;

        return $"Validation failed for resource type '{typeKey}' with {errorCount} errors: {errorSummary}{suffix}";
    }
}
