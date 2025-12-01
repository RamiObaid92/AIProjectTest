using System;
using System.Data.Common;
using System.Threading.Tasks;
using Library.Domain.Resources;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Library.Tests.Integration.Resources;

/// <summary>
/// Test fixture that manages a shared in-memory SQLite database, LibraryDbContext, and ResourceRepository
/// for integration tests. The database persists for the lifetime of the fixture.
/// </summary>
public class ResourceRepositoryFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the database context for direct database access in tests.
    /// </summary>
    public LibraryDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Gets the repository under test.
    /// </summary>
    public IResourceRepository Repository { get; private set; } = null!;

    private DbConnection _connection = null!;

    /// <summary>
    /// Initializes the in-memory SQLite database, applies migrations, and creates the repository.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and open a shared in-memory SQLite connection
        // The database exists only while the connection is open
        _connection = new SqliteConnection("DataSource=:memory:;Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();

        // Build DbContext options using the open connection
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create DbContext and apply migrations
        DbContext = new LibraryDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();

        // Create the repository instance
        Repository = new ResourceRepository(DbContext);
    }

    /// <summary>
    /// Disposes the database context and closes the connection.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (DbContext is not null)
        {
            await DbContext.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}

/// <summary>
/// xUnit collection definition for ResourceRepository integration tests.
/// Tests in this collection share the same <see cref="ResourceRepositoryFixture"/> instance.
/// </summary>
[CollectionDefinition("ResourceRepositoryTests")]
public class ResourceRepositoryCollection : ICollectionFixture<ResourceRepositoryFixture>
{
}
