using System.Text.Json;
using FluentAssertions;
using Library.Application.Resources;
using Library.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Library.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for the <see cref="ResourcesController"/> class.
/// Tests controller actions, parameter validation, and HTTP response behavior.
/// </summary>
public class ResourcesControllerTests
{
    private readonly Mock<IResourceService> _mockResourceService;
    private readonly Mock<ILogger<ResourcesController>> _mockLogger;
    private readonly ResourcesController _controller;

    public ResourcesControllerTests()
    {
        _mockResourceService = new Mock<IResourceService>();
        _mockLogger = new Mock<ILogger<ResourcesController>>();
        _controller = new ResourcesController(_mockResourceService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullResourceService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ResourcesController(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("resourceService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ResourcesController(_mockResourceService.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var controller = new ResourcesController(_mockResourceService.Object, _mockLogger.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion

    #region CreateResource Tests

    [Fact]
    public async Task CreateResource_WithNullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateResource(null!, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task CreateResource_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = CreateValidResourceCreateDto();
        var expectedDto = CreateResourceDto(Guid.NewGuid(), "book");

        _mockResourceService
            .Setup(s => s.CreateResourceAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.CreateResource(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(ResourcesController.GetResourceById));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expectedDto.Id);
        createdResult.Value.Should().Be(expectedDto);
    }

    [Fact]
    public async Task CreateResource_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var createDto = CreateValidResourceCreateDto();
        var expectedDto = CreateResourceDto(Guid.NewGuid(), "book");
        var cancellationToken = new CancellationToken();

        _mockResourceService
            .Setup(s => s.CreateResourceAsync(createDto, cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        await _controller.CreateResource(createDto, cancellationToken);

        // Assert
        _mockResourceService.Verify(
            s => s.CreateResourceAsync(createDto, cancellationToken),
            Times.Once);
    }

    #endregion

    #region GetResourceById Tests

    [Fact]
    public async Task GetResourceById_WhenResourceExists_ReturnsOkWithResource()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var expectedDto = CreateResourceDto(resourceId, "book");

        _mockResourceService
            .Setup(s => s.GetResourceByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetResourceById(resourceId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetResourceById_WhenResourceDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var resourceId = Guid.NewGuid();

        _mockResourceService
            .Setup(s => s.GetResourceByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceDto?)null);

        // Act
        var result = await _controller.GetResourceById(resourceId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetResourceById_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockResourceService
            .Setup(s => s.GetResourceByIdAsync(resourceId, cancellationToken))
            .ReturnsAsync(CreateResourceDto(resourceId, "book"));

        // Act
        await _controller.GetResourceById(resourceId, cancellationToken);

        // Assert
        _mockResourceService.Verify(
            s => s.GetResourceByIdAsync(resourceId, cancellationToken),
            Times.Once);
    }

    #endregion

    #region UpdateResource Tests

    [Fact]
    public async Task UpdateResource_WithNullDto_ReturnsBadRequest()
    {
        // Arrange
        var resourceId = Guid.NewGuid();

        // Act
        var result = await _controller.UpdateResource(resourceId, null!, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task UpdateResource_WhenResourceExists_ReturnsOkWithUpdatedResource()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var updateDto = CreateValidResourceUpdateDto();
        var expectedDto = CreateResourceDto(resourceId, "book");

        _mockResourceService
            .Setup(s => s.UpdateResourceAsync(resourceId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.UpdateResource(resourceId, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedDto);
    }

    [Fact]
    public async Task UpdateResource_WhenResourceDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var updateDto = CreateValidResourceUpdateDto();

        _mockResourceService
            .Setup(s => s.UpdateResourceAsync(resourceId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceDto?)null);

        // Act
        var result = await _controller.UpdateResource(resourceId, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateResource_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var updateDto = CreateValidResourceUpdateDto();
        var cancellationToken = new CancellationToken();

        _mockResourceService
            .Setup(s => s.UpdateResourceAsync(resourceId, updateDto, cancellationToken))
            .ReturnsAsync(CreateResourceDto(resourceId, "book"));

        // Act
        await _controller.UpdateResource(resourceId, updateDto, cancellationToken);

        // Assert
        _mockResourceService.Verify(
            s => s.UpdateResourceAsync(resourceId, updateDto, cancellationToken),
            Times.Once);
    }

    #endregion

    #region DeleteResource Tests

    [Fact]
    public async Task DeleteResource_ReturnsNoContent()
    {
        // Arrange
        var resourceId = Guid.NewGuid();

        _mockResourceService
            .Setup(s => s.DeleteResourceAsync(resourceId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteResource(resourceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteResource_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _mockResourceService
            .Setup(s => s.DeleteResourceAsync(resourceId, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.DeleteResource(resourceId, cancellationToken);

        // Assert
        _mockResourceService.Verify(
            s => s.DeleteResourceAsync(resourceId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task DeleteResource_WhenResourceDoesNotExist_StillReturnsNoContent()
    {
        // Arrange - Delete is idempotent, should not throw if resource doesn't exist
        var resourceId = Guid.NewGuid();

        _mockResourceService
            .Setup(s => s.DeleteResourceAsync(resourceId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteResource(resourceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region GetResources Tests

    [Fact]
    public async Task GetResources_WithNullQuery_UsesDefaultQuery()
    {
        // Arrange
        var expectedResources = new List<ResourceDto>
        {
            CreateResourceDto(Guid.NewGuid(), "book"),
            CreateResourceDto(Guid.NewGuid(), "article")
        };

        _mockResourceService
            .Setup(s => s.QueryResourcesAsync(It.IsAny<ResourceQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResources);

        // Act
        var result = await _controller.GetResources(null!, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResources);
    }

    [Fact]
    public async Task GetResources_WithValidQuery_ReturnsOkWithResources()
    {
        // Arrange
        var query = new ResourceQueryDto { Type = "book", PageNumber = 1, PageSize = 10 };
        var expectedResources = new List<ResourceDto>
        {
            CreateResourceDto(Guid.NewGuid(), "book")
        };

        _mockResourceService
            .Setup(s => s.QueryResourcesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResources);

        // Act
        var result = await _controller.GetResources(query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResources);
    }

    [Fact]
    public async Task GetResources_WhenNoResourcesFound_ReturnsEmptyList()
    {
        // Arrange
        var query = new ResourceQueryDto { Type = "nonexistent" };
        var emptyList = new List<ResourceDto>();

        _mockResourceService
            .Setup(s => s.QueryResourcesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _controller.GetResources(query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var resources = okResult.Value as IReadOnlyList<ResourceDto>;
        resources.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResources_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var query = new ResourceQueryDto { Type = "book", OwnerId = "owner-1" };
        var cancellationToken = new CancellationToken();

        _mockResourceService
            .Setup(s => s.QueryResourcesAsync(query, cancellationToken))
            .ReturnsAsync(new List<ResourceDto>());

        // Act
        await _controller.GetResources(query, cancellationToken);

        // Assert
        _mockResourceService.Verify(
            s => s.QueryResourcesAsync(query, cancellationToken),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static ResourceCreateDto CreateValidResourceCreateDto()
    {
        return new ResourceCreateDto
        {
            Type = "book",
            OwnerId = "owner-123",
            Payload = JsonDocument.Parse("""{"title":"Test Book","author":"Test Author"}""").RootElement
        };
    }

    private static ResourceUpdateDto CreateValidResourceUpdateDto()
    {
        return new ResourceUpdateDto
        {
            Payload = JsonDocument.Parse("""{"title":"Updated Book","author":"Updated Author"}""").RootElement
        };
    }

    private static ResourceDto CreateResourceDto(Guid id, string type)
    {
        return new ResourceDto
        {
            Id = id,
            Type = type,
            OwnerId = "owner-123",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Payload = JsonDocument.Parse("""{"title":"Test Book"}""").RootElement
        };
    }

    #endregion
}
