using Game.Core.Testing;
using Game.StrideApp.Testing;
using Game.WebApp.Testing;
using Xunit;

namespace Game.TestFrameworkTests;

/// <summary>
/// Platform integration tests that verify the test framework works correctly across different platforms.
/// These tests run against both Stride and Web (Blazor) platforms to ensure the fluent API works consistently.
/// Unlike game integration tests (in tests/Integration/), these test the framework's cross-platform capabilities.
/// </summary>
public class PlatformIntegrationTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

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
            "Web" => WebTestBridge.CreateTestInstance(),
            _ => throw new NotSupportedException($"Platform {platform} is not supported")
        };
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
                _disposables.Add(disposable);
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
                _disposables.Add(disposable);
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

    public void Dispose()
    {
        // Dispose all bridges (each bridge manages its own resources)
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }
}
