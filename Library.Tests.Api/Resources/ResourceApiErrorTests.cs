using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// API-level error scenario tests using a live Web API.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the API returns correct error responses for various
/// invalid request scenarios including validation errors, unknown types, and
/// non-existing resources.
/// </para>
/// <para>
/// Prerequisites:
/// <list type="bullet">
///   <item>The API must be running at the BaseUrl configured in <see cref="ApiTestFixture"/>.</item>
///   <item>The "book" TypeDescriptor must be configured in appsettings.json with "title" and "author" as required fields.</item>
/// </list>
/// </para>
/// </remarks>
[Collection("ApiTests")]
public class ResourceApiErrorTests
{
    private readonly ApiTestFixture _fixture;

    public ResourceApiErrorTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Create with validation errors

    [Fact]
    public async Task CreateResource_MissingRequiredField_Returns400WithValidationErrors()
    {
        // Arrange
        var requestBody = new
        {
            type = "book",
            ownerId = "error-test-owner-1",
            payload = new { } // missing required title and author fields
        };

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new()
        {
            DataObject = requestBody
        });
        var json = await response.JsonAsync();

        // Assert
        Assert.Equal(400, response.Status);
        Assert.NotNull(json);

        var root = json.Value;
        Assert.Equal("book", root.GetProperty("typeKey").GetString());
        Assert.True(root.TryGetProperty("errors", out var errorsElement));
        Assert.Equal(JsonValueKind.Array, errorsElement.ValueKind);
        Assert.Contains(errorsElement.EnumerateArray(), e =>
            e.GetProperty("field").GetString() == "title" &&
            e.GetProperty("code").GetString() == "Required");
    }

    [Fact]
    public async Task CreateResource_UnknownType_Returns400WithUnknownTypeError()
    {
        // Arrange
        var requestBody = new
        {
            type = "does-not-exist",
            ownerId = "error-test-owner-2",
            payload = new
            {
                title = "Some Title"
            }
        };

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new()
        {
            DataObject = requestBody
        });
        var json = await response.JsonAsync();

        // Assert
        Assert.Equal(400, response.Status);
        Assert.NotNull(json);

        var root = json.Value;
        Assert.Equal("does-not-exist", root.GetProperty("typeKey").GetString());
        Assert.True(root.TryGetProperty("errors", out var errorsElement));
        Assert.Contains(errorsElement.EnumerateArray(), e =>
            e.GetProperty("code").GetString() == "UnknownType");
    }

    #endregion

    #region Get non-existing resource

    [Fact]
    public async Task GetResource_NonExistingId_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{id}");

        // Assert
        Assert.Equal(404, response.Status);
    }

    #endregion

    #region Query parameter validation

    [Fact]
    public async Task GetResources_InvalidDateQuery_Returns400()
    {
        // Arrange
        var url = "/api/resources?createdAfterUtc=not-a-valid-datetime";

        // Act
        var response = await _fixture.Request.GetAsync(url);
        var json = await response.JsonAsync();

        // Assert
        Assert.Equal(400, response.Status);
        Assert.NotNull(json);

        var root = json.Value;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", root.GetProperty("title").GetString());
    }

    #endregion
}
