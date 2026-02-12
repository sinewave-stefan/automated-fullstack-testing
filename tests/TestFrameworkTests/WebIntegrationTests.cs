using Game.Core.Testing;
using Game.WebApp.Testing;
using Microsoft.Playwright;
using Xunit;

namespace Game.TestFrameworkTests;

/// <summary>
/// Integration tests using real Blazor WebAssembly instances.
/// These tests verify that the fluent API works with actual browser runtime.
/// Note: These tests require Playwright to be installed and a running Blazor app.
/// </summary>
public class WebIntegrationTests : IAsyncLifetime, IDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private WebTestBridge? _bridge;
    private BlazorTestServer? _testServer;
    private string? _appUrl;

    public async Task InitializeAsync()
    {
        // Start Blazor test server
        // Get the solution root by navigating up from the test assembly location
        // Test assembly is at: tests/TestFrameworkTests/bin/Debug/net8.0/Game.TestFrameworkTests.dll
        // Need to go up 5 levels: net8.0 -> Debug -> bin -> TestFrameworkTests -> tests -> solution root
        var testAssemblyLocation = typeof(WebIntegrationTests).Assembly.Location;
        var testProjectDir = Path.GetDirectoryName(testAssemblyLocation)!;
        
        // Navigate up from bin/Debug/net8.0 to solution root (5 levels up)
        var solutionRoot = Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(solutionRoot, "Game", "WebApp", "Game.WebApp.csproj");
        projectPath = Path.GetFullPath(projectPath);
        
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Blazor project not found at: {projectPath}. " +
                $"Test assembly location: {testAssemblyLocation}, " +
                $"Solution root: {solutionRoot}");
        }
        
        _testServer = new BlazorTestServer(projectPath);
        await _testServer.StartAsync();
        
        // Initialize Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();

        // Navigate to the game page with testMode=true
        _appUrl = $"{_testServer.BaseUrl}/game?testMode=true";
    }

    [Fact]
    public async Task PlayerMovement_BlazorInstance_UpdatesPosition()
    {
        // Arrange
        if (_page == null || _appUrl == null) 
            throw new InvalidOperationException("Page or app URL not initialized");
        
        _bridge = new WebTestBridge(_page, _appUrl);
        var scenario = new TestScenario(_bridge);

        // Act
        var player = scenario.Player("TestPlayer", x: 0, y: 0, health: 100);
        scenario.Step();
        scenario.Assert.Player(player).HasPosition(0, 0);

        player.Move(10, 5);
        scenario.Step();
        
        // Assert
        scenario.Assert.Player(player)
            .HasPosition(10, 5)
            .HasHealth(100)
            .IsAlive();
    }

    [Fact]
    public async Task PlayerDamage_BlazorInstance_ReducesHealth()
    {
        // Arrange
        if (_page == null || _appUrl == null) 
            throw new InvalidOperationException("Page or app URL not initialized");
        
        _bridge = new WebTestBridge(_page, _appUrl);
        var scenario = new TestScenario(_bridge);

        // Act
        var player = scenario.Player("TestPlayer", health: 100);
        scenario.Step();
        scenario.Assert.Player(player).HasHealth(100).IsAlive();

        player.TakeDamage(30);
        scenario.Step();
        scenario.Assert.Player(player).HasHealth(70).IsAlive();
    }

    public async Task DisposeAsync()
    {
        _bridge?.Dispose();
        
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        
        _playwright?.Dispose();
        _testServer?.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
