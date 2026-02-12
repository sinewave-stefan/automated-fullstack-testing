using Game.Core.Testing;
using Game.StrideApp.Testing;
using Xunit;

namespace Game.TestFrameworkTests;

/// <summary>
/// Integration tests using real Stride game engine instances.
/// These tests verify that the fluent API works with actual Stride runtime.
/// </summary>
public class StrideIntegrationTests : IDisposable
{
    private StrideTestBridge? _bridge;

    [Fact]
    public void PlayerMovement_StrideInstance_UpdatesPosition()
    {
        // Arrange
        _bridge = StrideTestBridge.CreateTestInstance();
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
    public void PlayerDamage_StrideInstance_ReducesHealth()
    {
        // Arrange
        _bridge = StrideTestBridge.CreateTestInstance();
        var scenario = new TestScenario(_bridge);

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

    [Fact]
    public void RenderingInitialization_StrideInstance_Works()
    {
        // Arrange
        _bridge = StrideTestBridge.CreateTestInstance();
        var scenario = new TestScenario(_bridge);

        // Act
        scenario.InitializeRendering(cameraSlots: 1);

        // Assert
        scenario.Assert.Rendering()
            .IsInitialized()
            .HasCameraSlots(1);
    }

    public void Dispose()
    {
        _bridge?.Dispose();
    }
}
