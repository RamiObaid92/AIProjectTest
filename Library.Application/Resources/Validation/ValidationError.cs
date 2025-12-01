namespace Library.Application.Resources.Validation;

/// <summary>
/// Represents a single validation error encountered during resource payload validation.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets the name of the field that failed validation.
    /// May be empty for errors not specific to a single field.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the error code identifying the type of validation failure.
    /// Common codes: "Required", "TypeMismatch", "MaxLength", "Pattern", "UnknownType", "InvalidPayloadShape".
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets a human-readable message describing the validation error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorCode">The error code identifying the type of failure.</param>
    /// <param name="message">A human-readable message describing the error.</param>
    public ValidationError(string fieldName, string errorCode, string message)
    {
        FieldName = fieldName;
        ErrorCode = errorCode;
        Message = message;
    }

    /// <summary>
    /// Creates a validation error for a required field that is missing.
    /// </summary>
    /// <param name="fieldName">The name of the required field.</param>
    /// <returns>A ValidationError with code "Required".</returns>
    public static ValidationError Required(string fieldName)
    {
        return new ValidationError(fieldName, "Required", $"Field '{fieldName}' is required.");
    }

    /// <summary>
    /// Creates a validation error for a field with an incorrect data type.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="expectedType">The expected data type name.</param>
    /// <returns>A ValidationError with code "TypeMismatch".</returns>
    public static ValidationError TypeMismatch(string fieldName, string expectedType)
    {
        return new ValidationError(fieldName, "TypeMismatch", $"Field '{fieldName}' must be of type '{expectedType}'.");
    }

    /// <summary>
    /// Creates a validation error for a string field exceeding its maximum length.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <returns>A ValidationError with code "MaxLength".</returns>
    public static ValidationError MaxLengthExceeded(string fieldName, int maxLength)
    {
        return new ValidationError(fieldName, "MaxLength", $"Field '{fieldName}' exceeds maximum length of {maxLength}.");
    }

    /// <summary>
    /// Creates a validation error for a field that does not match the required pattern.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>A ValidationError with code "Pattern".</returns>
    public static ValidationError PatternMismatch(string fieldName)
    {
        return new ValidationError(fieldName, "Pattern", $"Field '{fieldName}' does not match the required pattern.");
    }

    /// <summary>
    /// Creates a validation error for an unknown resource type.
    /// </summary>
    /// <param name="typeKey">The unknown type key.</param>
    /// <returns>A ValidationError with code "UnknownType".</returns>
    public static ValidationError UnknownType(string typeKey)
    {
        return new ValidationError("type", "UnknownType", $"Unknown resource type '{typeKey}'.");
    }

    /// <summary>
    /// Creates a validation error for an invalid payload shape.
    /// </summary>
    /// <returns>A ValidationError with code "InvalidPayloadShape".</returns>
    public static ValidationError InvalidPayloadShape()
    {
        return new ValidationError(string.Empty, "InvalidPayloadShape", "Payload must be a JSON object.");
    }
}
