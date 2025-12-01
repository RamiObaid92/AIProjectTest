using System.Text;
using Library.Application.TypeDescriptors;
using Library.Domain.TypeDescriptors;
using Microsoft.Extensions.Configuration;

namespace Library.Tests.Unit.TypeDescriptors;

/// <summary>
/// Extended unit tests for <see cref="ConfigTypeDescriptorRegistry"/> covering edge cases,
/// field configurations, and various descriptor scenarios.
/// </summary>
public class ConfigTypeDescriptorRegistryExtendedTests
{
    #region Configuration Builders

    private static IConfiguration BuildConfiguration(string json)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }

    #endregion

    #region Field Data Type Loading Tests

    [Theory]
    [InlineData("String", FieldDataType.String)]
    [InlineData("Int", FieldDataType.Int)]
    [InlineData("Bool", FieldDataType.Bool)]
    [InlineData("DateTime", FieldDataType.DateTime)]
    [InlineData("Decimal", FieldDataType.Decimal)]
    public void Constructor_LoadsFieldDataTypes_FromConfiguration(string dataTypeString, FieldDataType expectedType)
    {
        // Arrange
        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "field1", "dataType": "{{dataTypeString}}", "isRequired": true }
                        ]
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Single(descriptor.Fields);
        Assert.Equal(expectedType, descriptor.Fields[0].DataType);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(255)]
    [InlineData(1000)]
    [InlineData(null)]
    public void Constructor_LoadsMaxLength_FromConfiguration(int? maxLength)
    {
        // Arrange
        var maxLengthJson = maxLength.HasValue ? $", \"maxLength\": {maxLength}" : "";
        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "field1", "dataType": "String", "isRequired": false{{maxLengthJson}} }
                        ]
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Single(descriptor.Fields);
        Assert.Equal(maxLength, descriptor.Fields[0].MaxLength);
    }

    [Theory]
    [InlineData("^[A-Z]+$")]
    [InlineData("^[0-9]{5}$")]
    [InlineData("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")]
    [InlineData(null)]
    public void Constructor_LoadsPattern_FromConfiguration(string? pattern)
    {
        // Arrange
        var patternJson = pattern != null ? $", \"pattern\": \"{pattern.Replace("\\", "\\\\")}\"" : "";
        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "field1", "dataType": "String", "isRequired": false{{patternJson}} }
                        ]
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Single(descriptor.Fields);
        Assert.Equal(pattern, descriptor.Fields[0].Pattern);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_LoadsIsRequired_FromConfiguration(bool isRequired)
    {
        // Arrange
        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "field1", "dataType": "String", "isRequired": {{isRequired.ToString().ToLower()}} }
                        ]
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Single(descriptor.Fields);
        Assert.Equal(isRequired, descriptor.Fields[0].IsRequired);
    }

    #endregion

    #region Multiple Descriptors Tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_LoadsMultipleDescriptors_FromConfiguration(int count)
    {
        // Arrange
        var descriptorsJson = string.Join(",\n", Enumerable.Range(1, count).Select(i => $$"""
                    "type{{i}}": {
                        "typeKey": "type{{i}}",
                        "displayName": "Type {{i}}",
                        "schemaVersion": {{i}},
                        "fields": [
                            { "name": "field{{i}}", "dataType": "String", "isRequired": true }
                        ]
                    }
            """));

        var json = $$"""
            {
                "TypeDescriptors": {
                    {{descriptorsJson}}
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Assert
        for (int i = 1; i <= count; i++)
        {
            var descriptor = registry.GetDescriptorOrDefault($"type{i}");
            Assert.NotNull(descriptor);
            Assert.Equal($"type{i}", descriptor.TypeKey);
            Assert.Equal($"Type {i}", descriptor.DisplayName);
            Assert.Equal(i, descriptor.SchemaVersion);
        }
    }

    #endregion

    #region Schema Version Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public void Constructor_LoadsSchemaVersion_FromConfiguration(int schemaVersion)
    {
        // Arrange
        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": {{schemaVersion}},
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(schemaVersion, descriptor.SchemaVersion);
    }

    #endregion

    #region Indexing Configuration Tests

    [Fact]
    public void Constructor_LoadsFilterableFields_FromConfiguration()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "field1", "dataType": "String", "isRequired": true },
                            { "name": "field2", "dataType": "Int", "isRequired": false }
                        ],
                        "indexing": {
                            "filterableFields": ["field1", "field2"]
                        }
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.Indexing);
        Assert.Equal(2, descriptor.Indexing.FilterableFields.Count);
        Assert.Contains("field1", descriptor.Indexing.FilterableFields);
        Assert.Contains("field2", descriptor.Indexing.FilterableFields);
    }

    [Fact]
    public void Constructor_LoadsSortableFields_FromConfiguration()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [],
                        "indexing": {
                            "sortableFields": ["createdAt", "title", "priority"]
                        }
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.Indexing);
        Assert.Equal(3, descriptor.Indexing.SortableFields.Count);
        Assert.Contains("createdAt", descriptor.Indexing.SortableFields);
        Assert.Contains("title", descriptor.Indexing.SortableFields);
        Assert.Contains("priority", descriptor.Indexing.SortableFields);
    }

    [Fact]
    public void Constructor_LoadsFullTextFields_FromConfiguration()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [],
                        "indexing": {
                            "fullTextFields": ["title", "description", "content"]
                        }
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.Indexing);
        Assert.Equal(3, descriptor.Indexing.FullTextFields.Count);
        Assert.Contains("title", descriptor.Indexing.FullTextFields);
        Assert.Contains("description", descriptor.Indexing.FullTextFields);
        Assert.Contains("content", descriptor.Indexing.FullTextFields);
    }

    #endregion

    #region UiHints Configuration Tests

    [Fact]
    public void Constructor_LoadsTitleField_FromConfiguration()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [],
                        "uiHints": {
                            "titleField": "name"
                        }
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.UiHints);
        Assert.Equal("name", descriptor.UiHints.TitleField);
    }

    [Fact]
    public void Constructor_LoadsListFields_FromConfiguration()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [],
                        "uiHints": {
                            "listFields": ["title", "author", "date"]
                        }
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.UiHints);
        Assert.Equal(3, descriptor.UiHints.ListFields.Count);
        Assert.Contains("title", descriptor.UiHints.ListFields);
        Assert.Contains("author", descriptor.UiHints.ListFields);
        Assert.Contains("date", descriptor.UiHints.ListFields);
    }

    #endregion

    #region Case Sensitivity Edge Cases

    [Theory]
    [InlineData("Book", "book")]
    [InlineData("BOOK", "book")]
    [InlineData("BoOk", "book")]
    [InlineData("book", "BOOK")]
    [InlineData("article", "ARTICLE")]
    [InlineData("UserProfile", "userprofile")]
    public void GetDescriptorOrDefault_CaseVariations_ReturnsDescriptor(string configKey, string lookupKey)
    {
        // Arrange
        var json = $$"""
            {
                "TypeDescriptors": {
                    "{{configKey}}": {
                        "typeKey": "{{configKey.ToLowerInvariant()}}",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault(lookupKey);

        // Assert
        Assert.NotNull(descriptor);
    }

    #endregion

    #region Missing Optional Sections Tests

    [Fact]
    public void Constructor_NoIndexingSection_ReturnsNullIndexing()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Null(descriptor.Indexing);
    }

    [Fact]
    public void Constructor_NoUiHintsSection_ReturnsNullUiHints()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Null(descriptor.UiHints);
    }

    [Fact]
    public void Constructor_NoPolicySection_ReturnsNullPolicy()
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Null(descriptor.Policy);
    }

    #endregion

    #region Multiple Fields Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void Constructor_LoadsMultipleFields_FromConfiguration(int fieldCount)
    {
        // Arrange
        var fieldsJson = string.Join(",\n", Enumerable.Range(1, fieldCount).Select(i =>
            $$"""{ "name": "field{{i}}", "dataType": "String", "isRequired": {{(i % 2 == 0).ToString().ToLower()}} }"""));

        var json = $$"""
            {
                "TypeDescriptors": {
                    "test": {
                        "typeKey": "test",
                        "displayName": "Test",
                        "schemaVersion": 1,
                        "fields": [{{fieldsJson}}]
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("test");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(fieldCount, descriptor.Fields.Count);

        for (int i = 1; i <= fieldCount; i++)
        {
            var field = descriptor.Fields.FirstOrDefault(f => f.Name == $"field{i}");
            Assert.NotNull(field);
            Assert.Equal(i % 2 == 0, field.IsRequired);
        }
    }

    #endregion

    #region GetRequiredDescriptor Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nonexistent")]
    [InlineData("NONEXISTENT")]
    [InlineData("unknown-type-123")]
    public void GetRequiredDescriptor_InvalidOrUnknownType_ThrowsKeyNotFoundException(string typeKey)
    {
        // Arrange
        var json = """
            {
                "TypeDescriptors": {
                    "book": {
                        "typeKey": "book",
                        "displayName": "Book",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => registry.GetRequiredDescriptor(typeKey));
    }

    #endregion

    #region Display Name Tests

    [Theory]
    [InlineData("book", "Book")]
    [InlineData("article", "Article")]
    [InlineData("user_profile", "User Profile")]
    [InlineData("my-custom-type", "My Custom Type")]
    public void Constructor_LoadsDisplayName_FromConfiguration(string typeKey, string displayName)
    {
        // Arrange
        var json = $$"""
            {
                "TypeDescriptors": {
                    "{{typeKey}}": {
                        "typeKey": "{{typeKey}}",
                        "displayName": "{{displayName}}",
                        "schemaVersion": 1,
                        "fields": []
                    }
                }
            }
            """;

        var config = BuildConfiguration(json);

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault(typeKey);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(displayName, descriptor.DisplayName);
    }

    #endregion
}
