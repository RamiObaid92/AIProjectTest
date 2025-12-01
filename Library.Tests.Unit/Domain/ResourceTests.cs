using FluentAssertions;
using Library.Domain.Resources;

namespace Library.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the <see cref="Resource"/> domain entity.
/// Tests constructor validation, factory methods, and update behavior.
/// </summary>
public class ResourceTests
{
    private const string ValidType = "book";
    private const string ValidPayloadJson = """{"title":"Test Book"}""";
    private const string ValidOwnerId = "owner-123";
    private const string ValidMetadataJson = """{"tags":["fiction"]}""";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesResource()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var resource = new Resource(
            id,
            ValidType,
            ValidOwnerId,
            ValidMetadataJson,
            ValidPayloadJson,
            createdAt,
            updatedAt);

        // Assert
        resource.Id.Should().Be(id);
        resource.Type.Should().Be(ValidType);
        resource.OwnerId.Should().Be(ValidOwnerId);
        resource.MetadataJson.Should().Be(ValidMetadataJson);
        resource.PayloadJson.Should().Be(ValidPayloadJson);
        resource.CreatedAtUtc.Should().Be(createdAt);
        resource.UpdatedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void Constructor_WithNullOwnerId_CreatesResourceWithNullOwner()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var resource = new Resource(
            id,
            ValidType,
            ownerId: null,
            metadataJson: null,
            ValidPayloadJson,
            now,
            now);

        // Assert
        resource.OwnerId.Should().BeNull();
        resource.MetadataJson.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithNullOrEmptyType_ThrowsArgumentException(string? invalidType)
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var act = () => new Resource(
            id,
            invalidType!,
            ValidOwnerId,
            ValidMetadataJson,
            ValidPayloadJson,
            now,
            now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("type")
            .WithMessage("*cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithNullOrEmptyPayloadJson_ThrowsArgumentException(string? invalidPayload)
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var act = () => new Resource(
            id,
            ValidType,
            ValidOwnerId,
            ValidMetadataJson,
            invalidPayload!,
            now,
            now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("payloadJson")
            .WithMessage("*cannot be null or empty*");
    }

    #endregion

    #region CreateNew Factory Method Tests

    [Fact]
    public void CreateNew_WithValidParameters_ReturnsNewResource()
    {
        // Arrange
        var utcNow = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var resource = Resource.CreateNew(
            ValidType,
            ValidOwnerId,
            ValidMetadataJson,
            ValidPayloadJson,
            utcNow);

        // Assert
        resource.Should().NotBeNull();
        resource.Id.Should().NotBe(Guid.Empty);
        resource.Type.Should().Be(ValidType);
        resource.OwnerId.Should().Be(ValidOwnerId);
        resource.MetadataJson.Should().Be(ValidMetadataJson);
        resource.PayloadJson.Should().Be(ValidPayloadJson);
    }

    [Fact]
    public void CreateNew_SetsCreatedAtAndUpdatedAtToSameValue()
    {
        // Arrange
        var utcNow = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var resource = Resource.CreateNew(
            ValidType,
            ValidOwnerId,
            ValidMetadataJson,
            ValidPayloadJson,
            utcNow);

        // Assert
        resource.CreatedAtUtc.Should().Be(utcNow);
        resource.UpdatedAtUtc.Should().Be(utcNow);
        resource.CreatedAtUtc.Should().Be(resource.UpdatedAtUtc);
    }

    [Fact]
    public void CreateNew_GeneratesUniqueIds()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var resource1 = Resource.CreateNew(ValidType, null, null, ValidPayloadJson, utcNow);
        var resource2 = Resource.CreateNew(ValidType, null, null, ValidPayloadJson, utcNow);

        // Assert
        resource1.Id.Should().NotBe(resource2.Id);
        resource1.Id.Should().NotBe(Guid.Empty);
        resource2.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateNew_WithNullOrEmptyType_ThrowsArgumentException(string? invalidType)
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var act = () => Resource.CreateNew(
            invalidType!,
            ValidOwnerId,
            ValidMetadataJson,
            ValidPayloadJson,
            utcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("type");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateNew_WithNullOrEmptyPayloadJson_ThrowsArgumentException(string? invalidPayload)
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var act = () => Resource.CreateNew(
            ValidType,
            ValidOwnerId,
            ValidMetadataJson,
            invalidPayload!,
            utcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("payloadJson");
    }

    [Fact]
    public void CreateNew_WithNullOptionalParameters_CreatesValidResource()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var resource = Resource.CreateNew(
            ValidType,
            ownerId: null,
            metadataJson: null,
            ValidPayloadJson,
            utcNow);

        // Assert
        resource.OwnerId.Should().BeNull();
        resource.MetadataJson.Should().BeNull();
        resource.Type.Should().Be(ValidType);
        resource.PayloadJson.Should().Be(ValidPayloadJson);
    }

    #endregion

    #region UpdatePayload Tests

    [Fact]
    public void UpdatePayload_WithValidParameters_UpdatesPayloadAndTimestamp()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, createdAt);

        var newPayload = """{"title":"Updated Book","author":"New Author"}""";
        var newMetadata = """{"tags":["updated"]}""";
        var updateTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        resource.UpdatePayload(newPayload, newMetadata, updateTime);

        // Assert
        resource.PayloadJson.Should().Be(newPayload);
        resource.MetadataJson.Should().Be(newMetadata);
        resource.UpdatedAtUtc.Should().Be(updateTime);
    }

    [Fact]
    public void UpdatePayload_DoesNotChangeCreatedAtTimestamp()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, createdAt);

        var newPayload = """{"title":"Updated"}""";
        var updateTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        resource.UpdatePayload(newPayload, null, updateTime);

        // Assert
        resource.CreatedAtUtc.Should().Be(createdAt);
        resource.UpdatedAtUtc.Should().Be(updateTime);
        resource.CreatedAtUtc.Should().NotBe(resource.UpdatedAtUtc);
    }

    [Fact]
    public void UpdatePayload_DoesNotChangeTypeOrId()
    {
        // Arrange
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, DateTime.UtcNow);
        var originalId = resource.Id;
        var originalType = resource.Type;

        var newPayload = """{"title":"Updated"}""";
        var updateTime = DateTime.UtcNow.AddHours(1);

        // Act
        resource.UpdatePayload(newPayload, null, updateTime);

        // Assert
        resource.Id.Should().Be(originalId);
        resource.Type.Should().Be(originalType);
    }

    [Fact]
    public void UpdatePayload_WithNullMetadata_ClearsMetadata()
    {
        // Arrange
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, DateTime.UtcNow);
        resource.MetadataJson.Should().NotBeNull();

        var newPayload = """{"title":"Updated"}""";

        // Act
        resource.UpdatePayload(newPayload, metadataJson: null, DateTime.UtcNow);

        // Assert
        resource.MetadataJson.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UpdatePayload_WithNullOrEmptyPayloadJson_ThrowsArgumentException(string? invalidPayload)
    {
        // Arrange
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, DateTime.UtcNow);

        // Act
        var act = () => resource.UpdatePayload(invalidPayload!, ValidMetadataJson, DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("payloadJson")
            .WithMessage("*cannot be null or empty*");
    }

    #endregion

    #region SearchText Tests

    [Fact]
    public void SearchText_CanBeSetAndRetrieved()
    {
        // Arrange
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, DateTime.UtcNow);
        var searchText = "test book fiction novel";

        // Act
        resource.SearchText = searchText;

        // Assert
        resource.SearchText.Should().Be(searchText);
    }

    [Fact]
    public void SearchText_DefaultsToNull()
    {
        // Arrange & Act
        var resource = Resource.CreateNew(ValidType, ValidOwnerId, ValidMetadataJson, ValidPayloadJson, DateTime.UtcNow);

        // Assert
        resource.SearchText.Should().BeNull();
    }

    #endregion
}
