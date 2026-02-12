using Game.Core.Testing;
using Game.StrideApp.Testing;
using Game.WebApp.Testing;
using Microsoft.Playwright;
using Xunit;

namespace Game.TestFrameworkTests;

/// <summary>
/// Unified integration tests that run against both Stride and Web (Blazor) platforms.
/// These tests verify that the fluent API works consistently across all platforms.
/// </summary>
public class IntegrationTests : IAsyncLifetime, IDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private BlazorTestServer? _testServer;
    private string? _appUrl;
    private readonly List<IDisposable> _disposables = new();

    public async Task InitializeAsync()
    {
        // Initialize Playwright and Blazor test server for Web tests
        var testAssemblyLocation = typeof(IntegrationTests).Assembly.Location;
        var testProjectDir = Path.GetDirectoryName(testAssemblyLocation)!;
        var solutionRoot = Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(solutionRoot, "Game", "WebApp", "Game.WebApp.csproj");
        projectPath = Path.GetFullPath(projectPath);
        
        if (File.Exists(projectPath))
        {
            _testServer = new BlazorTestServer(projectPath);
            await _testServer.StartAsync();
            
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _page = await _browser.NewPageAsync();
            _appUrl = $"{_testServer.BaseUrl}/game?testMode=true";
        }
    }

    public static IEnumerable<object[]> TestBridges()
    {
        yield return new object[] { "Stride" };
        yield return new object[] { "Web" };
    }

    private ITestBridge CreateBridge(string platform)
    {
        return platform switch
        {
            "Stride" => StrideTestBridge.CreateTestInstance(),
            "Web" => CreateWebBridge(),
            _ => throw new NotSupportedException($"Platform {platform} is not supported")
        };
    }

    private ITestBridge CreateWebBridge()
    {
        if (_page == null || _appUrl == null)
        {
            throw new InvalidOperationException(
                "Web bridge requires Playwright and Blazor server to be initialized. " +
                "Ensure InitializeAsync() completed successfully. " +
                "This may happen if Blazor project file is not found.");
        }
        return new WebTestBridge(_page, _appUrl);
    }

    [Theory]
    [MemberData(nameof(TestBridges))]
    public void PlayerMovement_UpdatesPosition(string platform)
    {
        var bridge = CreateBridge(platform);
        
        try
        {
            // Arrange
            var scenario = new TestScenario(bridge);

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
        finally
        {
            if (bridge is IDisposable disposable)
            {
                if (platform == "Stride")
                {
                    _disposables.Add(disposable);
                }
                else
                {
                    // Web bridges are disposed immediately (they're per-test)
                    disposable.Dispose();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestBridges))]
    public void PlayerDamage_ReducesHealth(string platform)
    {
        var bridge = CreateBridge(platform);
        
        try
        {
            // Arrange
            var scenario = new TestScenario(bridge);

            // Act
            var player = scenario.Player("TestPlayer", health: 100);
            scenario.Step();
            scenario.Assert.Player(player).HasHealth(100).IsAlive();

            player.TakeDamage(30);
            scenario.Step();
            scenario.Assert.Player(player).HasHealth(70).IsAlive();

            player.TakeDamage(80);
            scenario.Step();
            scenario.Assert.Player(player).HasHealth(0).IsDead();
        }
        finally
        {
            if (bridge is IDisposable disposable)
            {
                if (platform == "Stride")
                {
                    _disposables.Add(disposable);
                }
                else
                {
                    // Web bridges are disposed immediately (they're per-test)
                    disposable.Dispose();
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestBridges))]
    public void RenderingInitialization_Works(string platform)
    {
        // Skip rendering test for Web (Web doesn't have full rendering system like Stride)
        if (platform == "Web")
        {
            // Web rendering is simplified - skip this test
            return;
        }

        var bridge = CreateBridge(platform);
        
        try
        {
            // Arrange
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 1);

            // Assert
            scenario.Assert.Rendering()
                .IsInitialized()
                .HasCameraSlots(1);
        }
        finally
        {
            if (bridge is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
        }
    }

    public async Task DisposeAsync()
    {
        // Dispose all bridges
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();

        // Clean up Web resources
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
