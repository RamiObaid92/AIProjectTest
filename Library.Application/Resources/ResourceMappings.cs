using System.Text.Json;
using Library.Domain.Resources;

namespace Library.Application.Resources;

/// <summary>
/// Provides mapping methods between Resource DTOs and domain models.
/// </summary>
public static class ResourceMappings
{
    private const int DefaultPageSize = 50;

    /// <summary>
    /// Maps a <see cref="Resource"/> domain model to a <see cref="ResourceDto"/>.
    /// </summary>
    /// <param name="resource">The domain resource to map.</param>
    /// <returns>A DTO representation of the resource.</returns>
    public static ResourceDto ToDto(this Resource resource)
    {
        return new ResourceDto
        {
            Id = resource.Id,
            Type = resource.Type,
            CreatedAtUtc = resource.CreatedAtUtc,
            UpdatedAtUtc = resource.UpdatedAtUtc,
            OwnerId = resource.OwnerId,
            Metadata = ParseJsonOrNull(resource.MetadataJson),
            Payload = JsonSerializer.Deserialize<JsonElement>(resource.PayloadJson)
        };
    }

    /// <summary>
    /// Maps a <see cref="ResourceCreateDto"/> to a new <see cref="Resource"/> domain model.
    /// </summary>
    /// <param name="dto">The create DTO containing the resource data.</param>
    /// <param name="utcNow">The current UTC timestamp for CreatedAtUtc and UpdatedAtUtc.</param>
    /// <returns>A new Resource domain object.</returns>
    public static Resource ToDomain(this ResourceCreateDto dto, DateTime utcNow)
    {
        var metadataJson = dto.Metadata.HasValue
            ? JsonSerializer.Serialize(dto.Metadata.Value)
            : null;

        var payloadJson = JsonSerializer.Serialize(dto.Payload);

        return Resource.CreateNew(
            type: dto.Type,
            ownerId: dto.OwnerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            utcNow: utcNow);
    }

    /// <summary>
    /// Applies an update DTO to an existing <see cref="Resource"/> domain model.
    /// </summary>
    /// <param name="resource">The existing resource to update.</param>
    /// <param name="dto">The update DTO containing the new payload and metadata.</param>
    /// <param name="utcNow">The current UTC timestamp for UpdatedAtUtc.</param>
    public static void ApplyUpdate(this Resource resource, ResourceUpdateDto dto, DateTime utcNow)
    {
        var metadataJson = dto.Metadata.HasValue
            ? JsonSerializer.Serialize(dto.Metadata.Value)
            : null;

        var payloadJson = JsonSerializer.Serialize(dto.Payload);

        resource.UpdatePayload(payloadJson, metadataJson, utcNow);
    }

    /// <summary>
    /// Maps a <see cref="ResourceQueryDto"/> to a <see cref="ResourceQueryCriteria"/> domain object.
    /// </summary>
    /// <param name="dto">The query DTO containing filter and pagination parameters.</param>
    /// <returns>A ResourceQueryCriteria for use with the repository.</returns>
    public static ResourceQueryCriteria ToCriteria(this ResourceQueryDto dto)
    {
        var pageNumber = dto.PageNumber < 1 ? 1 : dto.PageNumber;
        var pageSize = dto.PageSize <= 0 ? DefaultPageSize : dto.PageSize;

        var skip = (pageNumber - 1) * pageSize;
        var take = pageSize;

        return new ResourceQueryCriteria
        {
            Type = dto.Type,
            OwnerId = dto.OwnerId,
            CreatedAfterUtc = dto.CreatedAfterUtc,
            CreatedBeforeUtc = dto.CreatedBeforeUtc,
            Skip = skip,
            Take = take,
            SearchText = dto.SearchText
        };
    }

    /// <summary>
    /// Parses a JSON string to a JsonElement, returning null if the string is null or empty.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A JsonElement if the string is valid JSON; otherwise, null.</returns>
    private static JsonElement? ParseJsonOrNull(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
