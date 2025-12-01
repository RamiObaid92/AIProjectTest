using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Microsoft.Playwright;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// Playwright API tests for the Resources endpoints.
/// </summary>
/// <remarks>
/// <para>
/// These tests assume Library.WebApi is running and reachable at the BaseUrl 
/// configured in <see cref="ApiTestFixture"/>.
/// </para>
/// <para>
/// Database state is persistent between tests, so uniqueness is achieved using 
/// random ownerId/title values (e.g., <see cref="Guid.NewGuid"/>).
/// </para>
/// <para>
/// TypeDescriptor for "book" must exist in appsettings.json and require at least 
/// the "title" field in the payload.
/// </para>
/// </remarks>
[Collection("ApiTests")]
public class ResourceApiTests
{
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceApiTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared API test fixture.</param>
    public ResourceApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Helper Methods

    /// <summary>
    /// Creates a new "book" resource via POST /api/resources and returns the response JSON.
    /// </summary>
    /// <param name="title">The title of the book.</param>
    /// <param name="ownerId">The owner ID for the resource.</param>
    /// <param name="author">The author of the book. Defaults to "Test Author".</param>
    /// <returns>The created resource as a JsonElement.</returns>
    private async Task<JsonElement> CreateBookResourceAsync(string title, string ownerId, string author = "Test Author")
    {
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = ownerId,
                metadata = new
                {
                    tags = new[] { "playwright-test" }
                },
                payload = new
                {
                    title = title,
                    author = author
                }
            }
        });

        Assert.Equal(201, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);

        return body.Value;
    }

    /// <summary>
    /// Extracts the id (Guid) from a resource JSON element.
    /// </summary>
    /// <param name="resourceJson">The JSON element representing a resource.</param>
    /// <returns>The parsed Guid.</returns>
    private static Guid GetId(JsonElement resourceJson)
    {
        var idProperty = resourceJson.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(idProperty), "Resource id should not be null or empty");
        return Guid.Parse(idProperty);
    }

    #endregion

    #region POST /api/resources Tests

    /// <summary>
    /// Tests that POST /api/resources with a valid "book" payload returns 201 Created
    /// with the correct resource data.
    /// </summary>
    [Fact]
    public async Task CreateResource_Book_ReturnsCreatedResource()
    {
        // Arrange
        var title = $"Dune {Guid.NewGuid()}";
        var ownerId = $"api-test-owner-{Guid.NewGuid()}";

        // Act
        var created = await CreateBookResourceAsync(title, ownerId);

        // Assert
        Assert.Equal("book", created.GetProperty("type").GetString());
        Assert.Equal(ownerId, created.GetProperty("ownerId").GetString());
        Assert.Equal(title, created.GetProperty("payload").GetProperty("title").GetString());
        Assert.True(Guid.TryParse(created.GetProperty("id").GetString(), out _), "id should be a valid Guid");

        // Verify timestamps are present
        Assert.False(string.IsNullOrWhiteSpace(created.GetProperty("createdAtUtc").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(created.GetProperty("updatedAtUtc").GetString()));

        // Verify metadata was stored
        var tags = created.GetProperty("metadata").GetProperty("tags");
        Assert.Equal("playwright-test", tags[0].GetString());
    }

    #endregion

    #region GET /api/resources/{id} Tests

    /// <summary>
    /// Tests that GET /api/resources/{id} returns the created resource with correct data.
    /// </summary>
    [Fact]
    public async Task GetResourceById_ReturnsCreatedResource()
    {
        // Arrange - Create a resource first
        var title = $"Get Test Book {Guid.NewGuid()}";
        var ownerId = $"get-test-owner-{Guid.NewGuid()}";
        var created = await CreateBookResourceAsync(title, ownerId);
        var id = GetId(created);

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{id}");

        // Assert - Status code
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);
        var json = body.Value;

        // Assert - Data matches
        Assert.Equal(id.ToString(), json.GetProperty("id").GetString());
        Assert.Equal("book", json.GetProperty("type").GetString());
        Assert.Equal(ownerId, json.GetProperty("ownerId").GetString());
        Assert.Equal(title, json.GetProperty("payload").GetProperty("title").GetString());

        // Assert - Metadata is preserved
        var tags = json.GetProperty("metadata").GetProperty("tags");
        Assert.Equal("playwright-test", tags[0].GetString());
    }

    /// <summary>
    /// Tests that GET /api/resources/{id} returns 404 for a non-existent resource.
    /// </summary>
    [Fact]
    public async Task GetResourceById_NonExistent_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{nonExistentId}");

        // Assert
        Assert.Equal(404, response.Status);
    }

    #endregion

    #region GET /api/resources (Query) Tests

    /// <summary>
    /// Tests that GET /api/resources with type and ownerId filters returns only matching items.
    /// </summary>
    [Fact]
    public async Task GetResources_WithTypeAndOwnerFilter_ReturnsExpectedItems()
    {
        // Arrange - Create two resources with different owners
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var owner1 = $"owner-filter-1-{uniqueSuffix}";
        var owner2 = $"owner-filter-2-{uniqueSuffix}";
        var titleA = $"Filter Test A {uniqueSuffix}";
        var titleB = $"Filter Test B {uniqueSuffix}";

        await CreateBookResourceAsync(titleA, owner1);
        await CreateBookResourceAsync(titleB, owner2);

        // Act - Query for books owned by owner1
        var response = await _fixture.Request.GetAsync(
            $"/api/resources?type=book&ownerId={owner1}&pageNumber=1&pageSize=10");

        // Assert - Status code
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);
        var json = body.Value;

        // Assert - Response is an array
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.True(json.GetArrayLength() >= 1, "Should return at least one result");

        // Assert - All returned items match the filter
        var items = json.EnumerateArray().ToList();
        Assert.All(items, item =>
        {
            Assert.Equal("book", item.GetProperty("type").GetString());
            Assert.Equal(owner1, item.GetProperty("ownerId").GetString());
        });

        // Assert - The expected item is in the results
        Assert.Contains(items, item =>
            item.GetProperty("payload").GetProperty("title").GetString() == titleA);
    }

    /// <summary>
    /// Tests that GET /api/resources without filters returns a list of resources.
    /// </summary>
    [Fact]
    public async Task GetResources_WithoutFilters_ReturnsArray()
    {
        // Arrange - Ensure at least one resource exists
        await CreateBookResourceAsync($"List Test {Guid.NewGuid()}", "list-test-owner");

        // Act
        var response = await _fixture.Request.GetAsync("/api/resources?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(200, response.Status);

        var body = await response.JsonAsync();
        Assert.NotNull(body);
        var json = body.Value;

        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.True(json.GetArrayLength() >= 1, "Should return at least one resource");
    }

    #endregion

    #region PUT /api/resources/{id} Tests

    /// <summary>
    /// Tests that PUT /api/resources/{id} updates the payload and metadata correctly.
    /// </summary>
    [Fact]
    public async Task UpdateResource_ChangesPayloadAndMetadata()
    {
        // Arrange - Create a resource first
        var originalTitle = $"Original Title {Guid.NewGuid()}";
        var ownerId = $"update-test-owner-{Guid.NewGuid()}";
        var created = await CreateBookResourceAsync(originalTitle, ownerId);
        var id = GetId(created);

        var updatedTitle = $"Updated Title {Guid.NewGuid()}";

        // Act - Update the resource
        var updateResponse = await _fixture.Request.PutAsync($"/api/resources/{id}", new APIRequestContextOptions
        {
            DataObject = new
            {
                payload = new
                {
                    title = updatedTitle,
                    author = "Updated Author"
                },
                metadata = new
                {
                    tags = new[] { "updated", "playwright-test" }
                }
            }
        });

        // Assert - Status code
        Assert.Equal(200, updateResponse.Status);

        var body = await updateResponse.JsonAsync();
        Assert.NotNull(body);
        var json = body.Value;

        // Assert - Payload was updated
        Assert.Equal(updatedTitle, json.GetProperty("payload").GetProperty("title").GetString());

        // Assert - Metadata was updated
        var tags = json.GetProperty("metadata").GetProperty("tags")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        Assert.Contains("updated", tags);
        Assert.Contains("playwright-test", tags);

        // Assert - Type and owner remain unchanged
        Assert.Equal("book", json.GetProperty("type").GetString());
        Assert.Equal(ownerId, json.GetProperty("ownerId").GetString());
    }

    /// <summary>
    /// Tests that PUT /api/resources/{id} returns 404 for a non-existent resource.
    /// </summary>
    [Fact]
    public async Task UpdateResource_NonExistent_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.Request.PutAsync($"/api/resources/{nonExistentId}", new APIRequestContextOptions
        {
            DataObject = new
            {
                payload = new
                {
                    title = "Should Not Work",
                    author = "Nobody"
                }
            }
        });

        // Assert
        Assert.Equal(404, response.Status);
    }

    #endregion

    #region DELETE /api/resources/{id} Tests

    /// <summary>
    /// Tests that DELETE /api/resources/{id} removes the resource and subsequent GET returns 404.
    /// </summary>
    [Fact]
    public async Task DeleteResource_RemovesResource()
    {
        // Arrange - Create a resource first
        var title = $"Delete Test Book {Guid.NewGuid()}";
        var ownerId = $"delete-test-owner-{Guid.NewGuid()}";
        var created = await CreateBookResourceAsync(title, ownerId);
        var id = GetId(created);

        // Act - Delete the resource
        var deleteResponse = await _fixture.Request.DeleteAsync($"/api/resources/{id}");

        // Assert - Delete returns 204 No Content
        Assert.Equal(204, deleteResponse.Status);

        // Act - Try to get the deleted resource
        var getResponse = await _fixture.Request.GetAsync($"/api/resources/{id}");

        // Assert - GET returns 404 Not Found
        Assert.Equal(404, getResponse.Status);
    }

    /// <summary>
    /// Tests that DELETE /api/resources/{id} returns 204 even for non-existent resources (idempotent).
    /// </summary>
    [Fact]
    public async Task DeleteResource_NonExistent_Returns204()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.Request.DeleteAsync($"/api/resources/{nonExistentId}");

        // Assert - Delete is idempotent, returns 204 even if resource doesn't exist
        Assert.Equal(204, response.Status);
    }

    #endregion
}
