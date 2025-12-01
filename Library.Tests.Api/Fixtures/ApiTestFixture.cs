using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace Library.Tests.Api.Fixtures;

/// <summary>
/// Provides a shared Playwright APIRequestContext for API integration tests.
/// This fixture manages the lifecycle of a Playwright instance and an HTTP client
/// configured to communicate with the Library.WebApi.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Important:</strong> Tests expect the API to be running before <c>dotnet test</c> is executed.
/// The test fixture does not start the API automatically.
/// </para>
/// <para>
/// The base URL can be configured via the <c>LIBRARY_API_BASE_URL</c> environment variable.
/// If not set, defaults to <c>http://localhost:5000</c>.
/// </para>
/// <para>
/// To match your local development setup, check your <c>launchSettings.json</c> or Kestrel
/// configuration and set the environment variable accordingly (e.g., <c>http://localhost:5062</c>).
/// </para>
/// </remarks>
public class ApiTestFixture : IAsyncLifetime
{
    private const string DefaultBaseUrl = "http://localhost:5000";
    private const string BaseUrlEnvironmentVariable = "LIBRARY_API_BASE_URL";

    /// <summary>
    /// Gets the Playwright instance used by this fixture.
    /// </summary>
    public IPlaywright Playwright { get; private set; } = null!;

    /// <summary>
    /// Gets the API request context for making HTTP requests to the API.
    /// </summary>
    public IAPIRequestContext Request { get; private set; } = null!;

    /// <summary>
    /// Gets the base URL of the API under test.
    /// </summary>
    /// <remarks>
    /// This value is read from the <c>LIBRARY_API_BASE_URL</c> environment variable.
    /// If not set, defaults to <c>http://localhost:5000</c>.
    /// Developers should ensure this matches their local API configuration.
    /// </remarks>
    public string BaseUrl { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTestFixture"/> class.
    /// </summary>
    public ApiTestFixture()
    {
        BaseUrl = Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable) ?? DefaultBaseUrl;
    }

    /// <summary>
    /// Initializes the Playwright instance and creates an API request context.
    /// </summary>
    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Request = await Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = BaseUrl
        });
    }

    /// <summary>
    /// Disposes the API request context and Playwright instance.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (Request is not null)
        {
            await Request.DisposeAsync();
        }

        Playwright?.Dispose();
    }
}

/// <summary>
/// xUnit collection definition for API tests.
/// Tests decorated with <c>[Collection("ApiTests")]</c> will share the same <see cref="ApiTestFixture"/> instance.
/// </summary>
/// <remarks>
/// This class has no code and is never instantiated. Its purpose is to apply
/// the <see cref="CollectionDefinitionAttribute"/> and <see cref="ICollectionFixture{TFixture}"/>
/// to define the shared test context.
/// </remarks>
[CollectionDefinition("ApiTests")]
public class ApiTestsCollection : ICollectionFixture<ApiTestFixture>
{
}
