using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Library.Application.TypeDescriptors;
using Library.Domain.TypeDescriptors;

namespace Library.Application.Resources.Validation;

/// <summary>
/// Implementation of <see cref="IResourceValidationService"/> that validates
/// resource payloads against type descriptor schemas loaded from configuration.
/// </summary>
public class ResourceValidationService : IResourceValidationService
{
    private readonly ITypeDescriptorRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationService"/> class.
    /// </summary>
    /// <param name="registry">The type descriptor registry to use for looking up schemas.</param>
    /// <exception cref="ArgumentNullException">Thrown when registry is null.</exception>
    public ResourceValidationService(ITypeDescriptorRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public ResourceValidationResult Validate(string typeKey, JsonElement payload)
    {
        // Step 1: Get the type descriptor
        var descriptor = _registry.GetDescriptorOrDefault(typeKey);
        if (descriptor is null)
        {
            return ResourceValidationResult.Failure(ValidationError.UnknownType(typeKey));
        }

        // Step 2: Validate payload is a JSON object
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return ResourceValidationResult.Failure(ValidationError.InvalidPayloadShape());
        }

        // Step 3: Validate each field
        var errors = new List<ValidationError>();

        foreach (var field in descriptor.Fields)
        {
            ValidateField(field, payload, errors);
        }

        // TODO: Extra-field handling could be added here.
        // Currently, extra fields in the payload that are not in the descriptor are ignored.

        // Step 4: Build the result
        return errors.Count == 0
            ? ResourceValidationResult.Success()
            : ResourceValidationResult.Failure(errors);
    }

    /// <summary>
    /// Validates a single field against the payload.
    /// </summary>
    private static void ValidateField(FieldDefinition field, JsonElement payload, List<ValidationError> errors)
    {
        var hasProperty = payload.TryGetProperty(field.Name, out JsonElement value);

        // Check required constraint
        if (!hasProperty)
        {
            if (field.IsRequired)
            {
                errors.Add(ValidationError.Required(field.Name));
            }
            // If not required and not present, nothing to validate
            return;
        }

        // Check for null value
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (field.IsRequired)
            {
                errors.Add(ValidationError.Required(field.Name));
            }
            // Null is allowed for optional fields
            return;
        }

        // Validate data type
        if (!ValidateDataType(field, value, errors))
        {
            // If type validation failed, skip further constraints
            return;
        }

        // Validate string constraints (MaxLength and Pattern)
        if (field.DataType == FieldDataType.String)
        {
            ValidateStringConstraints(field, value, errors);
        }
    }

    /// <summary>
    /// Validates that the JSON value matches the expected data type.
    /// </summary>
    /// <returns>True if the type is valid; false otherwise.</returns>
    private static bool ValidateDataType(FieldDefinition field, JsonElement value, List<ValidationError> errors)
    {
        switch (field.DataType)
        {
            case FieldDataType.String:
                if (value.ValueKind != JsonValueKind.String)
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "String"));
                    return false;
                }
                break;

            case FieldDataType.Int:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out _))
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "Int"));
                    return false;
                }
                break;

            case FieldDataType.Bool:
                if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "Bool"));
                    return false;
                }
                break;

            case FieldDataType.Decimal:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out _))
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "Decimal"));
                    return false;
                }
                break;

            case FieldDataType.DateTime:
                if (value.ValueKind != JsonValueKind.String)
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "DateTime"));
                    return false;
                }

                var dateString = value.GetString();
                if (dateString is null || !DateTime.TryParse(
                    dateString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
                {
                    errors.Add(ValidationError.TypeMismatch(field.Name, "DateTime"));
                    return false;
                }
                break;

            default:
                // Unknown data type - treat as valid to be forward-compatible
                break;
        }

        return true;
    }

    /// <summary>
    /// Validates string-specific constraints (MaxLength and Pattern).
    /// </summary>
    private static void ValidateStringConstraints(FieldDefinition field, JsonElement value, List<ValidationError> errors)
    {
        var stringValue = value.GetString();
        if (stringValue is null)
        {
            return;
        }

        // Validate MaxLength
        if (field.MaxLength.HasValue && stringValue.Length > field.MaxLength.Value)
        {
            errors.Add(ValidationError.MaxLengthExceeded(field.Name, field.MaxLength.Value));
        }

        // Validate Pattern
        if (!string.IsNullOrEmpty(field.Pattern))
        {
            try
            {
                if (!Regex.IsMatch(stringValue, field.Pattern))
                {
                    errors.Add(ValidationError.PatternMismatch(field.Name));
                }
            }
            catch (RegexParseException)
            {
                // Invalid regex pattern in descriptor - log and skip pattern validation
                // In a real scenario, this should be logged or handled appropriately
            }
        }
    }
}
