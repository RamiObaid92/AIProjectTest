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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Library.Tests.Unit.Resources;

/// <summary>
/// Unit tests for <see cref="ResourceService"/>.
/// </summary>
public class ResourceServiceTests
{
    private readonly Mock<IResourceRepository> _repositoryMock;
    private readonly Mock<IResourceValidationService> _validationServiceMock;
    private readonly Mock<ITypeDescriptorRegistry> _typeDescriptorRegistryMock;
    private readonly ILogger<ResourceService> _logger;
    private readonly ResourceService _service;

    public ResourceServiceTests()
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

    /// <summary>
    /// Creates a sample Resource domain object for testing.
    /// </summary>
    private static Resource CreateSampleResource(
        Guid? id = null,
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Dune"" }",
        string? metadataJson = null)
    {
        var resourceId = id ?? Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        return new Resource(
            id: resourceId,
            type: type,
            ownerId: ownerId,
            metadataJson: metadataJson,
            payloadJson: payloadJson,
            createdAtUtc: utcNow,
            updatedAtUtc: utcNow);
    }

    /// <summary>
    /// Creates a sample ResourceCreateDto for testing.
    /// </summary>
    private static ResourceCreateDto CreateSampleCreateDto(
        string type = "book",
        string ownerId = "user-1",
        string payloadJson = @"{ ""title"": ""Dune"" }")
    {
        using var document = JsonDocument.Parse(payloadJson);
        var payload = document.RootElement.Clone();

        return new ResourceCreateDto
        {
            Type = type,
            OwnerId = ownerId,
            Payload = payload,
            Metadata = null
        };
    }

    /// <summary>
    /// Creates a sample ResourceUpdateDto for testing.
    /// </summary>
    private static ResourceUpdateDto CreateSampleUpdateDto(string payloadJson = @"{ ""title"": ""Updated Title"" }")
    {
        using var document = JsonDocument.Parse(payloadJson);
        var payload = document.RootElement.Clone();

        return new ResourceUpdateDto
        {
            Payload = payload,
            Metadata = null
        };
    }

    /// <summary>
    /// Creates a sample ResourceQueryDto for testing.
    /// </summary>
    private static ResourceQueryDto CreateSampleQueryDto(
        string? type = "book",
        int pageNumber = 1,
        int pageSize = 10)
    {
        return new ResourceQueryDto
        {
            Type = type,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    #endregion

    #region CreateResourceAsync Tests

    [Fact]
    public async Task CreateResourceAsync_ValidRequest_CallsRepositoryAndReturnsDto()
    {
        // Arrange
        var dto = CreateSampleCreateDto();
        Resource? capturedResource = null;

        _validationServiceMock
            .Setup(v => v.Validate("book", It.IsAny<JsonElement>()))
            .Returns(ResourceValidationResult.Success());

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((resource, _) => capturedResource = resource)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateResourceAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("book", result.Type);
        Assert.Equal("user-1", result.OwnerId);
        Assert.Equal("Dune", result.Payload.GetProperty("title").GetString());

        Assert.NotNull(capturedResource);
        Assert.Equal("book", capturedResource!.Type);
        Assert.Equal("user-1", capturedResource.OwnerId);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Once);
        _validationServiceMock.Verify(v => v.Validate("book", It.IsAny<JsonElement>()), Times.Once);
    }

    [Fact]
    public async Task CreateResourceAsync_InvalidRequest_ThrowsValidationExceptionAndDoesNotCallRepository()
    {
        // Arrange
        var dto = CreateSampleCreateDto(payloadJson: @"{ }"); // missing title

        var errors = new[]
        {
            new ValidationError("title", "Required", "Title is required.")
        };
        var validationResult = ResourceValidationResult.Failure(errors);

        _validationServiceMock
            .Setup(v => v.Validate("book", It.IsAny<JsonElement>()))
            .Returns(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceValidationException>(
            () => _service.CreateResourceAsync(dto, CancellationToken.None));

        Assert.Equal("book", exception.TypeKey);
        Assert.Single(exception.Errors);
        Assert.Equal("title", exception.Errors[0].FieldName);
        Assert.Equal("Required", exception.Errors[0].ErrorCode);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateResourceAsync_NullDto_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateResourceAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateResourceAsync_EmptyType_ThrowsArgumentException()
    {
        // Arrange
        var dto = CreateSampleCreateDto(type: "");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateResourceAsync(dto, CancellationToken.None));
    }

    #endregion

    #region GetResourceByIdAsync Tests

    [Fact]
    public async Task GetResourceByIdAsync_ExistingResource_ReturnsDto()
    {
        // Arrange
        var resource = CreateSampleResource();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        // Act
        var result = await _service.GetResourceByIdAsync(resource.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(resource.Id, result!.Id);
        Assert.Equal("book", result.Type);
        Assert.Equal("user-1", result.OwnerId);
        Assert.Equal("Dune", result.Payload.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetResourceByIdAsync_NonExistingResource_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        // Act
        var result = await _service.GetResourceByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateResourceAsync Tests

    [Fact]
    public async Task UpdateResourceAsync_NonExistingResource_ReturnsNullAndDoesNotCallUpdate()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = CreateSampleUpdateDto();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        // Act
        var result = await _service.UpdateResourceAsync(nonExistentId, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateResourceAsync_ValidRequest_UpdatesResourceAndReturnsDto()
    {
        // Arrange
        var existingResource = CreateSampleResource();
        var dto = CreateSampleUpdateDto(@"{ ""title"": ""Updated Title"" }");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(existingResource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingResource);

        _validationServiceMock
            .Setup(v => v.Validate("book", It.IsAny<JsonElement>()))
            .Returns(ResourceValidationResult.Success());

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateResourceAsync(existingResource.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingResource.Id, result!.Id);
        Assert.Equal("book", result.Type);
        Assert.Equal("Updated Title", result.Payload.GetProperty("title").GetString());

        _validationServiceMock.Verify(v => v.Validate("book", It.IsAny<JsonElement>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateResourceAsync_InvalidPayload_ThrowsValidationExceptionAndDoesNotUpdate()
    {
        // Arrange
        var existingResource = CreateSampleResource();
        var dto = CreateSampleUpdateDto(@"{ }"); // invalid payload

        _repositoryMock
            .Setup(r => r.GetByIdAsync(existingResource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingResource);

        var errors = new[]
        {
            new ValidationError("title", "Required", "Title is required.")
        };
        var validationResult = ResourceValidationResult.Failure(errors);

        _validationServiceMock
            .Setup(v => v.Validate("book", It.IsAny<JsonElement>()))
            .Returns(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceValidationException>(
            () => _service.UpdateResourceAsync(existingResource.Id, dto, CancellationToken.None));

        Assert.Equal("book", exception.TypeKey);
        Assert.Single(exception.Errors);
        Assert.Equal("title", exception.Errors[0].FieldName);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateResourceAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.UpdateResourceAsync(resourceId, null!, CancellationToken.None));
    }

    #endregion

    #region DeleteResourceAsync Tests

    [Fact]
    public async Task DeleteResourceAsync_CallsRepository()
    {
        // Arrange
        var resourceId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.DeleteAsync(resourceId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteResourceAsync(resourceId, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(resourceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region QueryResourcesAsync Tests

    [Fact]
    public async Task QueryResourcesAsync_ValidQuery_ReturnsMappedDtos()
    {
        // Arrange
        var query = CreateSampleQueryDto(type: "book", pageNumber: 1, pageSize: 10);

        var resources = new List<Resource>
        {
            CreateSampleResource(payloadJson: @"{ ""title"": ""First"" }"),
            CreateSampleResource(payloadJson: @"{ ""title"": ""Second"" }")
        };

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resources);

        // Act
        var result = await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("book", dto.Type));
        Assert.Contains(result, dto => dto.Payload.GetProperty("title").GetString() == "First");
        Assert.Contains(result, dto => dto.Payload.GetProperty("title").GetString() == "Second");
    }

    [Fact]
    public async Task QueryResourcesAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = CreateSampleQueryDto(type: "nonexistent");

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resource>());

        // Act
        var result = await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryResourcesAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.QueryResourcesAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task QueryResourcesAsync_MapsQueryCriteriaCorrectly()
    {
        // Arrange
        var query = new ResourceQueryDto
        {
            Type = "article",
            OwnerId = "owner-123",
            PageNumber = 2,
            PageSize = 25
        };

        ResourceQueryCriteria? capturedCriteria = null;

        _repositoryMock
            .Setup(r => r.QueryAsync(It.IsAny<ResourceQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceQueryCriteria, CancellationToken>((criteria, _) => capturedCriteria = criteria)
            .ReturnsAsync(new List<Resource>());

        // Act
        await _service.QueryResourcesAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedCriteria);
        Assert.Equal("article", capturedCriteria!.Type);
        Assert.Equal("owner-123", capturedCriteria.OwnerId);
        Assert.Equal(25, capturedCriteria.Take); // pageSize
        Assert.Equal(25, capturedCriteria.Skip); // (pageNumber - 1) * pageSize = (2-1) * 25 = 25
    }

    #endregion
}
