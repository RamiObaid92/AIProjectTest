using System.Text.Json;
using Library.Application.Resources.Validation;
using Library.Application.TypeDescriptors;
using Library.Domain.Resources;
using Microsoft.Extensions.Logging;

namespace Library.Application.Resources;

/// <summary>
/// Application service for managing resources.
/// Orchestrates validation, persistence, and mapping between DTOs and domain models.
/// </summary>
public class ResourceService : IResourceService
{
    private readonly IResourceRepository _repository;
    private readonly IResourceValidationService _validationService;
    private readonly ITypeDescriptorRegistry _typeDescriptorRegistry;
    private readonly ILogger<ResourceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceService"/> class.
    /// </summary>
    /// <param name="repository">The resource repository for persistence operations.</param>
    /// <param name="validationService">The validation service for payload validation.</param>
    /// <param name="typeDescriptorRegistry">The type descriptor registry.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public ResourceService(
        IResourceRepository repository,
        IResourceValidationService validationService,
        ITypeDescriptorRegistry typeDescriptorRegistry,
        ILogger<ResourceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _typeDescriptorRegistry = typeDescriptorRegistry ?? throw new ArgumentNullException(nameof(typeDescriptorRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ResourceDto> CreateResourceAsync(ResourceCreateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            throw new ArgumentException("Resource type cannot be null or empty.", nameof(dto));
        }

        // Validate payload against type descriptor schema
        var validationResult = _validationService.Validate(dto.Type, dto.Payload);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Validation failed for creating resource of type '{TypeKey}' with {ErrorCount} errors",
                dto.Type,
                validationResult.Errors.Count);

            throw new ResourceValidationException(dto.Type, validationResult.Errors);
        }

        // Create domain object
        var utcNow = DateTime.UtcNow;
        var resource = dto.ToDomain(utcNow);

        // Compute and set SearchText
        resource.SearchText = BuildSearchText(dto.Type, dto.Payload, dto.Metadata);

        // Persist
        await _repository.AddAsync(resource, cancellationToken);

        _logger.LogInformation(
            "Created resource {ResourceId} of type '{TypeKey}'",
            resource.Id,
            resource.Type);

        return resource.ToDto();
    }

    /// <inheritdoc />
    public async Task<ResourceDto?> GetResourceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _repository.GetByIdAsync(id, cancellationToken);

        if (resource is null)
        {
            _logger.LogDebug("Resource {ResourceId} not found", id);
            return null;
        }

        return resource.ToDto();
    }

    /// <inheritdoc />
    public async Task<ResourceDto?> UpdateResourceAsync(Guid id, ResourceUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Load existing resource
        var resource = await _repository.GetByIdAsync(id, cancellationToken);
        if (resource is null)
        {
            _logger.LogDebug("Resource {ResourceId} not found for update", id);
            return null;
        }

        // Use existing type for validation (type is immutable)
        var typeKey = resource.Type;

        // Validate new payload against type descriptor schema
        var validationResult = _validationService.Validate(typeKey, dto.Payload);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Validation failed for updating resource {ResourceId} of type '{TypeKey}' with {ErrorCount} errors",
                id,
                typeKey,
                validationResult.Errors.Count);

            throw new ResourceValidationException(typeKey, validationResult.Errors);
        }

        // Apply update
        var utcNow = DateTime.UtcNow;
        resource.ApplyUpdate(dto, utcNow);

        // Compute and set SearchText
        resource.SearchText = BuildSearchText(resource.Type, dto.Payload, dto.Metadata);

        // Persist
        await _repository.UpdateAsync(resource, cancellationToken);

        _logger.LogInformation(
            "Updated resource {ResourceId} of type '{TypeKey}'",
            resource.Id,
            resource.Type);

        return resource.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Deleted resource {ResourceId}", id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResourceDto>> QueryResourcesAsync(ResourceQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var criteria = query.ToCriteria();
        var resources = await _repository.QueryAsync(criteria, cancellationToken);

        _logger.LogDebug(
            "Query returned {Count} resources for type '{Type}'",
            resources.Count,
            query.Type ?? "(all)");

        return resources.Select(r => r.ToDto()).ToList();
    }

    /// <summary>
    /// Builds a denormalized search text string from the payload and metadata
    /// based on the TypeDescriptor configuration.
    /// </summary>
    /// <param name="typeKey">The resource type key.</param>
    /// <param name="payload">The payload JSON element.</param>
    /// <param name="metadata">The optional metadata JSON element.</param>
    /// <returns>A space-separated string of searchable terms, or null if no terms found.</returns>
    private string? BuildSearchText(string typeKey, JsonElement payload, JsonElement? metadata)
    {
        var terms = new List<string>();
        var descriptor = _typeDescriptorRegistry.GetDescriptorOrDefault(typeKey);

        if (descriptor is null)
        {
            // Fallback: collect all top-level string values from payload
            if (payload.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in payload.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            terms.Add(value);
                        }
                    }
                }
            }
        }
        else
        {
            // Use descriptor to determine which fields to include

            // Add title field if configured
            if (!string.IsNullOrEmpty(descriptor.UiHints?.TitleField))
            {
                var titleValue = GetStringProperty(payload, descriptor.UiHints.TitleField);
                if (!string.IsNullOrWhiteSpace(titleValue))
                {
                    terms.Add(titleValue);
                }
            }

            // Add full-text fields if configured
            if (descriptor.Indexing?.FullTextFields is not null)
            {
                foreach (var fieldName in descriptor.Indexing.FullTextFields)
                {
                    var fieldValue = GetStringProperty(payload, fieldName);
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                    {
                        terms.Add(fieldValue);
                    }
                }
            }

            // TODO: Optionally extract tags from metadata
            // if (metadata.HasValue && metadata.Value.TryGetProperty("tags", out var tagsElement)
            //     && tagsElement.ValueKind == JsonValueKind.Array)
            // {
            //     foreach (var tag in tagsElement.EnumerateArray())
            //     {
            //         if (tag.ValueKind == JsonValueKind.String)
            //         {
            //             var tagValue = tag.GetString();
            //             if (!string.IsNullOrWhiteSpace(tagValue))
            //             {
            //                 terms.Add(tagValue);
            //             }
            //         }
            //     }
            // }
        }

        if (terms.Count == 0)
        {
            return null;
        }

        // Remove duplicates and join with spaces
        var distinctTerms = terms.Distinct(StringComparer.OrdinalIgnoreCase);
        return string.Join(" ", distinctTerms);
    }

    /// <summary>
    /// Gets a string property value from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <param name="propertyName">The name of the property to read.</param>
    /// <returns>The string value, or null if not found or not a string.</returns>
    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }
}
