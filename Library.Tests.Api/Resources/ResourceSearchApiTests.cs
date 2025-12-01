using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// API tests verifying search behavior via the searchText query parameter on GET /api/resources.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the searchText query parameter correctly filters resources
/// based on their computed SearchText field. For resources of type "book", the SearchText
/// is computed from payload fields like title (as configured in the TypeDescriptor).
/// </para>
/// <para>
/// Prerequisites:
/// <list type="bullet">
///   <item>The API must be running at the BaseUrl configured in <see cref="ApiTestFixture"/>.</item>
///   <item>The "book" TypeDescriptor must be configured to include "title" in SearchText computation.</item>
/// </list>
/// </para>
/// </remarks>
[Collection("ApiTests")]
public class ResourceSearchApiTests
{
    private readonly ApiTestFixture _fixture;

    public ResourceSearchApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Helper Methods

    /// <summary>
    /// Creates a book resource with the specified title and owner.
    /// </summary>
    /// <param name="title">The book title.</param>
    /// <param name="ownerId">The owner identifier.</param>
    /// <returns>The created resource as a JsonElement.</returns>
    private async Task<JsonElement> CreateBookAsync(string title, string ownerId)
    {
        var response = await _fixture.Request.PostAsync("/api/resources", new()
        {
            DataObject = new
            {
                type = "book",
                ownerId = ownerId,
                metadata = new
                {
                    tags = new[] { "search-test" }
                },
                payload = new
                {
                    title = title,
                    author = "Test Author"
                }
            }
        });

        Assert.Equal(201, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        return body.Value;
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task GetResources_WithSearchText_FindsMatchingTitle()
    {
        // Arrange - Create books with distinct titles
        var r1 = await CreateBookAsync("Dune", "search-owner-1");
        var r2 = await CreateBookAsync("Children of Dune", "search-owner-1");
        var r3 = await CreateBookAsync("The Hobbit", "search-owner-1");

        // Act - Search for "Dune" which should match r1 and r2 but not r3
        var response = await _fixture.Request.GetAsync(
            "/api/resources?type=book&searchText=Dune&pageNumber=1&pageSize=100");

        // Assert
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        var json = body.Value;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        var items = json.EnumerateArray().ToList();
        Assert.True(items.Count >= 2, $"Expected at least 2 items matching 'Dune', but got {items.Count}");

        // Verify matching items are included
        Assert.Contains(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == "Dune");
        Assert.Contains(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == "Children of Dune");

        // Verify non-matching item is excluded
        Assert.DoesNotContain(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == "The Hobbit");
    }

    [Fact]
    public async Task GetResources_WithSearchText_NoMatches_ReturnsEmptyArray()
    {
        // Arrange - Create some books that won't match our search
        await CreateBookAsync("Search Nothing 1", "search-owner-2");
        await CreateBookAsync("Search Nothing 2", "search-owner-2");

        // Act - Search for something that definitely won't match
        var response = await _fixture.Request.GetAsync(
            "/api/resources?type=book&searchText=XYZ_NO_MATCH_12345&pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        var json = body.Value;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Empty(json.EnumerateArray());
    }

    [Fact]
    public async Task GetResources_WithSearchText_PartialMatch_ReturnsItems()
    {
        // Arrange - Create a book with a longer title
        await CreateBookAsync("The Foundation Trilogy", "search-owner-3");

        // Act - Search for partial match "Foundation"
        var response = await _fixture.Request.GetAsync(
            "/api/resources?type=book&searchText=Foundation&pageNumber=1&pageSize=100");

        // Assert
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        var json = body.Value;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        var items = json.EnumerateArray().ToList();
        Assert.True(items.Count >= 1, "Expected at least 1 item matching 'Foundation'");

        // Verify the partial match is found
        Assert.Contains(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == "The Foundation Trilogy");
    }

    [Fact]
    public async Task GetResources_WithSearchText_CaseInsensitive_ReturnsItems()
    {
        // Arrange - Create a book with mixed case title
        await CreateBookAsync("Game of Thrones", "search-owner-4");

        // Act - Search with different casing
        var response = await _fixture.Request.GetAsync(
            "/api/resources?type=book&searchText=game&pageNumber=1&pageSize=100");

        // Assert
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        var json = body.Value;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);

        var items = json.EnumerateArray().ToList();
        Assert.True(items.Count >= 1, "Expected at least 1 item matching 'game' (case-insensitive)");

        // Verify the case-insensitive match is found
        Assert.Contains(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == "Game of Thrones");
    }

    #endregion
}
