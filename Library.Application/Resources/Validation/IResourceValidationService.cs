using System.Text.Json;

namespace Library.Application.Resources.Validation;

/// <summary>
/// Provides validation of resource payloads against type descriptor schemas.
/// </summary>
public interface IResourceValidationService
{
    /// <summary>
    /// Validates a JSON payload against the schema defined by the specified type descriptor.
    /// </summary>
    /// <param name="typeKey">The type key identifying which type descriptor to use.</param>
    /// <param name="payload">The JSON payload to validate.</param>
    /// <returns>A validation result indicating success or containing validation errors.</returns>
    ResourceValidationResult Validate(string typeKey, JsonElement payload);
}
