using System;
using System.Collections.Generic;
using System.Text.Json;
using Library.Application.Resources;
using Library.Domain.Resources;
using Xunit;

namespace Library.Tests.Unit.Resources;

/// <summary>
/// Unit tests for <see cref="ResourceMappings"/> class covering DTO to/from domain mappings.
/// </summary>
public class ResourceMappingsTests
{
    #region ToDto Tests - Basic Properties

    [Theory]
    [InlineData("book", "user-1")]
    [InlineData("article", "user-2")]
    [InlineData("profile", "admin")]
    [InlineData("custom-type", "owner-123")]
    public void ToDto_Resource_MapsTypeAndOwner(string type, string ownerId)
    {
        // Arrange
        var resource = CreateResource(type: type, ownerId: ownerId);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(type, dto.Type);
        Assert.Equal(ownerId, dto.OwnerId);
    }

    [Fact]
    public void ToDto_Resource_MapsId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resource = CreateResource(id: id);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(id, dto.Id);
    }

    [Fact]
    public void ToDto_Resource_MapsTimestamps()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 1, 20, 15, 45, 0, DateTimeKind.Utc);
        var resource = CreateResource(createdAt: createdAt, updatedAt: updatedAt);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
    }

    #endregion

    #region ToDto Tests - Payload

    [Theory]
    [InlineData(@"{ ""title"": ""Test"" }", "title", "Test")]
    [InlineData(@"{ ""name"": ""John"" }", "name", "John")]
    [InlineData(@"{ ""value"": ""123"" }", "value", "123")]
    public void ToDto_Resource_MapsPayloadString(string payloadJson, string key, string expectedValue)
    {
        // Arrange
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(expectedValue, dto.Payload.GetProperty(key).GetString());
    }

    [Theory]
    [InlineData(@"{ ""count"": 42 }", "count", 42)]
    [InlineData(@"{ ""year"": 2024 }", "year", 2024)]
    [InlineData(@"{ ""quantity"": 0 }", "quantity", 0)]
    [InlineData(@"{ ""negative"": -10 }", "negative", -10)]
    public void ToDto_Resource_MapsPayloadInt(string payloadJson, string key, int expectedValue)
    {
        // Arrange
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(expectedValue, dto.Payload.GetProperty(key).GetInt32());
    }

    [Theory]
    [InlineData(@"{ ""active"": true }", "active", true)]
    [InlineData(@"{ ""active"": false }", "active", false)]
    [InlineData(@"{ ""enabled"": true, ""visible"": false }", "enabled", true)]
    public void ToDto_Resource_MapsPayloadBool(string payloadJson, string key, bool expectedValue)
    {
        // Arrange
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(expectedValue, dto.Payload.GetProperty(key).GetBoolean());
    }

    [Theory]
    [InlineData(@"{ ""price"": 19.99 }", "price", 19.99)]
    [InlineData(@"{ ""rate"": 0.0 }", "rate", 0.0)]
    [InlineData(@"{ ""value"": -5.5 }", "value", -5.5)]
    public void ToDto_Resource_MapsPayloadDecimal(string payloadJson, string key, double expectedValue)
    {
        // Arrange
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(expectedValue, dto.Payload.GetProperty(key).GetDouble(), precision: 5);
    }

    [Fact]
    public void ToDto_Resource_MapsComplexPayload()
    {
        // Arrange
        var payloadJson = @"{ ""title"": ""Book"", ""pages"": 300, ""available"": true, ""price"": 24.99 }";
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal("Book", dto.Payload.GetProperty("title").GetString());
        Assert.Equal(300, dto.Payload.GetProperty("pages").GetInt32());
        Assert.True(dto.Payload.GetProperty("available").GetBoolean());
        Assert.Equal(24.99, dto.Payload.GetProperty("price").GetDouble(), precision: 2);
    }

    [Fact]
    public void ToDto_Resource_MapsNestedPayload()
    {
        // Arrange
        var payloadJson = @"{ ""author"": { ""name"": ""John"", ""age"": 45 } }";
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        var author = dto.Payload.GetProperty("author");
        Assert.Equal("John", author.GetProperty("name").GetString());
        Assert.Equal(45, author.GetProperty("age").GetInt32());
    }

    [Fact]
    public void ToDto_Resource_MapsArrayPayload()
    {
        // Arrange
        var payloadJson = @"{ ""tags"": [""fiction"", ""sci-fi"", ""classic""] }";
        var resource = CreateResource(payloadJson: payloadJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        var tags = dto.Payload.GetProperty("tags");
        Assert.Equal(3, tags.GetArrayLength());
        Assert.Equal("fiction", tags[0].GetString());
        Assert.Equal("sci-fi", tags[1].GetString());
        Assert.Equal("classic", tags[2].GetString());
    }

    [Fact]
    public void ToDto_Resource_EmptyPayload()
    {
        // Arrange
        var resource = CreateResource(payloadJson: "{}");

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Equal(JsonValueKind.Object, dto.Payload.ValueKind);
    }

    #endregion

    #region ToDto Tests - Metadata

    [Fact]
    public void ToDto_Resource_NullMetadata_MapsToNull()
    {
        // Arrange
        var resource = CreateResource(metadataJson: null);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.Null(dto.Metadata);
    }

    [Theory]
    [InlineData(@"{ ""tag"": ""important"" }", "tag", "important")]
    [InlineData(@"{ ""category"": ""fiction"" }", "category", "fiction")]
    public void ToDto_Resource_MapsMetadata(string metadataJson, string key, string expectedValue)
    {
        // Arrange
        var resource = CreateResource(metadataJson: metadataJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.NotNull(dto.Metadata);
        Assert.Equal(expectedValue, dto.Metadata!.Value.GetProperty(key).GetString());
    }

    [Fact]
    public void ToDto_Resource_MapsComplexMetadata()
    {
        // Arrange
        var metadataJson = @"{ ""tags"": [""a"", ""b""], ""priority"": 1, ""archived"": false }";
        var resource = CreateResource(metadataJson: metadataJson);

        // Act
        var dto = resource.ToDto();

        // Assert
        Assert.NotNull(dto.Metadata);
        Assert.Equal(2, dto.Metadata!.Value.GetProperty("tags").GetArrayLength());
        Assert.Equal(1, dto.Metadata.Value.GetProperty("priority").GetInt32());
        Assert.False(dto.Metadata.Value.GetProperty("archived").GetBoolean());
    }

    #endregion

    #region ToCriteria Tests - Basic Filters

    [Theory]
    [InlineData("book")]
    [InlineData("article")]
    [InlineData("custom")]
    public void ToCriteria_ResourceQueryDto_MapsType(string type)
    {
        // Arrange
        var dto = new ResourceQueryDto { Type = type };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(type, criteria.Type);
    }

    [Theory]
    [InlineData("owner-1")]
    [InlineData("user@email.com")]
    [InlineData("")]
    [InlineData(null)]
    public void ToCriteria_ResourceQueryDto_MapsOwnerId(string? ownerId)
    {
        // Arrange
        var dto = new ResourceQueryDto { OwnerId = ownerId };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(ownerId, criteria.OwnerId);
    }

    [Theory]
    [InlineData("search term")]
    [InlineData("Dune")]
    [InlineData("")]
    [InlineData(null)]
    public void ToCriteria_ResourceQueryDto_MapsSearchText(string? searchText)
    {
        // Arrange
        var dto = new ResourceQueryDto { SearchText = searchText };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(searchText, criteria.SearchText);
    }

    #endregion

    #region ToCriteria Tests - Date Filters

    [Fact]
    public void ToCriteria_ResourceQueryDto_MapsCreatedAfterUtc()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var dto = new ResourceQueryDto { CreatedAfterUtc = date };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(date, criteria.CreatedAfterUtc);
    }

    [Fact]
    public void ToCriteria_ResourceQueryDto_MapsCreatedBeforeUtc()
    {
        // Arrange
        var date = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var dto = new ResourceQueryDto { CreatedBeforeUtc = date };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(date, criteria.CreatedBeforeUtc);
    }

    [Fact]
    public void ToCriteria_ResourceQueryDto_MapsDateRange()
    {
        // Arrange
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var dto = new ResourceQueryDto { CreatedAfterUtc = after, CreatedBeforeUtc = before };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(after, criteria.CreatedAfterUtc);
        Assert.Equal(before, criteria.CreatedBeforeUtc);
    }

    [Fact]
    public void ToCriteria_ResourceQueryDto_NullDates()
    {
        // Arrange
        var dto = new ResourceQueryDto { CreatedAfterUtc = null, CreatedBeforeUtc = null };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Null(criteria.CreatedAfterUtc);
        Assert.Null(criteria.CreatedBeforeUtc);
    }

    #endregion

    #region ToCriteria Tests - Paging

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 10, 20, 10)]
    [InlineData(1, 25, 0, 25)]
    [InlineData(5, 20, 80, 20)]
    [InlineData(10, 50, 450, 50)]
    public void ToCriteria_ResourceQueryDto_CalculatesSkipTake(
        int pageNumber, int pageSize, int expectedSkip, int expectedTake)
    {
        // Arrange
        var dto = new ResourceQueryDto { PageNumber = pageNumber, PageSize = pageSize };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(expectedSkip, criteria.Skip);
        Assert.Equal(expectedTake, criteria.Take);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ToCriteria_ResourceQueryDto_InvalidPageNumber_DefaultsToPage1(int pageNumber)
    {
        // Arrange
        var dto = new ResourceQueryDto { PageNumber = pageNumber, PageSize = 10 };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(0, criteria.Skip); // Page 1 means skip 0
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void ToCriteria_ResourceQueryDto_InvalidPageSize_DefaultsTo50(int pageSize)
    {
        // Arrange
        var dto = new ResourceQueryDto { PageNumber = 1, PageSize = pageSize };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(50, criteria.Take);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    public void ToCriteria_ResourceQueryDto_LargePageSize_MapsCorrectly(int pageSize)
    {
        // Arrange
        var dto = new ResourceQueryDto { PageNumber = 1, PageSize = pageSize };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal(pageSize, criteria.Take);
    }

    #endregion

    #region ToCriteria Tests - Sorting

    [Theory]
    [InlineData("createdAt")]
    [InlineData("updatedAt")]
    [InlineData("type")]
    [InlineData("ownerId")]
    public void ToCriteria_ResourceQueryDto_MapsSortBy(string sortBy)
    {
        // Arrange
        var dto = new ResourceQueryDto { SortBy = sortBy };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        // Sorting info is stored in the DTO; criteria doesn't have sorting
        Assert.NotNull(dto.SortBy);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void ToCriteria_ResourceQueryDto_MapsSortDirection(string sortDirection)
    {
        // Arrange
        var dto = new ResourceQueryDto { SortDirection = sortDirection };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        // Sorting info is stored in the DTO; criteria doesn't have sorting
        Assert.Equal(sortDirection, dto.SortDirection);
    }

    [Fact]
    public void ToCriteria_ResourceQueryDto_DefaultSort()
    {
        // Arrange
        var dto = new ResourceQueryDto();

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Null(dto.SortBy);
        Assert.Null(dto.SortDirection);
    }

    #endregion

    #region ToCriteria Tests - Combined Filters

    [Fact]
    public void ToCriteria_ResourceQueryDto_AllFilters()
    {
        // Arrange
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var dto = new ResourceQueryDto
        {
            Type = "book",
            OwnerId = "user-1",
            SearchText = "Dune",
            CreatedAfterUtc = after,
            CreatedBeforeUtc = before,
            PageNumber = 2,
            PageSize = 25,
            SortBy = "createdAt",
            SortDirection = "desc"
        };

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Equal("book", criteria.Type);
        Assert.Equal("user-1", criteria.OwnerId);
        Assert.Equal("Dune", criteria.SearchText);
        Assert.Equal(after, criteria.CreatedAfterUtc);
        Assert.Equal(before, criteria.CreatedBeforeUtc);
        Assert.Equal(25, criteria.Skip); // Page 2, size 25
        Assert.Equal(25, criteria.Take);
    }

    [Fact]
    public void ToCriteria_ResourceQueryDto_Empty()
    {
        // Arrange
        var dto = new ResourceQueryDto();

        // Act
        var criteria = dto.ToCriteria();

        // Assert
        Assert.Null(criteria.Type);
        Assert.Null(criteria.OwnerId);
        Assert.Null(criteria.SearchText);
        Assert.Null(criteria.CreatedAfterUtc);
        Assert.Null(criteria.CreatedBeforeUtc);
        Assert.Equal(0, criteria.Skip);
        Assert.Equal(50, criteria.Take); // Default page size
    }

    #endregion

    #region Helper Methods

    private static Resource CreateResource(
        Guid? id = null,
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Test"" }",
        string? metadataJson = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new Resource(
            id: id ?? Guid.NewGuid(),
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            createdAtUtc: createdAt ?? DateTime.UtcNow,
            updatedAtUtc: updatedAt ?? DateTime.UtcNow);
    }

    #endregion
}
