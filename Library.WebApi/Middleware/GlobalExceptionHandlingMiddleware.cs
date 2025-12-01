using System.Net;
using System.Text.Json;
using Library.Application.Resources;
using Library.WebApi.Errors;

namespace Library.WebApi.Middleware;

/// <summary>
/// Middleware that handles exceptions globally and converts them to structured JSON error responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to handle the request and catch any exceptions.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ResourceValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles ResourceValidationException by returning a 400 Bad Request with validation details.
    /// </summary>
    private async Task HandleValidationExceptionAsync(HttpContext context, ResourceValidationException ex)
    {
        _logger.LogWarning(
            "Validation failed for resource type '{TypeKey}' with {ErrorCount} errors. TraceId: {TraceId}",
            ex.TypeKey,
            ex.Errors.Count,
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot write validation error response");
            throw ex;
        }

        var response = new ValidationErrorResponse
        {
            Type = "https://httpstatuses.com/400",
            Title = "One or more validation errors occurred.",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = $"Validation failed for resource type '{ex.TypeKey}'.",
            TraceId = context.TraceIdentifier,
            TypeKey = ex.TypeKey,
            Errors = ex.Errors.Select(e => new ValidationFieldError
            {
                Field = e.FieldName,
                Code = e.ErrorCode,
                Message = e.Message
            }).ToList()
        };

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Handles unexpected exceptions by returning a 500 Internal Server Error.
    /// </summary>
    private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(
            ex,
            "An unexpected error occurred. TraceId: {TraceId}",
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot write error response");
            throw ex;
        }

        var response = new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/500",
            Title = "An unexpected error occurred.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "An internal server error occurred. Please try again later.",
            TraceId = context.TraceIdentifier
        };

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
