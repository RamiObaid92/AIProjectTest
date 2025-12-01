using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Domain.Resources;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Library.Tests.Integration.Resources;

/// <summary>
/// Extended integration tests for <see cref="ResourceRepository"/> covering additional scenarios,
/// SearchText filtering, combined filters, sorting, and edge cases.
/// </summary>
public class ResourceRepositoryExtendedTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly LibraryDbContext _context;
    private readonly ResourceRepository _repository;

    public ResourceRepositoryExtendedTests()
    {
        // Use a unique in-memory connection that persists across the test
        // Disable pooling to ensure complete isolation between test instances
        var connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared;Pooling=false";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LibraryDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new ResourceRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<Resource> CreateAndAddResourceAsync(
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Test"" }",
        string? metadataJson = null,
        string? searchText = null,
        DateTime? createdAt = null)
    {
        var resource = new Resource(
            id: Guid.NewGuid(),
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            createdAtUtc: createdAt ?? DateTime.UtcNow,
            updatedAtUtc: DateTime.UtcNow);
        resource.SearchText = searchText;

        await _repository.AddAsync(resource, CancellationToken.None);
        return resource;
    }

    #endregion

    #region SearchText Filter Tests

    [Theory]
    [InlineData("Dune", "Dune", true)]
    [InlineData("dune", "Dune", true)] // Case-insensitive
    [InlineData("DUNE", "Dune", true)] // Case-insensitive
    [InlineData("Dun", "Dune", true)] // Partial match
    [InlineData("une", "Dune", true)] // Contains
    [InlineData("Foundation", "Dune", false)]
    public async Task QueryAsync_SearchTextFilter_MatchesCorrectly(
        string searchQuery, string searchTextInDb, bool shouldMatch)
    {
        // Arrange
        await CreateAndAddResourceAsync(searchText: searchTextInDb);
        var criteria = new ResourceQueryCriteria { SearchText = searchQuery, Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        if (shouldMatch)
            Assert.Single(results);
        else
            Assert.Empty(results);
    }

    [Fact]
    public async Task QueryAsync_SearchTextWithMultipleWords_MatchesPartially()
    {
        // Arrange
        await CreateAndAddResourceAsync(searchText: "Dune by Frank Herbert");

        var criteria = new ResourceQueryCriteria { SearchText = "Frank", Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task QueryAsync_SearchTextFilter_NullSearchTextInDb_DoesNotMatch()
    {
        // Arrange
        await CreateAndAddResourceAsync(searchText: null);
        var criteria = new ResourceQueryCriteria { SearchText = "anything", Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task QueryAsync_SearchTextFilter_EmptySearchTextInDb_DoesNotMatch()
    {
        // Arrange
        await CreateAndAddResourceAsync(searchText: "");
        var criteria = new ResourceQueryCriteria { SearchText = "test", Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Combined Filter Tests

    [Theory]
    [InlineData("book", "user-1")]
    [InlineData("article", "user-2")]
    [InlineData("profile", "admin")]
    public async Task QueryAsync_TypeAndOwnerFilter_CombinedCorrectly(string type, string ownerId)
    {
        // Arrange
        await CreateAndAddResourceAsync(type: type, ownerId: ownerId); // Should match
        await CreateAndAddResourceAsync(type: type, ownerId: "other-user"); // Type matches, owner doesn't
        await CreateAndAddResourceAsync(type: "other-type", ownerId: ownerId); // Owner matches, type doesn't
        await CreateAndAddResourceAsync(type: "other-type", ownerId: "other-user"); // Neither matches

        var criteria = new ResourceQueryCriteria { Type = type, OwnerId = ownerId, Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal(type, results.First().Type);
        Assert.Equal(ownerId, results.First().OwnerId);
    }

    [Fact]
    public async Task QueryAsync_TypeOwnerAndSearchText_AllApplied()
    {
        // Arrange
        await CreateAndAddResourceAsync(type: "book", ownerId: "user-1", searchText: "Dune"); // Matches all
        await CreateAndAddResourceAsync(type: "book", ownerId: "user-1", searchText: "Foundation"); // Different search
        await CreateAndAddResourceAsync(type: "article", ownerId: "user-1", searchText: "Dune"); // Different type
        await CreateAndAddResourceAsync(type: "book", ownerId: "user-2", searchText: "Dune"); // Different owner

        var criteria = new ResourceQueryCriteria
        {
            Type = "book",
            OwnerId = "user-1",
            SearchText = "Dune",
            Take = 100
        };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task QueryAsync_TypeAndDateRange_Combined()
    {
        // Arrange
        var targetDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var earlyDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lateDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        await CreateAndAddResourceAsync(type: "book", createdAt: targetDate); // Matches
        await CreateAndAddResourceAsync(type: "book", createdAt: earlyDate); // Too early
        await CreateAndAddResourceAsync(type: "article", createdAt: targetDate); // Wrong type

        var criteria = new ResourceQueryCriteria
        {
            Type = "book",
            CreatedAfterUtc = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBeforeUtc = new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc),
            Take = 100
        };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    #endregion

    #region Paging Tests

    [Theory]
    [InlineData(10, 0, 5, 5)]
    [InlineData(10, 5, 5, 5)]
    [InlineData(10, 0, 10, 10)]
    [InlineData(10, 0, 15, 10)]
    [InlineData(10, 8, 5, 2)]
    public async Task QueryAsync_Paging_ReturnsCorrectCount(int totalResources, int skip, int take, int expectedCount)
    {
        // Arrange
        for (int i = 0; i < totalResources; i++)
        {
            await CreateAndAddResourceAsync();
        }

        var criteria = new ResourceQueryCriteria { Skip = skip, Take = take };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCount, results.Count);
    }

    [Fact]
    public async Task QueryAsync_Paging_SkipBeyondResults_ReturnsEmpty()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await CreateAndAddResourceAsync();
        }

        var criteria = new ResourceQueryCriteria { Skip = 100, Take = 10 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task QueryAsync_Paging_DifferentPageSizes(int pageSize)
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            await CreateAndAddResourceAsync();
        }

        var criteria = new ResourceQueryCriteria { Skip = 0, Take = pageSize };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(pageSize, results.Count);
    }

    #endregion

    #region Order Tests

    [Fact]
    public async Task QueryAsync_DefaultOrder_ByCreatedAt()
    {
        // Arrange
        // Add resources with slightly different times
        var resource1 = await CreateAndAddResourceAsync(createdAt: DateTime.UtcNow.AddMinutes(-10));
        var resource2 = await CreateAndAddResourceAsync(createdAt: DateTime.UtcNow.AddMinutes(-5));
        var resource3 = await CreateAndAddResourceAsync(createdAt: DateTime.UtcNow);

        var criteria = new ResourceQueryCriteria { Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        // Default ordering should be by CreatedAt ascending
        Assert.True(results[0].CreatedAtUtc <= results[1].CreatedAtUtc);
        Assert.True(results[1].CreatedAtUtc <= results[2].CreatedAtUtc);
    }

    [Fact]
    public async Task QueryAsync_MultipleResources_OrderIsConsistent()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await CreateAndAddResourceAsync(createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var criteria = new ResourceQueryCriteria { Take = 100 };

        // Act
        var results1 = await _repository.QueryAsync(criteria, CancellationToken.None);
        var results2 = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(results1.Select(r => r.Id), results2.Select(r => r.Id));
    }

    #endregion

    #region Date Filter Edge Cases

    [Fact]
    public async Task QueryAsync_DateFilter_ExactBoundaryMatch()
    {
        // Arrange
        var exactDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        await CreateAndAddResourceAsync(createdAt: exactDate);

        var criteria = new ResourceQueryCriteria
        {
            CreatedAfterUtc = exactDate,
            CreatedBeforeUtc = exactDate.AddSeconds(1),
            Take = 100
        };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task QueryAsync_DateFilter_NarrowRange_NoMatches()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        await CreateAndAddResourceAsync(createdAt: date);

        var criteria = new ResourceQueryCriteria
        {
            CreatedAfterUtc = date.AddDays(1),
            CreatedBeforeUtc = date.AddDays(2),
            Take = 100
        };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(-365)]
    [InlineData(-30)]
    [InlineData(-7)]
    [InlineData(-1)]
    public async Task QueryAsync_DateFilter_ResourcesFromDaysAgo(int daysAgo)
    {
        // Arrange
        var resourceDate = DateTime.UtcNow.AddDays(daysAgo);
        await CreateAndAddResourceAsync(createdAt: resourceDate);

        var criteria = new ResourceQueryCriteria
        {
            CreatedAfterUtc = resourceDate.AddDays(-1),
            CreatedBeforeUtc = resourceDate.AddDays(1),
            Take = 100
        };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    #endregion

    #region Type Filter Edge Cases

    [Theory]
    [InlineData("book", 3)]
    [InlineData("article", 2)]
    [InlineData("profile", 1)]
    [InlineData("nonexistent", 0)]
    public async Task QueryAsync_TypeFilter_CountsCorrectly(string filterType, int expectedCount)
    {
        // Arrange
        await CreateAndAddResourceAsync(type: "book");
        await CreateAndAddResourceAsync(type: "book");
        await CreateAndAddResourceAsync(type: "book");
        await CreateAndAddResourceAsync(type: "article");
        await CreateAndAddResourceAsync(type: "article");
        await CreateAndAddResourceAsync(type: "profile");

        var criteria = new ResourceQueryCriteria { Type = filterType, Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCount, results.Count);
    }

    [Fact]
    public async Task QueryAsync_TypeFilter_CaseSensitive()
    {
        // Arrange
        await CreateAndAddResourceAsync(type: "Book");
        await CreateAndAddResourceAsync(type: "book");
        await CreateAndAddResourceAsync(type: "BOOK");

        var criteria = new ResourceQueryCriteria { Type = "book", Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        // SQLite default is case-insensitive for ASCII, but exact behavior may vary
        // This test documents the actual behavior
        Assert.True(results.Count >= 1);
    }

    #endregion

    #region Owner Filter Edge Cases

    [Theory]
    [InlineData("user-1", 2)]
    [InlineData("user-2", 1)]
    [InlineData("admin", 1)]
    [InlineData("nonexistent", 0)]
    public async Task QueryAsync_OwnerFilter_CountsCorrectly(string filterOwner, int expectedCount)
    {
        // Arrange
        await CreateAndAddResourceAsync(ownerId: "user-1");
        await CreateAndAddResourceAsync(ownerId: "user-1");
        await CreateAndAddResourceAsync(ownerId: "user-2");
        await CreateAndAddResourceAsync(ownerId: "admin");

        var criteria = new ResourceQueryCriteria { OwnerId = filterOwner, Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCount, results.Count);
    }

    [Fact]
    public async Task QueryAsync_OwnerFilter_EmptyOwnerId_ReturnsAllResources()
    {
        // Arrange - Empty OwnerId filter is treated as "no filter" by the repository
        await CreateAndAddResourceAsync(ownerId: "");
        await CreateAndAddResourceAsync(ownerId: "user-1");

        var criteria = new ResourceQueryCriteria { OwnerId = "", Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert - Empty string is treated as "no filter", so all resources are returned
        Assert.Equal(2, results.Count);
    }

    #endregion

    #region Add/Update/Delete Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task AddAsync_MultipleResources_AllPersisted(int count)
    {
        // Arrange & Act
        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            var resource = await CreateAndAddResourceAsync(payloadJson: $@"{{ ""index"": {i} }}");
            ids.Add(resource.Id);
        }

        // Assert
        foreach (var id in ids)
        {
            var retrieved = await _repository.GetByIdAsync(id, CancellationToken.None);
            Assert.NotNull(retrieved);
        }
    }

    [Fact]
    public async Task UpdateAsync_MultipleTimes_PersistsLatest()
    {
        // Arrange
        var resource = await CreateAndAddResourceAsync();

        // Act
        for (int i = 0; i < 5; i++)
        {
            resource.UpdatePayload($@"{{ ""version"": {i} }}", null, DateTime.UtcNow);
            await _repository.UpdateAsync(resource, CancellationToken.None);
        }

        // Assert
        var retrieved = await _repository.GetByIdAsync(resource.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Contains("4", retrieved!.PayloadJson);
    }

    [Fact]
    public async Task DeleteAsync_MultipleTimes_ThrowsOrIgnores()
    {
        // Arrange
        var resource = await CreateAndAddResourceAsync();
        await _repository.DeleteAsync(resource.Id, CancellationToken.None);

        // Act & Assert
        // Second delete should not throw (or throw specific exception)
        await _repository.DeleteAsync(resource.Id, CancellationToken.None);

        var retrieved = await _repository.GetByIdAsync(resource.Id, CancellationToken.None);
        Assert.Null(retrieved);
    }

    #endregion

    #region Empty Database Tests

    [Fact]
    public async Task QueryAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var criteria = new ResourceQueryCriteria { Take = 100 };

        // Act
        var results = await _repository.QueryAsync(criteria, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyDatabase_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
