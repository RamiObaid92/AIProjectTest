using System;
using System.Collections.Generic;
using System.Text.Json;
using Library.Application.Resources.Validation;
using Library.Application.TypeDescriptors;
using Library.Domain.TypeDescriptors;
using Xunit;

namespace Library.Tests.Unit.Resources.Validation;

/// <summary>
/// Unit tests for <see cref="ResourceValidationService"/>.
/// </summary>
public class ResourceValidationServiceTests
{
    #region In-Memory Registry Stub

    /// <summary>
    /// In-memory implementation of <see cref="ITypeDescriptorRegistry"/> for testing purposes.
    /// </summary>
    private class InMemoryTypeDescriptorRegistry : ITypeDescriptorRegistry
    {
        private readonly Dictionary<string, TypeDescriptor> _descriptors;

        public InMemoryTypeDescriptorRegistry(Dictionary<string, TypeDescriptor> descriptors)
        {
            _descriptors = new Dictionary<string, TypeDescriptor>(
                descriptors ?? new Dictionary<string, TypeDescriptor>(),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public TypeDescriptor? GetDescriptorOrDefault(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
            {
                return null;
            }

            return _descriptors.TryGetValue(typeKey, out var descriptor) ? descriptor : null;
        }

        public TypeDescriptor GetRequiredDescriptor(string typeKey)
        {
            var descriptor = GetDescriptorOrDefault(typeKey);
            if (descriptor is null)
            {
                throw new KeyNotFoundException($"Type descriptor '{typeKey}' was not found.");
            }

            return descriptor;
        }
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Creates a sample TypeDescriptor for "book" with standard fields for testing.
    /// </summary>
    private static TypeDescriptor CreateBookDescriptor()
    {
        return new TypeDescriptor
        {
            TypeKey = "book",
            DisplayName = "Book",
            SchemaVersion = 1,
            Fields = new[]
            {
                new FieldDefinition
                {
                    Name = "title",
                    DataType = FieldDataType.String,
                    IsRequired = true,
                    MaxLength = 100
                },
                new FieldDefinition
                {
                    Name = "pages",
                    DataType = FieldDataType.Int,
                    IsRequired = false
                },
                new FieldDefinition
                {
                    Name = "isbn",
                    DataType = FieldDataType.String,
                    IsRequired = false,
                    Pattern = "^[0-9-]+$"
                }
            },
            Indexing = null,
            Policy = null,
            UiHints = null
        };
    }

    /// <summary>
    /// Creates a ResourceValidationService with a registry containing only the "book" descriptor.
    /// </summary>
    private static ResourceValidationService CreateServiceWithBookDescriptor()
    {
        var descriptors = new Dictionary<string, TypeDescriptor>
        {
            { "book", CreateBookDescriptor() }
        };

        var registry = new InMemoryTypeDescriptorRegistry(descriptors);
        return new ResourceValidationService(registry);
    }

    /// <summary>
    /// Creates a ResourceValidationService with an empty registry (no descriptors).
    /// </summary>
    private static ResourceValidationService CreateServiceWithEmptyRegistry()
    {
        var registry = new InMemoryTypeDescriptorRegistry(new Dictionary<string, TypeDescriptor>());
        return new ResourceValidationService(registry);
    }

    #endregion

    #region Valid Payload Tests

    [Fact]
    public void Validate_ValidPayload_ReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""pages"": 500, ""isbn"": ""123-456"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidPayloadWithOnlyRequiredFields_ReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Required Field Tests

    [Fact]
    public void Validate_MissingRequiredField_ReturnsError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""pages"": 500 }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "Required");
    }

    [Fact]
    public void Validate_NullRequiredField_ReturnsError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": null }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "Required");
    }

    #endregion

    #region Type Mismatch Tests

    [Fact]
    public void Validate_WrongType_ReturnsTypeMismatchError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""pages"": ""not-a-number"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "pages" && e.ErrorCode == "TypeMismatch");
    }

    [Fact]
    public void Validate_StringInsteadOfInt_ReturnsTypeMismatchError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""pages"": ""123"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "pages" && e.ErrorCode == "TypeMismatch");
    }

    [Fact]
    public void Validate_IntInsteadOfString_ReturnsTypeMismatchError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": 123 }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "TypeMismatch");
    }

    #endregion

    #region MaxLength Tests

    [Fact]
    public void Validate_StringExceedsMaxLength_ReturnsMaxLengthError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var longTitle = new string('a', 101);
        var json = $@"{{ ""title"": ""{longTitle}"" }}";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "MaxLength");
    }

    [Fact]
    public void Validate_StringAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var exactTitle = new string('a', 100);
        var json = $@"{{ ""title"": ""{exactTitle}"" }}";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Pattern Tests

    [Fact]
    public void Validate_StringDoesNotMatchPattern_ReturnsPatternError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""isbn"": ""ABC123"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "isbn" && e.ErrorCode == "Pattern");
    }

    [Fact]
    public void Validate_StringMatchesPattern_ReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""isbn"": ""978-0-441-17271-9"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Unknown Type Tests

    [Fact]
    public void Validate_UnknownType_ReturnsUnknownTypeError()
    {
        // Arrange
        var service = CreateServiceWithEmptyRegistry();
        var json = @"{ ""title"": ""Something"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("does-not-exist", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "UnknownType");
    }

    [Fact]
    public void Validate_UnknownTypeWithBookRegistry_ReturnsUnknownTypeError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""name"": ""Article Name"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("article", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "UnknownType");
    }

    #endregion

    #region Invalid Payload Shape Tests

    [Fact]
    public void Validate_PayloadIsArray_ReturnsInvalidPayloadShapeError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"[ ""item1"", ""item2"" ]";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "InvalidPayloadShape");
    }

    [Fact]
    public void Validate_PayloadIsString_ReturnsInvalidPayloadShapeError()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"""just a string""";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "InvalidPayloadShape");
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        // Missing title (required), pages is wrong type
        var json = @"{ ""pages"": ""not-a-number"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "Required");
        Assert.Contains(result.Errors, e => e.FieldName == "pages" && e.ErrorCode == "TypeMismatch");
    }

    #endregion

    #region Case Insensitive Type Key Tests

    [Fact]
    public void Validate_TypeKeyIsCaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"" }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("BOOK", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ResourceValidationService(null!));
    }

    #endregion

    #region Extra Fields Tests

    [Fact]
    public void Validate_ExtraFieldsInPayload_IgnoresAndReturnsSuccess()
    {
        // Arrange
        var service = CreateServiceWithBookDescriptor();
        var json = @"{ ""title"": ""Dune"", ""unknownField"": ""value"", ""anotherUnknown"": 123 }";
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement;

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion
}
