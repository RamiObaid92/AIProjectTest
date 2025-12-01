using Library.Application.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

/// <summary>
/// API controller for managing generic resources.
/// Provides CRUD operations and querying capabilities for resources of any type.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ResourcesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourcesController"/> class.
    /// </summary>
    /// <param name="resourceService">The resource service for business operations.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public ResourcesController(IResourceService resourceService, ILogger<ResourcesController> logger)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    /// <param name="dto">The resource creation data including type, payload, and optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The created resource.</returns>
    /// <response code="201">Returns the newly created resource.</response>
    /// <response code="400">If the request is invalid or validation fails.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResourceDto>> CreateResource(
        [FromBody] ResourceCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest("Request body is required.");
        }

        var created = await _resourceService.CreateResourceAsync(dto, cancellationToken);

        _logger.LogInformation(
            "Created resource {ResourceId} of type '{Type}'",
            created.Id,
            created.Type);

        return CreatedAtAction(nameof(GetResourceById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Gets a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The resource if found.</returns>
    /// <response code="200">Returns the resource.</response>
    /// <response code="404">If the resource is not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceDto>> GetResourceById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetResourceByIdAsync(id, cancellationToken);

        if (resource is null)
        {
            _logger.LogDebug("Resource {ResourceId} not found", id);
            return NotFound();
        }

        return Ok(resource);
    }

    /// <summary>
    /// Updates an existing resource.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="dto">The update data including new payload and optional metadata.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The updated resource.</returns>
    /// <response code="200">Returns the updated resource.</response>
    /// <response code="400">If the request is invalid or validation fails.</response>
    /// <response code="404">If the resource is not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceDto>> UpdateResource(
        Guid id,
        [FromBody] ResourceUpdateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest("Request body is required.");
        }

        var updated = await _resourceService.UpdateResourceAsync(id, dto, cancellationToken);

        if (updated is null)
        {
            _logger.LogDebug("Resource {ResourceId} not found for update", id);
            return NotFound();
        }

        _logger.LogInformation("Updated resource {ResourceId}", id);

        return Ok(updated);
    }

    /// <summary>
    /// Deletes a resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to delete.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">The resource was deleted or did not exist.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteResource(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delete requested for resource {ResourceId}", id);

        await _resourceService.DeleteResourceAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Queries resources based on filter criteria.
    /// </summary>
    /// <param name="query">The query parameters for filtering, sorting, and pagination.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A list of resources matching the query criteria.</returns>
    /// <response code="200">Returns the list of resources.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetResources(
        [FromQuery] ResourceQueryDto query,
        CancellationToken cancellationToken)
    {
        // Ensure query is not null (ASP.NET Core should create instance, but be safe)
        query ??= new ResourceQueryDto();

        var resources = await _resourceService.QueryResourcesAsync(query, cancellationToken);

        _logger.LogDebug(
            "Query returned {Count} resources for type '{Type}'",
            resources.Count,
            query.Type ?? "(all)");

        return Ok(resources);
    }
}
