using System;
using System.Collections.Generic;
using System.Text.Json;
using Library.Application.Resources.Validation;
using Library.Application.TypeDescriptors;
using Library.Domain.TypeDescriptors;
using Xunit;

namespace Library.Tests.Unit.Resources.Validation;

/// <summary>
/// Extended unit tests for <see cref="ResourceValidationService"/> using Theory-based tests
/// for comprehensive coverage of validation scenarios.
/// </summary>
public class ResourceValidationServiceExtendedTests
{
    #region In-Memory Registry Stub

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
            if (string.IsNullOrEmpty(typeKey)) return null;
            return _descriptors.TryGetValue(typeKey, out var descriptor) ? descriptor : null;
        }

        public TypeDescriptor GetRequiredDescriptor(string typeKey)
        {
            var descriptor = GetDescriptorOrDefault(typeKey);
            return descriptor ?? throw new KeyNotFoundException($"Type descriptor '{typeKey}' was not found.");
        }
    }

    #endregion

    #region Test Helpers

    private static ResourceValidationService CreateService(params (string key, TypeDescriptor descriptor)[] descriptors)
    {
        var dict = new Dictionary<string, TypeDescriptor>();
        foreach (var (key, descriptor) in descriptors)
        {
            dict[key] = descriptor;
        }
        return new ResourceValidationService(new InMemoryTypeDescriptorRegistry(dict));
    }

    private static TypeDescriptor CreateDescriptor(string typeKey, params FieldDefinition[] fields)
    {
        return new TypeDescriptor
        {
            TypeKey = typeKey,
            DisplayName = typeKey,
            SchemaVersion = 1,
            Fields = fields
        };
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    #endregion

    #region Required Field Validation Tests

    [Theory]
    [InlineData("title")]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("content")]
    [InlineData("author")]
    public void Validate_MissingRequiredField_ReturnsErrorForField(string fieldName)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = fieldName, DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == fieldName && e.ErrorCode == "Required");
    }

    [Fact]
    public void Validate_RequiredStringField_Null_ReturnsError()
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{ \"title\": null }");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "Required");
    }

    [Fact]
    public void Validate_RequiredStringField_EmptyString_ReturnsSuccess()
    {
        // Note: Empty strings are valid string values; they satisfy the "required" constraint.
        // If empty strings should not be allowed, use a pattern or minLength constraint.
        
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{ \"title\": \"\" }");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("test")]
    [InlineData("A longer string value")]
    [InlineData("Special chars: !@#$%^&*()")]
    public void Validate_RequiredStringField_WithValue_ReturnsSuccess(string value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"title\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Validate_MultipleRequiredFieldsMissing_ReturnsAllErrors(int missingCount)
    {
        // Arrange
        var fields = Enumerable.Range(1, missingCount)
            .Select(i => new FieldDefinition { Name = $"field{i}", DataType = FieldDataType.String, IsRequired = true })
            .ToArray();

        var descriptor = CreateDescriptor("test", fields);
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(missingCount, result.Errors.Count);
        for (int i = 1; i <= missingCount; i++)
        {
            Assert.Contains(result.Errors, e => e.FieldName == $"field{i}" && e.ErrorCode == "Required");
        }
    }

    #endregion

    #region Type Mismatch Validation Tests

    [Theory]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("12.34")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Validate_StringField_WithNonStringValue_ReturnsTypeMismatch(string jsonValue)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "name", DataType = FieldDataType.String, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"name\": {jsonValue} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "name" && e.ErrorCode == "TypeMismatch");
    }

    [Theory]
    [InlineData("\"text\"")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("12.5")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Validate_IntField_WithNonIntValue_ReturnsTypeMismatch(string jsonValue)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "count", DataType = FieldDataType.Int, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"count\": {jsonValue} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "count" && e.ErrorCode == "TypeMismatch");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(-999)]
    [InlineData(2147483647)]
    public void Validate_IntField_WithValidInt_ReturnsSuccess(int value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "count", DataType = FieldDataType.Int, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"count\": {value} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("\"text\"")]
    [InlineData("123")]
    [InlineData("12.5")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Validate_BoolField_WithNonBoolValue_ReturnsTypeMismatch(string jsonValue)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "active", DataType = FieldDataType.Bool, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"active\": {jsonValue} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "active" && e.ErrorCode == "TypeMismatch");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_BoolField_WithValidBool_ReturnsSuccess(bool value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "active", DataType = FieldDataType.Bool, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"active\": {value.ToString().ToLower()} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region MaxLength Validation Tests

    [Theory]
    [InlineData(5, 6)]
    [InlineData(10, 11)]
    [InlineData(100, 101)]
    [InlineData(255, 256)]
    public void Validate_StringExceedsMaxLength_ReturnsMaxLengthError(int maxLength, int actualLength)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "text", DataType = FieldDataType.String, IsRequired = false, MaxLength = maxLength });
        var service = CreateService(("test", descriptor));
        var value = new string('x', actualLength);
        var payload = ParseJson($"{{ \"text\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "text" && e.ErrorCode == "MaxLength");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(10, 1)]
    [InlineData(100, 50)]
    public void Validate_StringWithinMaxLength_ReturnsSuccess(int maxLength, int actualLength)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "text", DataType = FieldDataType.String, IsRequired = false, MaxLength = maxLength });
        var service = CreateService(("test", descriptor));
        var value = new string('x', actualLength);
        var payload = ParseJson($"{{ \"text\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyString_PassesMaxLengthValidation()
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "text", DataType = FieldDataType.String, IsRequired = false, MaxLength = 10 });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{ \"text\": \"\" }");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Pattern Validation Tests

    [Theory]
    [InlineData("^[A-Z]+$", "abc")]
    [InlineData("^[A-Z]+$", "123")]
    [InlineData("^[0-9]{5}$", "1234")]
    [InlineData("^[0-9]{5}$", "123456")]
    [InlineData("^[0-9]{5}$", "abcde")]
    public void Validate_StringDoesNotMatchPattern_ReturnsPatternError(string pattern, string value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "code", DataType = FieldDataType.String, IsRequired = false, Pattern = pattern });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"code\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "code" && e.ErrorCode == "Pattern");
    }

    [Theory]
    [InlineData("^[A-Z]+$", "ABC")]
    [InlineData("^[A-Z]+$", "XYZ")]
    [InlineData("^[0-9]{5}$", "12345")]
    [InlineData("^[0-9]{5}$", "00000")]
    [InlineData("^[a-z0-9]+$", "abc123")]
    public void Validate_StringMatchesPattern_ReturnsSuccess(string pattern, string value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "code", DataType = FieldDataType.String, IsRequired = false, Pattern = pattern });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"code\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("^[0-9-]+$", "123-456-789")]
    [InlineData("^[0-9-]+$", "978-0-441-17271-9")]
    public void Validate_IsbnPattern_ValidIsbn_ReturnsSuccess(string pattern, string value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "isbn", DataType = FieldDataType.String, IsRequired = false, Pattern = pattern });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"isbn\": \"{value}\" }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Unknown Type Tests

    [Theory]
    [InlineData("unknown")]
    [InlineData("does-not-exist")]
    [InlineData("INVALID")]
    [InlineData("random-type-123")]
    [InlineData("")]
    public void Validate_UnknownType_ReturnsUnknownTypeError(string typeKey)
    {
        // Arrange
        var descriptor = CreateDescriptor("book", new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("book", descriptor));
        var payload = ParseJson("{ \"title\": \"Test\" }");

        // Act
        var result = service.Validate(typeKey, payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "UnknownType");
    }

    #endregion

    #region Payload Shape Validation Tests

    [Theory]
    [InlineData("[]")]
    [InlineData("[1, 2, 3]")]
    [InlineData("[\"a\", \"b\"]")]
    [InlineData("[[]]")]
    public void Validate_PayloadIsArray_ReturnsInvalidPayloadShape(string json)
    {
        // Arrange
        var descriptor = CreateDescriptor("test", new FieldDefinition { Name = "field", DataType = FieldDataType.String, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson(json);

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "InvalidPayloadShape");
    }

    [Theory]
    [InlineData("\"string\"")]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("12.34")]
    public void Validate_PayloadIsNotObject_ReturnsInvalidPayloadShape(string json)
    {
        // Arrange
        var descriptor = CreateDescriptor("test", new FieldDefinition { Name = "field", DataType = FieldDataType.String, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson(json);

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "InvalidPayloadShape");
    }

    #endregion

    #region Optional Fields Tests

    [Fact]
    public void Validate_OptionalFieldMissing_ReturnsSuccess()
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "optional", DataType = FieldDataType.String, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_OptionalFieldNull_ReturnsSuccess()
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "optional", DataType = FieldDataType.String, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{ \"optional\": null }");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Validate_AllOptionalFieldsMissing_ReturnsSuccess(int fieldCount)
    {
        // Arrange
        var fields = Enumerable.Range(1, fieldCount)
            .Select(i => new FieldDefinition { Name = $"opt{i}", DataType = FieldDataType.String, IsRequired = false })
            .ToArray();
        var descriptor = CreateDescriptor("test", fields);
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Extra Fields Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_ExtraFieldsInPayload_IgnoresAndReturnsSuccess(int extraFieldCount)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "known", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService(("test", descriptor));

        var extraFields = string.Join(", ", Enumerable.Range(1, extraFieldCount)
            .Select(i => $"\"extra{i}\": \"value{i}\""));
        var payload = ParseJson($"{{ \"known\": \"value\", {extraFields} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Combined Validation Tests

    [Fact]
    public void Validate_MixedRequiredAndOptionalFields_ValidPayload_ReturnsSuccess()
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "required1", DataType = FieldDataType.String, IsRequired = true },
            new FieldDefinition { Name = "required2", DataType = FieldDataType.Int, IsRequired = true },
            new FieldDefinition { Name = "optional1", DataType = FieldDataType.String, IsRequired = false },
            new FieldDefinition { Name = "optional2", DataType = FieldDataType.Bool, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson("{ \"required1\": \"value\", \"required2\": 42 }");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComplexPayload_AllFieldsValid_ReturnsSuccess()
    {
        // Arrange
        var descriptor = CreateDescriptor("book",
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true, MaxLength = 100 },
            new FieldDefinition { Name = "author", DataType = FieldDataType.String, IsRequired = true },
            new FieldDefinition { Name = "pages", DataType = FieldDataType.Int, IsRequired = false },
            new FieldDefinition { Name = "published", DataType = FieldDataType.Bool, IsRequired = false },
            new FieldDefinition { Name = "isbn", DataType = FieldDataType.String, IsRequired = false, Pattern = "^[0-9-]+$" });
        var service = CreateService(("book", descriptor));
        var payload = ParseJson(@"{ 
            ""title"": ""Dune"", 
            ""author"": ""Frank Herbert"", 
            ""pages"": 412, 
            ""published"": true,
            ""isbn"": ""978-0-441-17271-9""
        }");

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComplexPayload_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var descriptor = CreateDescriptor("book",
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true, MaxLength = 10 },
            new FieldDefinition { Name = "pages", DataType = FieldDataType.Int, IsRequired = true },
            new FieldDefinition { Name = "isbn", DataType = FieldDataType.String, IsRequired = false, Pattern = "^[0-9-]+$" });
        var service = CreateService(("book", descriptor));
        var payload = ParseJson(@"{ 
            ""title"": ""This title is way too long"",
            ""pages"": ""not a number"",
            ""isbn"": ""ABC123""
        }");

        // Act
        var result = service.Validate("book", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
        Assert.Contains(result.Errors, e => e.FieldName == "title" && e.ErrorCode == "MaxLength");
        Assert.Contains(result.Errors, e => e.FieldName == "pages" && e.ErrorCode == "TypeMismatch");
        Assert.Contains(result.Errors, e => e.FieldName == "isbn" && e.ErrorCode == "Pattern");
    }

    #endregion

    #region Type Key Case Sensitivity Tests

    [Theory]
    [InlineData("book", "BOOK")]
    [InlineData("Book", "book")]
    [InlineData("ARTICLE", "article")]
    [InlineData("UserProfile", "USERPROFILE")]
    public void Validate_TypeKeyCaseInsensitive_ReturnsSuccess(string registeredKey, string lookupKey)
    {
        // Arrange
        var descriptor = CreateDescriptor(registeredKey,
            new FieldDefinition { Name = "title", DataType = FieldDataType.String, IsRequired = true });
        var service = CreateService((registeredKey, descriptor));
        var payload = ParseJson("{ \"title\": \"Test\" }");

        // Act
        var result = service.Validate(lookupKey, payload);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Decimal Field Validation Tests

    [Theory]
    [InlineData("12.34")]
    [InlineData("0.5")]
    [InlineData("-10.99")]
    [InlineData("100")]
    [InlineData("0")]
    public void Validate_DecimalField_WithValidNumber_ReturnsSuccess(string value)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "price", DataType = FieldDataType.Decimal, IsRequired = true });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"price\": {value} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("\"text\"")]
    [InlineData("true")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Validate_DecimalField_WithNonNumericValue_ReturnsTypeMismatch(string jsonValue)
    {
        // Arrange
        var descriptor = CreateDescriptor("test",
            new FieldDefinition { Name = "price", DataType = FieldDataType.Decimal, IsRequired = false });
        var service = CreateService(("test", descriptor));
        var payload = ParseJson($"{{ \"price\": {jsonValue} }}");

        // Act
        var result = service.Validate("test", payload);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "price" && e.ErrorCode == "TypeMismatch");
    }

    #endregion
}
