using System.Text;
using Library.Application.TypeDescriptors;
using Library.Domain.TypeDescriptors;
using Microsoft.Extensions.Configuration;

namespace Library.Tests.Unit.TypeDescriptors;

/// <summary>
/// Unit tests for <see cref="ConfigTypeDescriptorRegistry"/>.
/// </summary>
public class ConfigTypeDescriptorRegistryTests
{
    /// <summary>
    /// Builds an in-memory IConfiguration with sample type descriptors for testing.
    /// </summary>
    private static IConfiguration BuildConfigurationWithSampleDescriptors()
    {
        var json = """
            {
                "TypeDescriptors": {
                    "book": {
                        "typeKey": "book",
                        "displayName": "Book",
                        "schemaVersion": 1,
                        "fields": [
                            { "name": "title", "dataType": "String", "isRequired": true, "maxLength": 300 },
                            { "name": "author", "dataType": "String", "isRequired": true },
                            { "name": "publishedYear", "dataType": "Int", "isRequired": false }
                        ],
                        "indexing": {
                            "filterableFields": ["author", "publishedYear"],
                            "sortableFields": ["publishedYear", "title"],
                            "fullTextFields": ["title", "author"]
                        }
                    },
                    "article": {
                        "typeKey": "article",
                        "displayName": "Article",
                        "schemaVersion": 2,
                        "fields": [
                            { "name": "title", "dataType": "String", "isRequired": true },
                            { "name": "url", "dataType": "String", "isRequired": true, "maxLength": 500 }
                        ]
                    }
                }
            }
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        return config;
    }

    /// <summary>
    /// Builds an empty IConfiguration with no type descriptors.
    /// </summary>
    private static IConfiguration BuildEmptyConfiguration()
    {
        var json = """
            {
                "TypeDescriptors": {}
            }
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        return config;
    }

    [Fact]
    public void Constructor_LoadsDescriptors_FromConfiguration()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var bookDescriptor = registry.GetDescriptorOrDefault("book");
        var articleDescriptor = registry.GetDescriptorOrDefault("article");

        // Assert - Book descriptor
        Assert.NotNull(bookDescriptor);
        Assert.Equal("book", bookDescriptor.TypeKey);
        Assert.Equal("Book", bookDescriptor.DisplayName);
        Assert.Equal(1, bookDescriptor.SchemaVersion);
        Assert.NotNull(bookDescriptor.Fields);
        Assert.Equal(3, bookDescriptor.Fields.Count);

        var titleField = bookDescriptor.Fields.FirstOrDefault(f => f.Name == "title");
        Assert.NotNull(titleField);
        Assert.Equal(FieldDataType.String, titleField.DataType);
        Assert.True(titleField.IsRequired);
        Assert.Equal(300, titleField.MaxLength);

        // Assert - Indexing loaded correctly
        Assert.NotNull(bookDescriptor.Indexing);
        Assert.Contains("author", bookDescriptor.Indexing.FilterableFields);
        Assert.Contains("publishedYear", bookDescriptor.Indexing.SortableFields);

        // Assert - Article descriptor
        Assert.NotNull(articleDescriptor);
        Assert.Equal("article", articleDescriptor.TypeKey);
        Assert.Equal("Article", articleDescriptor.DisplayName);
        Assert.Equal(2, articleDescriptor.SchemaVersion);
        Assert.NotNull(articleDescriptor.Fields);
        Assert.Equal(2, articleDescriptor.Fields.Count);
    }

    [Fact]
    public void GetDescriptorOrDefault_UnknownType_ReturnsNull()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act
        var descriptor = registry.GetDescriptorOrDefault("does-not-exist");

        // Assert
        Assert.Null(descriptor);
    }

    [Fact]
    public void GetDescriptorOrDefault_NullOrEmptyTypeKey_ReturnsNull()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act
        var nullResult = registry.GetDescriptorOrDefault(null!);
        var emptyResult = registry.GetDescriptorOrDefault("");
        var whitespaceResult = registry.GetDescriptorOrDefault("   ");

        // Assert
        Assert.Null(nullResult);
        Assert.Null(emptyResult);
        Assert.Null(whitespaceResult);
    }

    [Fact]
    public void GetDescriptorOrDefault_IsCaseInsensitive()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act
        var descriptorLower = registry.GetDescriptorOrDefault("book");
        var descriptorUpper = registry.GetDescriptorOrDefault("BOOK");
        var descriptorMixed = registry.GetDescriptorOrDefault("BoOk");

        // Assert
        Assert.NotNull(descriptorLower);
        Assert.NotNull(descriptorUpper);
        Assert.NotNull(descriptorMixed);

        // All should refer to the same type
        Assert.Equal("book", descriptorLower.TypeKey);
        Assert.Equal("book", descriptorUpper.TypeKey);
        Assert.Equal("book", descriptorMixed.TypeKey);
    }

    [Fact]
    public void GetRequiredDescriptor_KnownType_ReturnsDescriptor()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act
        var descriptor = registry.GetRequiredDescriptor("book");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("book", descriptor.TypeKey);
        Assert.Equal("Book", descriptor.DisplayName);
    }

    [Fact]
    public void GetRequiredDescriptor_UnknownType_ThrowsKeyNotFoundException()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(
            () => registry.GetRequiredDescriptor("unknown-type"));

        Assert.Contains("unknown-type", exception.Message);
    }

    [Fact]
    public void GetRequiredDescriptor_IsCaseInsensitive()
    {
        // Arrange
        var config = BuildConfigurationWithSampleDescriptors();
        var registry = new ConfigTypeDescriptorRegistry(config);

        // Act
        var descriptorUpper = registry.GetRequiredDescriptor("ARTICLE");
        var descriptorLower = registry.GetRequiredDescriptor("article");

        // Assert
        Assert.NotNull(descriptorUpper);
        Assert.NotNull(descriptorLower);
        Assert.Equal("article", descriptorUpper.TypeKey);
        Assert.Equal("article", descriptorLower.TypeKey);
    }

    [Fact]
    public void Constructor_EmptyConfiguration_CreatesEmptyRegistry()
    {
        // Arrange
        var config = BuildEmptyConfiguration();

        // Act
        var registry = new ConfigTypeDescriptorRegistry(config);
        var descriptor = registry.GetDescriptorOrDefault("anything");

        // Assert
        Assert.Null(descriptor);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConfigTypeDescriptorRegistry(null!));
    }
}
