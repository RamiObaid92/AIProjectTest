using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Library.Application.Resources;
using Library.Application.Resources.Validation;
using Library.Application.TypeDescriptors;
using Library.Domain.Resources;
using Library.Domain.TypeDescriptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Library.Tests.Unit.Resources;

/// <summary>
/// Extended unit tests for <see cref="ResourceService"/> covering additional scenarios,
/// edge cases, SearchText behavior, and paging logic.
/// </summary>
public class ResourceServiceExtendedTests
{
    private readonly Mock<IResourceRepository> _repositoryMock;
    private readonly Mock<IResourceValidationService> _validationServiceMock;
    private readonly Mock<ITypeDescriptorRegistry> _typeDescriptorRegistryMock;
    private readonly ILogger<ResourceService> _logger;
    private readonly ResourceService _service;

    public ResourceServiceExtendedTests()
    {
        _repositoryMock = new Mock<IResourceRepository>();
        _validationServiceMock = new Mock<IResourceValidationService>();
        _typeDescriptorRegistryMock = new Mock<ITypeDescriptorRegistry>();
        _logger = NullLogger<ResourceService>.Instance;

        _service = new ResourceService(
            _repositoryMock.Object,
            _validationServiceMock.Object,
            _typeDescriptorRegistryMock.Object,
            _logger);
    }

    #region Helper Methods

    private static Resource CreateResource(
        Guid? id = null,
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Test"" }",
        string? metadataJson = null,
        string? searchText = null)
    {
        var resource = new Resource(
            id: id ?? Guid.NewGuid(),
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            createdAtUtc: DateTime.UtcNow,
            updatedAtUtc: DateTime.UtcNow);
        resource.SearchText = searchText;
        return resource;
    }

    private static ResourceCreateDto CreateDto(
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Test"" }",
        string? metadataJson = null)
    {
        using var payloadDoc = JsonDocument.Parse(payloadJson);
        var payload = payloadDoc.RootElement.Clone();

        JsonElement? metadata = null;
        if (metadataJson != null)
        {
            using var metadataDoc = JsonDocument.Parse(metadataJson);
            metadata = metadataDoc.RootElement.Clone();
        }

        return new ResourceCreateDto
        {
            Type = type,
            OwnerId = ownerId,
            Payload = payload,
            Metadata = metadata
        };
    }

    private void SetupValidationSuccess()
    {
        _validationServiceMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<JsonElement>()))
            .Returns(ResourceValidationResult.Success());
    }

    private void SetupValidationFailure(params ValidationError[] errors)
    {
        _validationServiceMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<JsonElement>()))
            .Returns(ResourceValidationResult.Failure(errors));
    }

    #endregion

    #region CreateResourceAsync - Type Variations Tests

    [Theory]
    [InlineData("book")]
    [InlineData("article")]
    [InlineData("user-profile")]
    [InlineData("my_custom_type")]
    [InlineData("Type123")]
    public async Task CreateResourceAsync_VariousTypes_CreatesCorrectType(string type)
    {
        // Arrange
        var dto = CreateDto(type: type);
        Resource? captured = null;

        SetupValidationSuccess();
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateResourceAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(type, result.Type);
        Assert.NotNull(captured);
        Assert.Equal(type, captured!.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateResourceAsync_EmptyOrNullType_ThrowsArgumentException(string? type)
    {
        // Arrange
        var dto = new ResourceCreateDto
        {
            Type = type!,
            OwnerId = "user-1",
            Payload = JsonDocument.Parse("{}").RootElement.Clone()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateResourceAsync(dto, CancellationToken.None));
    }

    #endregion

    #region CreateResourceAsync - Owner Variations Tests

    [Theory]
    [InlineData("user-1")]
    [InlineData("owner_123")]
    [InlineData("admin@example.com")]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateResourceAsync_VariousOwners_CreatesWithCorrectOwner(string? ownerId)
    {
        // Arrange
        var dto = CreateDto(ownerId: ownerId ?? "");
        Resource? captured = null;

        SetupValidationSuccess();
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateResourceAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(ownerId ?? "", result.OwnerId);
    }

    #endregion

    #region CreateResourceAsync - Validation Error Tests

    [Theory]
    [InlineData("title", "Required", "Title is required")]
    [InlineData("author", "Required", "Author is required")]
    [InlineData("name", "MaxLength", "Name exceeds max length")]
    [InlineData("email", "Pattern", "Invalid email format")]
    public async Task CreateResourceAsync_ValidationError_ThrowsWithCorrectDetails(
        string fieldName, string errorCode, string message)
    {
        // Arrange
        var dto = CreateDto();
        SetupValidationFailure(new ValidationError(fieldName, errorCode, message));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceValidationException>(() =>
            _service.CreateResourceAsync(dto, CancellationToken.None));

        Assert.Single(ex.Errors);
        Assert.Equal(fieldName, ex.Errors[0].FieldName);
        Assert.Equal(errorCode, ex.Errors[0].ErrorCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task CreateResourceAsync_MultipleValidationErrors_ThrowsWithAllErrors(int errorCount)
    {
        // Arrange
        var dto = CreateDto();
        var errors = Enumerable.Range(1, errorCount)
            .Select(i => new ValidationError($"field{i}", "Error", $"Error {i}"))
            .ToArray();
        SetupValidationFailure(errors);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ResourceValidationException>(() =>
            _service.CreateResourceAsync(dto, CancellationToken.None));

        Assert.Equal(errorCount, ex.Errors.Count);
    }

    #endregion

    #region CreateResourceAsync - SearchText Tests

    [Fact]
    public async Task CreateResourceAsync_WithDescriptor_ComputesSearchText()
    {
        // Arrange
        var dto = CreateDto(payloadJson: @"{ ""title"": ""Dune"", ""author"": ""Frank Herbert"" }");
        Resource? captured = null;

        var descriptor = new TypeDescriptor
        {
            TypeKey = "book",
            DisplayName = "Book",
            SchemaVersion = 1,
            Fields = new List<FieldDefinition>(),
            UiHints = new UiHints { TitleField = "title" },
            Indexing = new IndexingDefinition { FullTextFields = new[] { "title", "author" } }
        };

        _typeDescriptorRegistryMock
            .Setup(r => r.GetDescriptorOrDefault("book"))
            .Returns(descriptor);

        SetupValidationSuccess();
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateResourceAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.NotNull(captured!.SearchText);
        Assert.Contains("Dune", captured.SearchText);
    }

    [Fact]
    public async Task CreateResourceAsync_WithoutDescriptor_FallbackSearchText()
    {
        // Arrange
        var dto = CreateDto(payloadJson: @"{ ""title"": ""Test Title"", ""other"": ""Value"" }");
        Resource? captured = null;

        _typeDescriptorRegistryMock
            .Setup(r => r.GetDescriptorOrDefault("book"))
            .Returns((TypeDescriptor?)null);

        SetupValidationSuccess();
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateResourceAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        // Fallback should include all string values from payload
        Assert.NotNull(captured!.SearchText);
    }

    #endregion

    #region GetResourceByIdAsync Tests

    [Theory]
    [InlineData("book", "user-1")]
    [InlineData("article", "user-2")]
    [InlineData("profile", "admin")]
    public async Task GetResourceByIdAsync_ExistingResource_ReturnsCorrectDto(string type, string ownerId)
    {
        // Arrange
        var id = Guid.NewGuid();
        var resource = CreateResource(id: id, type: type, ownerId: ownerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        // Act
        var result = await _service.GetResourceByIdAsync(id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal(type, result.Type);
        Assert.Equal(ownerId, result.OwnerId);
    }

    [Fact]
    public async Task GetResourceByIdAsync_MultipleCalls_ReturnsCorrectResources()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var resource1 = CreateResource(id: id1, payloadJson: @"{ ""title"": ""First"" }");
        var resource2 = CreateResource(id: id2, payloadJson: @"{ ""title"": ""Second"" }");

        _repositoryMock.Setup(r => r.GetByIdAsync(id1, It.IsAny<CancellationToken>())).ReturnsAsync(resource1);
        _repositoryMock.Setup(r => r.GetByIdAsync(id2, It.IsAny<CancellationToken>())).ReturnsAsync(resource2);

        // Act
        var result1 = await _service.GetResourceByIdAsync(id1, CancellationToken.None);
        var result2 = await _service.GetResourceByIdAsync(id2, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("First", result1!.Payload.GetProperty("title").GetString());
        Assert.Equal("Second", result2!.Payload.GetProperty("title").GetString());
    }

    #endregion

    #region UpdateResourceAsync Tests

    [Fact]
    public async Task UpdateResourceAsync_ExistingResource_UpdatesSearchText()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingResource = CreateResource(id: id, searchText: "Old SearchText");

        using var payloadDoc = JsonDocument.Parse(@"{ ""title"": ""New Title"" }");
        var updateDto = new ResourceUpdateDto
        {
            Payload = payloadDoc.RootElement.Clone(),
            Metadata = null
        };

        var descriptor = new TypeDescriptor
        {
            TypeKey = "book",
            DisplayName = "Book",
            SchemaVersion = 1,
            Fields = new List<FieldDefinition>(),
            UiHints = new UiHints { TitleField = "title" }
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingResource);

        _typeDescriptorRegistryMock
            .Setup(r => r.GetDescriptorOrDefault("book"))
            .Returns(descriptor);

        SetupValidationSuccess();

        Resource? captured = null;
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateResourceAsync(id, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Contains("New Title", captured!.SearchText ?? "");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task UpdateResourceAsync_RepositoryCalledOnce_PerUpdate(int updateCount)
    {
        // Arrange
        var id = Guid.NewGuid();
        var resource = CreateResource(id: id);

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        SetupValidationSuccess();
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        for (int i = 0; i < updateCount; i++)
        {
            using var doc = JsonDocument.Parse($@"{{ ""title"": ""Title {i}"" }}");
            var dto = new ResourceUpdateDto { Payload = doc.RootElement.Clone() };
            await _service.UpdateResourceAsync(id, dto, CancellationToken.None);
        }

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Exactly(updateCount));
    }

    #endregion

    #region DeleteResourceAsync Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task DeleteResourceAsync_MultipleIds_CallsRepositoryForEach(int deleteCount)
    {
        // Arrange
        var ids = Enumerable.Range(0, deleteCount).Select(_ => Guid.NewGuid()).ToList();
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        foreach (var id in ids)
        {
            await _service.DeleteResourceAsync(id, CancellationToken.None);
        }

        // Assert
        foreach (var id in ids)
        {
            _repositoryMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion

    #region QueryResourcesAsync - Paging Tests

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 10, 20, 10)]
    [InlineData(1, 25, 0, 25)]
    [InlineData(5, 20, 80, 20)]
    public async Task QueryResourcesAsync_PageNumber_CalculatesSkipCorrectly(
        int pageNumber, int pageSize, int expectedSkip, int expectedTake)
    {
        // Arrange
        var query = new ResourceQueryDto { PageNumber = pageNumber, PageSize = pageSize };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(expectedSkip, captured!.Skip);
        Assert.Equal(expectedTake, captured.Take);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task QueryResourcesAsync_InvalidPageNumber_DefaultsToPage1(int pageNumber)
    {
        // Arrange
        var query = new ResourceQueryDto { PageNumber = pageNumber, PageSize = 10 };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(0, captured!.Skip); // Page 1 means Skip 0
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task QueryResourcesAsync_InvalidPageSize_DefaultsTo50(int pageSize)
    {
        // Arrange
        var query = new ResourceQueryDto { PageNumber = 1, PageSize = pageSize };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(50, captured!.Take); // Default page size is 50
    }

    #endregion

    #region QueryResourcesAsync - Filter Tests

    [Theory]
    [InlineData("book")]
    [InlineData("article")]
    [InlineData("custom-type")]
    public async Task QueryResourcesAsync_TypeFilter_PassedToCriteria(string type)
    {
        // Arrange
        var query = new ResourceQueryDto { Type = type };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(type, captured!.Type);
    }

    [Theory]
    [InlineData("owner-1")]
    [InlineData("user@example.com")]
    [InlineData("admin_123")]
    public async Task QueryResourcesAsync_OwnerFilter_PassedToCriteria(string ownerId)
    {
        // Arrange
        var query = new ResourceQueryDto { OwnerId = ownerId };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(ownerId, captured!.OwnerId);
    }

    [Theory]
    [InlineData("Dune")]
    [InlineData("Foundation")]
    [InlineData("test search")]
    public async Task QueryResourcesAsync_SearchTextFilter_PassedToCriteria(string searchText)
    {
        // Arrange
        var query = new ResourceQueryDto { SearchText = searchText };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(searchText, captured!.SearchText);
    }

    [Fact]
    public async Task QueryResourcesAsync_DateFilters_PassedToCriteria()
    {
        // Arrange
        var afterDate = DateTime.UtcNow.AddDays(-7);
        var beforeDate = DateTime.UtcNow;
        var query = new ResourceQueryDto { CreatedAfterUtc = afterDate, CreatedBeforeUtc = beforeDate };
        ResourceQueryCriteria? captured = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(afterDate, captured!.CreatedAfterUtc);
        Assert.Equal(beforeDate, captured.CreatedBeforeUtc);
    }

    #endregion

    #region QueryResourcesAsync - Result Mapping Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task QueryResourcesAsync_VariousResultCounts_MapsAllToDtos(int count)
    {
        // Arrange
        var query = new ResourceQueryDto();
        var resources = Enumerable.Range(0, count)
            .Select(i => CreateResource(payloadJson: $@"{{ ""title"": ""Item {i}"" }}"))
            .ToList();

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resources);

        // Act
        var result = await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(count, result.Count);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ResourceService(
            null!,
            _validationServiceMock.Object,
            _typeDescriptorRegistryMock.Object,
            _logger));
    }

    [Fact]
    public void Constructor_NullValidationService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ResourceService(
            _repositoryMock.Object,
            null!,
            _typeDescriptorRegistryMock.Object,
            _logger));
    }

    [Fact]
    public void Constructor_NullTypeDescriptorRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ResourceService(
            _repositoryMock.Object,
            _validationServiceMock.Object,
            null!,
            _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ResourceService(
            _repositoryMock.Object,
            _validationServiceMock.Object,
            _typeDescriptorRegistryMock.Object,
            null!));
    }

    #endregion
}
