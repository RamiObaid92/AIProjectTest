using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Library.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Library.Tests.Integration.Resources;

/// <summary>
/// Integration tests for <see cref="Library.Infrastructure.Repositories.ResourceRepository"/>.
/// Tests use a real in-memory SQLite database to verify persistence operations.
/// </summary>
[Collection("ResourceRepositoryTests")]
public class ResourceRepositoryTests
{
    private readonly ResourceRepositoryFixture _fixture;

    public ResourceRepositoryTests(ResourceRepositoryFixture fixture)
    {
        _fixture = fixture;
    }

    #region Helper Methods

    /// <summary>
    /// Clears all resources from the database.
    /// </summary>
    private async Task ClearDatabaseAsync()
    {
        _fixture.DbContext.Resources.RemoveRange(_fixture.DbContext.Resources);
        await _fixture.DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a sample Resource with the specified parameters.
    /// </summary>
    private static Resource CreateSampleResource(
        string type = "book",
        string ownerId = "user-1",
        object? payload = null,
        object? metadata = null,
        DateTime? utcNow = null)
    {
        var timestamp = utcNow ?? DateTime.UtcNow;
        var payloadJson = JsonSerializer.Serialize(payload ?? new { title = "Sample" });
        var metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;

        return Resource.CreateNew(
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            utcNow: timestamp);
    }

    #endregion

    #region AddAsync + GetByIdAsync Tests

    [Fact]
    public async Task AddAsync_Then_GetByIdAsync_PersistsAndReturnsResource()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var resource = Resource.CreateNew(
            type: "book",
            ownerId: "user-1",
            metadataJson: JsonSerializer.Serialize(new { tags = new[] { "sci-fi" } }),
            payloadJson: JsonSerializer.Serialize(new { title = "Dune" }),
            utcNow: utcNow);

        // Act
        await _fixture.Repository.AddAsync(resource, CancellationToken.None);
        var loaded = await _fixture.Repository.GetByIdAsync(resource.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(resource.Id, loaded!.Id);
        Assert.Equal("book", loaded.Type);
        Assert.Equal("user-1", loaded.OwnerId);
        Assert.Equal(resource.PayloadJson, loaded.PayloadJson);
        Assert.Equal(resource.MetadataJson, loaded.MetadataJson);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _fixture.Repository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesPayloadAndMetadata()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var resource = Resource.CreateNew(
            type: "book",
            ownerId: "user-1",
            metadataJson: JsonSerializer.Serialize(new { tags = new[] { "tag1" } }),
            payloadJson: JsonSerializer.Serialize(new { title = "Original" }),
            utcNow: utcNow);

        await _fixture.Repository.AddAsync(resource, CancellationToken.None);

        // Modify the resource
        var utcNow2 = DateTime.UtcNow.AddSeconds(1);
        resource.UpdatePayload(
            payloadJson: JsonSerializer.Serialize(new { title = "Updated" }),
            metadataJson: JsonSerializer.Serialize(new { tags = new[] { "tag2", "tag3" } }),
            utcNow: utcNow2);

        // Act
        await _fixture.Repository.UpdateAsync(resource, CancellationToken.None);
        var loaded = await _fixture.Repository.GetByIdAsync(resource.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);

        // Verify payload was updated
        using var payloadDoc = JsonDocument.Parse(loaded!.PayloadJson);
        Assert.Equal("Updated", payloadDoc.RootElement.GetProperty("title").GetString());

        // Verify metadata was updated
        Assert.NotNull(loaded.MetadataJson);
        using var metadataDoc = JsonDocument.Parse(loaded.MetadataJson!);
        var tags = metadataDoc.RootElement.GetProperty("tags")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        Assert.Contains("tag2", tags);
        Assert.Contains("tag3", tags);

        // Verify UpdatedAtUtc was updated
        Assert.True(loaded.UpdatedAtUtc >= utcNow2.AddSeconds(-1));
    }

    [Fact]
    public async Task UpdateAsync_NonExistentResource_DoesNothing()
    {
        // Arrange
        var nonExistentResource = Resource.CreateNew(
            type: "book",
            ownerId: "user-1",
            metadataJson: null,
            payloadJson: JsonSerializer.Serialize(new { title = "Ghost" }),
            utcNow: DateTime.UtcNow);

        // Act - Should not throw
        await _fixture.Repository.UpdateAsync(nonExistentResource, CancellationToken.None);

        // Assert - Resource should still not exist
        var result = await _fixture.Repository.GetByIdAsync(nonExistentResource.Id, CancellationToken.None);
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingResource_RemovesFromDatabase()
    {
        // Arrange
        var resource = CreateSampleResource();
        await _fixture.Repository.AddAsync(resource, CancellationToken.None);

        // Verify it exists
        var beforeDelete = await _fixture.Repository.GetByIdAsync(resource.Id, CancellationToken.None);
        Assert.NotNull(beforeDelete);

        // Act
        await _fixture.Repository.DeleteAsync(resource.Id, CancellationToken.None);

        // Assert
        var afterDelete = await _fixture.Repository.GetByIdAsync(resource.Id, CancellationToken.None);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentResource_DoesNothing()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act - Should not throw
        await _fixture.Repository.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert - No exception means success
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_FiltersByTypeAndOwner()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        // Create multiple resources with different types and owners
        var bookUser1 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "Book 1" });
        var bookUser2 = CreateSampleResource(type: "book", ownerId: "user-2", payload: new { title = "Book 2" });
        var articleUser1 = CreateSampleResource(type: "article", ownerId: "user-1", payload: new { title = "Article 1" });

        await _fixture.Repository.AddAsync(bookUser1, CancellationToken.None);
        await _fixture.Repository.AddAsync(bookUser2, CancellationToken.None);
        await _fixture.Repository.AddAsync(articleUser1, CancellationToken.None);

        // Act - Query for books owned by user-1
        var criteria = new ResourceQueryCriteria
        {
            Type = "book",
            OwnerId = "user-1"
        };

        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var result = results.Single();
        Assert.Equal("book", result.Type);
        Assert.Equal("user-1", result.OwnerId);
        Assert.Equal(bookUser1.Id, result.Id);
    }

    [Fact]
    public async Task QueryAsync_FiltersByTypeOnly()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var book1 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "Book 1" });
        var book2 = CreateSampleResource(type: "book", ownerId: "user-2", payload: new { title = "Book 2" });
        var article1 = CreateSampleResource(type: "article", ownerId: "user-1", payload: new { title = "Article 1" });

        await _fixture.Repository.AddAsync(book1, CancellationToken.None);
        await _fixture.Repository.AddAsync(book2, CancellationToken.None);
        await _fixture.Repository.AddAsync(article1, CancellationToken.None);

        // Act
        var criteria = new ResourceQueryCriteria { Type = "book" };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("book", r.Type));
    }

    [Fact]
    public async Task QueryAsync_FiltersByOwnerOnly()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var book1 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "Book 1" });
        var article1 = CreateSampleResource(type: "article", ownerId: "user-1", payload: new { title = "Article 1" });
        var book2 = CreateSampleResource(type: "book", ownerId: "user-2", payload: new { title = "Book 2" });

        await _fixture.Repository.AddAsync(book1, CancellationToken.None);
        await _fixture.Repository.AddAsync(article1, CancellationToken.None);
        await _fixture.Repository.AddAsync(book2, CancellationToken.None);

        // Act
        var criteria = new ResourceQueryCriteria { OwnerId = "user-1" };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("user-1", r.OwnerId));
    }

    [Fact]
    public async Task QueryAsync_NoFilters_ReturnsAll()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var resource1 = CreateSampleResource(type: "book", ownerId: "user-1");
        var resource2 = CreateSampleResource(type: "article", ownerId: "user-2");

        await _fixture.Repository.AddAsync(resource1, CancellationToken.None);
        await _fixture.Repository.AddAsync(resource2, CancellationToken.None);

        // Act
        var criteria = new ResourceQueryCriteria();
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task QueryAsync_AppliesPaging()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        // Create resources with staggered creation times for predictable ordering
        var baseTime = DateTime.UtcNow;
        var resource1 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "First" }, utcNow: baseTime);
        var resource2 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "Second" }, utcNow: baseTime.AddSeconds(1));
        var resource3 = CreateSampleResource(type: "book", ownerId: "user-1", payload: new { title = "Third" }, utcNow: baseTime.AddSeconds(2));

        await _fixture.Repository.AddAsync(resource1, CancellationToken.None);
        await _fixture.Repository.AddAsync(resource2, CancellationToken.None);
        await _fixture.Repository.AddAsync(resource3, CancellationToken.None);

        // Act - Skip the first, take one
        var criteria = new ResourceQueryCriteria
        {
            Type = "book",
            Skip = 1,
            Take = 1
        };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var result = results.Single();

        // Repository orders by CreatedAt ascending, so skipping 1 should give us "Second"
        using var doc = JsonDocument.Parse(result.PayloadJson);
        Assert.Equal("Second", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task QueryAsync_TakeOnly_LimitsResults()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var resource1 = CreateSampleResource(type: "book", payload: new { title = "A" });
        var resource2 = CreateSampleResource(type: "book", payload: new { title = "B" });
        var resource3 = CreateSampleResource(type: "book", payload: new { title = "C" });

        await _fixture.Repository.AddAsync(resource1, CancellationToken.None);
        await _fixture.Repository.AddAsync(resource2, CancellationToken.None);
        await _fixture.Repository.AddAsync(resource3, CancellationToken.None);

        // Act
        var criteria = new ResourceQueryCriteria
        {
            Type = "book",
            Take = 2
        };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task QueryAsync_CreatedAfterUtc_FiltersCorrectly()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var baseTime = DateTime.UtcNow;
        var oldResource = CreateSampleResource(type: "book", payload: new { title = "Old" }, utcNow: baseTime.AddDays(-2));
        var newResource = CreateSampleResource(type: "book", payload: new { title = "New" }, utcNow: baseTime);

        await _fixture.Repository.AddAsync(oldResource, CancellationToken.None);
        await _fixture.Repository.AddAsync(newResource, CancellationToken.None);

        // Act - Filter for resources created after yesterday
        var criteria = new ResourceQueryCriteria
        {
            CreatedAfterUtc = baseTime.AddDays(-1)
        };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
        using var doc = JsonDocument.Parse(results.Single().PayloadJson);
        Assert.Equal("New", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task QueryAsync_CreatedBeforeUtc_FiltersCorrectly()
    {
        // Arrange - Clear database first
        await ClearDatabaseAsync();

        var baseTime = DateTime.UtcNow;
        var oldResource = CreateSampleResource(type: "book", payload: new { title = "Old" }, utcNow: baseTime.AddDays(-2));
        var newResource = CreateSampleResource(type: "book", payload: new { title = "New" }, utcNow: baseTime);

        await _fixture.Repository.AddAsync(oldResource, CancellationToken.None);
        await _fixture.Repository.AddAsync(newResource, CancellationToken.None);

        // Act - Filter for resources created before yesterday
        var criteria = new ResourceQueryCriteria
        {
            CreatedBeforeUtc = baseTime.AddDays(-1)
        };
        var results = await _fixture.Repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
        using var doc = JsonDocument.Parse(results.Single().PayloadJson);
        Assert.Equal("Old", doc.RootElement.GetProperty("title").GetString());
    }

    #endregion
}
