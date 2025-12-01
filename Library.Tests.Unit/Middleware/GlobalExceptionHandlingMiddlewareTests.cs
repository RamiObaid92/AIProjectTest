using System.Net;
using System.Text.Json;
using FluentAssertions;
using Library.Application.Resources;
using Library.Application.Resources.Validation;
using Library.WebApi.Errors;
using Library.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Library.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for the <see cref="GlobalExceptionHandlingMiddleware"/> class.
/// Tests exception handling, response formatting, and logging behavior.
/// </summary>
public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _mockLogger;

    public GlobalExceptionHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GlobalExceptionHandlingMiddleware(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var act = () => new GlobalExceptionHandlingMiddleware(next, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    #endregion

    #region InvokeAsync - Success Path

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_DoesNotModifyResponse()
    {
        // Arrange
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    #endregion

    #region InvokeAsync - ResourceValidationException Handling

    [Fact]
    public async Task InvokeAsync_WhenResourceValidationExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            ValidationError.Required("title"),
            ValidationError.TypeMismatch("year", "integer")
        };
        var exception = new ResourceValidationException("book", errors);

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenResourceValidationExceptionThrown_ReturnsCorrectErrorStructure()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            ValidationError.Required("title")
        };
        var exception = new ResourceValidationException("book", errors);

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ValidationErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Status.Should().Be(400);
        response.TypeKey.Should().Be("book");
        response.Title.Should().Contain("validation errors");
        response.Errors.Should().HaveCount(1);
        response.Errors[0].Field.Should().Be("title");
        response.Errors[0].Code.Should().Be("Required");
    }

    [Fact]
    public async Task InvokeAsync_WhenResourceValidationExceptionThrown_IncludesTraceId()
    {
        // Arrange
        var exception = new ResourceValidationException("book", new[] { ValidationError.Required("title") });

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();
        context.TraceIdentifier = "test-trace-id-123";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ValidationErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response!.TraceId.Should().Be("test-trace-id-123");
    }

    [Fact]
    public async Task InvokeAsync_WhenResourceValidationExceptionWithMultipleErrors_IncludesAllErrors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            ValidationError.Required("title"),
            ValidationError.TypeMismatch("year", "integer"),
            ValidationError.MaxLengthExceeded("description", 1000)
        };
        var exception = new ResourceValidationException("book", errors);

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ValidationErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response!.Errors.Should().HaveCount(3);
        response.Errors.Select(e => e.Field).Should().Contain(new[] { "title", "year", "description" });
    }

    #endregion

    #region InvokeAsync - Unexpected Exception Handling

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionThrown_ReturnsGenericErrorMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Sensitive database error details");

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Status.Should().Be(500);
        response.Title.Should().Contain("unexpected error");
        response.Detail.Should().NotContain("Sensitive database error details");
        response.Detail.Should().Contain("internal server error");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionThrown_IncludesTraceId()
    {
        // Arrange
        var exception = new Exception("Error");

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-456";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response!.TraceId.Should().Be("trace-456");
    }

    #endregion

    #region InvokeAsync - Response Already Started

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStartedAndValidationException_RethrowsException()
    {
        // Arrange
        var exception = new ResourceValidationException("book", new[] { ValidationError.Required("title") });

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContextWithResponseStarted();

        // Act
        var act = async () => await middleware.InvokeAsync(context);

        // Assert
        await act.Should().ThrowAsync<ResourceValidationException>();
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStartedAndUnexpectedException_RethrowsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Error");

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContextWithResponseStarted();

        // Act
        var act = async () => await middleware.InvokeAsync(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public async Task InvokeAsync_ValidationError_UsesCamelCasePropertyNames()
    {
        // Arrange
        var exception = new ResourceValidationException("book", new[] { ValidationError.Required("title") });

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        responseBody.Should().Contain("\"status\":");
        responseBody.Should().Contain("\"title\":");
        responseBody.Should().Contain("\"typeKey\":");
        responseBody.Should().Contain("\"traceId\":");
        responseBody.Should().NotContain("\"Status\":");
        responseBody.Should().NotContain("\"Title\":");
    }

    [Fact]
    public async Task InvokeAsync_ApiError_UsesCamelCasePropertyNames()
    {
        // Arrange
        var exception = new Exception("Error");

        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        responseBody.Should().Contain("\"status\":");
        responseBody.Should().Contain("\"title\":");
        responseBody.Should().NotContain("\"Status\":");
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";
        return context;
    }

    private static HttpContext CreateHttpContextWithResponseStarted()
    {
        var mockResponse = new Mock<HttpResponse>();
        mockResponse.Setup(r => r.HasStarted).Returns(true);

        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockContext.Setup(c => c.TraceIdentifier).Returns("test-trace-id");

        return mockContext.Object;
    }

    #endregion
}
