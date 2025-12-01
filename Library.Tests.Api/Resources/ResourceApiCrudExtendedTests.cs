using System;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Microsoft.Playwright;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// Extended API tests for create, update, and delete operations with various payload scenarios.
/// </summary>
[Collection("ApiTests")]
public class ResourceApiCrudExtendedTests
{
    private readonly ApiTestFixture _fixture;

    public ResourceApiCrudExtendedTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Create - Payload Variation Tests

    [Theory]
    [InlineData(@"{ ""title"": ""Simple"", ""author"": ""Author"" }")]
    [InlineData(@"{ ""title"": ""With Number"", ""author"": ""Author"", ""count"": 42 }")]
    [InlineData(@"{ ""title"": ""With Bool"", ""author"": ""Author"", ""active"": true }")]
    [InlineData(@"{ ""title"": ""With Decimal"", ""author"": ""Author"", ""price"": 19.99 }")]
    public async Task CreateResource_VariousPayloads_ReturnsCreated(string payloadJson)
    {
        // Arrange
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = payloadDoc.RootElement
            }
        });

        // Assert
        Assert.Equal(201, response.Status);
    }

    [Fact]
    public async Task CreateResource_NestedPayload_ReturnsCreated()
    {
        // Arrange
        var payloadJson = @"{ ""title"": ""Book"", ""author"": ""Test"", ""authorInfo"": { ""name"": ""John"", ""age"": 45 } }";
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = payloadDoc.RootElement
            }
        });

        // Assert
        Assert.Equal(201, response.Status);
    }

    [Fact]
    public async Task CreateResource_ArrayInPayload_ReturnsCreated()
    {
        // Arrange
        var payloadJson = @"{ ""title"": ""Book"", ""author"": ""Test"", ""tags"": [""fiction"", ""sci-fi""] }";
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = payloadDoc.RootElement
            }
        });

        // Assert
        Assert.Equal(201, response.Status);
    }

    [Fact]
    public async Task CreateResource_ComplexPayload_PreservedInResponse()
    {
        // Arrange
        var payloadJson = @"{ ""title"": ""Complex"", ""author"": ""Test"", ""meta"": { ""tags"": [""a"", ""b""], ""count"": 5 } }";
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = payloadDoc.RootElement
            }
        });
        var content = await response.TextAsync();
        using var responseDoc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(201, response.Status);
        var payload = responseDoc.RootElement.GetProperty("payload");
        Assert.Equal("Complex", payload.GetProperty("title").GetString());
        Assert.Equal(5, payload.GetProperty("meta").GetProperty("count").GetInt32());
    }

    #endregion

    #region Create - Type Variation Tests

    [Fact]
    public async Task CreateResource_BookType_ReturnsCreated()
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test Book", author = "Author" }
            }
        });

        // Assert
        Assert.Equal(201, response.Status);

        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("book", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task CreateResource_ArticleType_WithUrl_ReturnsCreated()
    {
        // Arrange - article type requires title and url fields
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "article",
                ownerId = "test-user",
                payload = new { title = "Test Article", url = "https://example.com/article" }
            }
        });

        // Assert
        Assert.Equal(201, response.Status);

        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("article", doc.RootElement.GetProperty("type").GetString());
    }

    #endregion

    #region Create - Owner Variation Tests

    [Theory]
    [InlineData("user-1")]
    [InlineData("owner_123")]
    [InlineData("admin@example.com")]
    [InlineData("")]
    public async Task CreateResource_VariousOwners_ReturnsCreated(string ownerId)
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId,
                payload = new { title = "Test", author = "Author" }
            }
        });

        // Assert
        Assert.Equal(201, response.Status);

        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(ownerId, doc.RootElement.GetProperty("ownerId").GetString());
    }

    #endregion

    #region Create - Metadata Tests

    [Fact]
    public async Task CreateResource_WithMetadata_ReturnsCreated()
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test", author = "Author" },
                metadata = new { tags = new[] { "important", "featured" }, priority = 1 }
            }
        });

        // Assert
        Assert.Equal(201, response.Status);

        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("metadata", out _));
    }

    [Fact]
    public async Task CreateResource_NullMetadata_ReturnsCreated()
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test", author = "Author" }
            }
        });

        // Assert
        Assert.Equal(201, response.Status);
    }

    #endregion

    #region Create - Response Structure Tests

    [Fact]
    public async Task CreateResource_ResponseHasId()
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test", author = "Author" }
            }
        });
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("id", out var idElement));
        Assert.True(Guid.TryParse(idElement.GetString(), out _));
    }

    [Fact]
    public async Task CreateResource_ResponseHasTimestamps()
    {
        // Act
        var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test", author = "Author" }
            }
        });
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("createdAtUtc", out _));
        Assert.True(doc.RootElement.TryGetProperty("updatedAtUtc", out _));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateResource_ExistingResource_ReturnsOk()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Original", author = "Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _fixture.Request.PutAsync($"/api/resources/{id}", new APIRequestContextOptions
        {
            DataObject = new
            {
                payload = new { title = "Updated", author = "Author" }
            }
        });

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Theory]
    [InlineData(@"{ ""title"": ""Updated 1"", ""author"": ""Author"" }")]
    [InlineData(@"{ ""title"": ""Updated 2"", ""author"": ""Author"", ""newField"": ""value"" }")]
    [InlineData(@"{ ""title"": ""Different"", ""author"": ""New Author"", ""payload"": true }")]
    public async Task UpdateResource_VariousPayloads_ReturnsOk(string newPayloadJson)
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Original", author = "Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        using var payloadDoc = JsonDocument.Parse(newPayloadJson);

        // Act
        var response = await _fixture.Request.PutAsync($"/api/resources/{id}", new APIRequestContextOptions
        {
            DataObject = new { payload = payloadDoc.RootElement }
        });

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task UpdateResource_NonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.Request.PutAsync($"/api/resources/{Guid.NewGuid()}", new APIRequestContextOptions
        {
            DataObject = new
            {
                payload = new { title = "Updated", author = "Author" }
            }
        });

        // Assert
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task UpdateResource_PayloadPreservedAfterUpdate()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Original", author = "Author", extra = "data" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        await _fixture.Request.PutAsync($"/api/resources/{id}", new APIRequestContextOptions
        {
            DataObject = new
            {
                payload = new { title = "Updated", author = "Author", newField = 123 }
            }
        });
        var getResponse = await _fixture.Request.GetAsync($"/api/resources/{id}");
        var getContent = await getResponse.TextAsync();
        using var getDoc = JsonDocument.Parse(getContent);

        // Assert
        var payload = getDoc.RootElement.GetProperty("payload");
        Assert.Equal("Updated", payload.GetProperty("title").GetString());
        Assert.Equal(123, payload.GetProperty("newField").GetInt32());
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteResource_ExistingResource_ReturnsNoContent()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "To Delete", author = "Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _fixture.Request.DeleteAsync($"/api/resources/{id}");

        // Assert
        Assert.Equal(204, response.Status);
    }

    [Fact]
    public async Task DeleteResource_NonExistentId_ReturnsNotFoundOrNoContent()
    {
        // Act
        var response = await _fixture.Request.DeleteAsync($"/api/resources/{Guid.NewGuid()}");

        // Assert
        // Depending on implementation, may return NotFound or NoContent
        Assert.True(response.Status == 404 || response.Status == 204);
    }

    [Fact]
    public async Task DeleteResource_ThenGet_ReturnsNotFound()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "To Delete", author = "Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        await _fixture.Request.DeleteAsync($"/api/resources/{id}");

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{id}");

        // Assert
        Assert.Equal(404, response.Status);
    }

    #endregion

    #region Get By Id Tests

    [Fact]
    public async Task GetResource_ExistingId_ReturnsOk()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Test", author = "Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{id}");

        // Assert
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task GetResource_NonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task GetResource_ReturnsCorrectData()
    {
        // Arrange
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "specific-owner",
                payload = new { title = "Specific Title", author = "Specific Author" }
            }
        });
        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _fixture.Request.GetAsync($"/api/resources/{id}");
        var content = await response.TextAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal("book", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("specific-owner", doc.RootElement.GetProperty("ownerId").GetString());
        Assert.Equal("Specific Title", doc.RootElement.GetProperty("payload").GetProperty("title").GetString());
    }

    #endregion

    #region Multiple Operations Tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task CreateMultipleResources_AllCreatedSuccessfully(int count)
    {
        // Arrange & Act
        var responses = new System.Collections.Generic.List<int>();
        for (int i = 0; i < count; i++)
        {
            var response = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
            {
                DataObject = new
                {
                    type = "book",
                    ownerId = "test-user",
                    payload = new { title = $"Book {i}", author = "Author" }
                }
            });
            responses.Add(response.Status);
        }

        // Assert
        Assert.All(responses, code => Assert.Equal(201, code));
    }

    [Fact]
    public async Task CreateUpdateDelete_FullLifecycle()
    {
        // Create
        var createResponse = await _fixture.Request.PostAsync("/api/resources", new APIRequestContextOptions
        {
            DataObject = new
            {
                type = "book",
                ownerId = "test-user",
                payload = new { title = "Lifecycle Test", author = "Author" }
            }
        });
        Assert.Equal(201, createResponse.Status);

        var createContent = await createResponse.TextAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var id = createDoc.RootElement.GetProperty("id").GetString();

        // Update
        var updateResponse = await _fixture.Request.PutAsync($"/api/resources/{id}", new APIRequestContextOptions
        {
            DataObject = new { payload = new { title = "Updated Lifecycle Test", author = "Author" } }
        });
        Assert.Equal(200, updateResponse.Status);

        // Verify update
        var getResponse = await _fixture.Request.GetAsync($"/api/resources/{id}");
        var getContent = await getResponse.TextAsync();
        using var getDoc = JsonDocument.Parse(getContent);
        Assert.Equal("Updated Lifecycle Test", getDoc.RootElement.GetProperty("payload").GetProperty("title").GetString());

        // Delete
        var deleteResponse = await _fixture.Request.DeleteAsync($"/api/resources/{id}");
        Assert.Equal(204, deleteResponse.Status);

        // Verify deletion
        var finalGetResponse = await _fixture.Request.GetAsync($"/api/resources/{id}");
        Assert.Equal(404, finalGetResponse.Status);
    }

    #endregion
}
