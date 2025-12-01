using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Microsoft.Playwright;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// Extended API tests for resource endpoints covering query parameter combinations,
/// paging, sorting, and various filter scenarios.
/// </summary>
[Collection("ApiTests")]
public class ResourceApiQueryTests
{
    private readonly ApiTestFixture _fixture;

    public ResourceApiQueryTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Paging Tests

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 25)]
    [InlineData(1, 50)]
    [InlineData(2, 10)]
    [InlineData(5, 20)]
    public async Task GetResources_WithPagingParams_ReturnsOk(int pageNumber, int pageSize)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    public async Task GetResources_InvalidPageNumber_ReturnsOkWithDefaults(int pageNumber, int pageSize)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task GetResources_InvalidPageSize_ReturnsOkWithDefaults(int pageNumber, int pageSize)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    #endregion

    #region Type Filter Tests

    [Theory]
    [InlineData("book")]
    [InlineData("article")]
    [InlineData("profile")]
    [InlineData("custom-type")]
    public async Task GetResources_WithTypeFilter_ReturnsOk(string type)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?type={type}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task GetResources_WithTypeFilter_FilterApplied()
    {
        // Arrange
        var uniqueOwner = Guid.NewGuid().ToString();
        await CreateResourceAsync("book", @"{ ""title"": ""Book 1"", ""author"": ""Author"" }", uniqueOwner);
        await CreateResourceAsync("article", @"{ ""title"": ""Article 1"", ""url"": ""https://example.com/article1"" }", uniqueOwner);

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?type=book&ownerId={uniqueOwner}");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            Assert.Equal("book", item.GetProperty("type").GetString());
        }
    }

    #endregion

    #region Owner Filter Tests

    [Theory]
    [InlineData("user-1")]
    [InlineData("owner-123")]
    [InlineData("admin@example.com")]
    public async Task GetResources_WithOwnerFilter_ReturnsOk(string ownerId)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?ownerId={ownerId}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task GetResources_WithOwnerFilter_FilterApplied()
    {
        // Arrange
        var uniqueOwner = Guid.NewGuid().ToString();
        await CreateResourceAsync("book", @"{ ""title"": ""User1 Book"", ""author"": ""Author"" }", uniqueOwner);
        await CreateResourceAsync("book", @"{ ""title"": ""User2 Book"", ""author"": ""Author"" }", "user-2");

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?ownerId={uniqueOwner}");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            Assert.Equal(uniqueOwner, item.GetProperty("ownerId").GetString());
        }
    }

    #endregion

    #region SearchText Filter Tests

    [Theory]
    [InlineData("Dune")]
    [InlineData("Foundation")]
    [InlineData("test search")]
    [InlineData("multi word query")]
    public async Task GetResources_WithSearchText_ReturnsOk(string searchText)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?searchText={Uri.EscapeDataString(searchText)}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task GetResources_WithSearchText_ReturnsMatchingResources()
    {
        // Arrange
        var uniqueOwner = Guid.NewGuid().ToString();
        await CreateResourceAsync("book", @"{ ""title"": ""Dune"", ""author"": ""Frank Herbert"" }", uniqueOwner);
        await CreateResourceAsync("book", @"{ ""title"": ""Foundation"", ""author"": ""Isaac Asimov"" }", uniqueOwner);

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?searchText=Dune&ownerId={uniqueOwner}");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Contains("Dune", content);
    }

    #endregion

    #region Sort Tests

    [Theory]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    [InlineData("updatedAt", "asc")]
    [InlineData("updatedAt", "desc")]
    [InlineData("type", "asc")]
    [InlineData("type", "desc")]
    public async Task GetResources_WithSorting_ReturnsOk(string sortBy, string sortDirection)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?sortBy={sortBy}&sortDirection={sortDirection}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    #endregion

    #region Date Filter Tests

    [Fact]
    public async Task GetResources_WithDateFilters_ReturnsOk()
    {
        // Arrange
        var afterDate = "2024-01-01T00:00:00Z";
        var beforeDate = "2024-12-31T23:59:59Z";

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?createdAfterUtc={afterDate}&createdBeforeUtc={beforeDate}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Theory]
    [InlineData("2024-01-01T00:00:00Z")]
    [InlineData("2024-06-15T12:30:00Z")]
    [InlineData("2023-01-01T00:00:00Z")]
    public async Task GetResources_WithCreatedAfter_ReturnsOk(string afterDate)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?createdAfterUtc={afterDate}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Theory]
    [InlineData("2024-12-31T23:59:59Z")]
    [InlineData("2025-01-01T00:00:00Z")]
    [InlineData("2024-06-15T12:30:00Z")]
    public async Task GetResources_WithCreatedBefore_ReturnsOk(string beforeDate)
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?createdBeforeUtc={beforeDate}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public async Task GetResources_AllFilters_ReturnsOk()
    {
        // Act
        var response = await _fixture.Request.GetAsync(
            "/api/resources?type=book&ownerId=user-1&searchText=test" +
            "&pageNumber=1&pageSize=10" +
            "&sortBy=createdAt&sortDirection=desc" +
            "&createdAfterUtc=2024-01-01T00:00:00Z&createdBeforeUtc=2024-12-31T23:59:59Z");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task GetResources_TypeAndOwner_BothApplied()
    {
        // Arrange
        var uniqueOwner = Guid.NewGuid().ToString();
        await CreateResourceAsync("book", @"{ ""title"": ""Book"", ""author"": ""Author"" }", uniqueOwner);
        await CreateResourceAsync("article", @"{ ""title"": ""Article"", ""url"": ""https://example.com/article"" }", uniqueOwner);
        await CreateResourceAsync("book", @"{ ""title"": ""Book 2"", ""author"": ""Author"" }", "user-2");

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?type=book&ownerId={uniqueOwner}");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            Assert.Equal("book", item.GetProperty("type").GetString());
            Assert.Equal(uniqueOwner, item.GetProperty("ownerId").GetString());
        }
    }

    [Fact]
    public async Task GetResources_TypeAndSearchText_BothApplied()
    {
        // Arrange
        var uniqueOwner = Guid.NewGuid().ToString();
        await CreateResourceAsync("book", @"{ ""title"": ""Dune"", ""author"": ""Frank Herbert"" }", uniqueOwner);
        await CreateResourceAsync("article", @"{ ""title"": ""Dune Article"", ""url"": ""https://example.com/dune-article"" }", uniqueOwner);

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources?type=book&searchText=Dune&ownerId={uniqueOwner}");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            Assert.Equal("book", item.GetProperty("type").GetString());
        }
    }

    #endregion

    #region Empty Results Tests

    [Fact]
    public async Task GetResources_NonExistentType_ReturnsEmptyArray()
    {
        // Act
        var response = await _fixture.Request.GetAsync("/api/resources?type=nonexistent-type-xyz");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetResources_NonExistentOwner_ReturnsEmptyArray()
    {
        // Act
        var response = await _fixture.Request.GetAsync("/api/resources?ownerId=nonexistent-owner-xyz");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetResources_NonMatchingSearchText_ReturnsEmptyArray()
    {
        // Act
        var response = await _fixture.Request.GetAsync("/api/resources?searchText=xyz-nonexistent-search-123");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    #endregion

    #region Response Structure Tests

    [Fact]
    public async Task GetResources_ReturnsArrayOfResources()
    {
        // Act
        var response = await _fixture.Request.GetAsync("/api/resources");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetResources_EachResourceHasRequiredProperties()
    {
        // Arrange
        await CreateResourceAsync("book", @"{ ""title"": ""Test"", ""author"": ""Author"" }");

        // Act
        var response = await _fixture.Request.GetAsync("/api/resources?pageSize=1");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(200, response.Status);
        if (doc.RootElement.GetArrayLength() > 0)
        {
            var item = doc.RootElement[0];
            Assert.True(item.TryGetProperty("id", out _));
            Assert.True(item.TryGetProperty("type", out _));
            Assert.True(item.TryGetProperty("ownerId", out _));
            Assert.True(item.TryGetProperty("payload", out _));
            Assert.True(item.TryGetProperty("createdAtUtc", out _));
            Assert.True(item.TryGetProperty("updatedAtUtc", out _));
        }
    }

    #endregion

    #region Helper Methods

    private async Task<JsonElement> CreateResourceAsync(string type, string payloadJson, string ownerId = "api-test-user")
    {
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type,
                ownerId,
                payload = payloadDoc.RootElement
            }
        });

        Assert.True(response.Ok, $"Failed to create resource: {await response.TextAsync()}");

        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.Clone();
    }

    #endregion
}
