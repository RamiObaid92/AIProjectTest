using System.Threading.Tasks;
using Library.Tests.Api.Fixtures;
using Xunit;

namespace Library.Tests.Api.Resources;

/// <summary>
/// Smoke tests to verify the API is reachable and responding.
/// These tests validate the basic test infrastructure setup.
/// </summary>
[Collection("ApiTests")]
public class ApiSmokeTests
{
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiSmokeTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared API test fixture.</param>
    public ApiSmokeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that the API root endpoint responds to a request.
    /// This is a basic connectivity test to ensure the API is running
    /// and the Playwright request context is properly configured.
    /// </summary>
    /// <remarks>
    /// The specific status code is not strictly validated because the root endpoint
    /// behavior may vary (200, 404, etc.). The important thing is that the server
    /// responds without throwing an exception.
    /// </remarks>
    [Fact]
    public async Task ApiRoot_Responds_Something()
    {
        // Arrange
        var request = _fixture.Request;

        // Act
        var response = await request.GetAsync("/");

        // Assert
        Assert.NotNull(response);

        // Verify we got a valid HTTP response (any status code from 100-599)
        // This confirms the server is running and reachable
        Assert.True(
            response.Status is >= 100 and < 600,
            $"Expected a valid HTTP status code, but got {response.Status}");
    }

    /// <summary>
    /// Verifies that the fixture has a valid base URL configured.
    /// </summary>
    [Fact]
    public void Fixture_HasValidBaseUrl()
    {
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(_fixture.BaseUrl));
        Assert.StartsWith("http", _fixture.BaseUrl);
    }
}
