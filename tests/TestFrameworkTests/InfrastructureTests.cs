using Game.Core;
using Game.Core.Testing;

namespace Game.TestFrameworkTests;

/// <summary>
/// Tests for the unified test framework infrastructure (low-level components).
/// Validates ITestBridge implementations and TestSpecExecutor functionality.
/// 
/// These tests exercise the framework infrastructure directly, without using the fluent API.
/// For tests of the user-facing fluent API, see ScenarioApiTests.cs.
/// </summary>
public class InfrastructureTests
{
    [Fact]
    public void InMemoryTestBridge_ExecutesBasicCommands()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var player = new Player("Test Player", 100);
        bridge.AddPlayer("player1", player);

        // Act
        bridge.Step();
        var snapshot = bridge.GetSnapshot();

        // Assert
        Assert.Equal(1, snapshot.Frame);
        Assert.Single(snapshot.Players);
        Assert.Equal("Test Player", snapshot.Players[0].Name);
        Assert.Equal(100, snapshot.Players[0].Health);
    }

    [Fact]
    public void InMemoryTestBridge_ExecutesMoveCommand()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var player = new Player("Test Player", 100);
        bridge.AddPlayer("player1", player);

        // Act
        var moveCommand = TestCommand.Move("player1", 10f, 5f);
        bridge.ExecuteCommand(moveCommand);
        var snapshot = bridge.GetSnapshot();

        // Assert
        var playerSnapshot = snapshot.Players[0];
        Assert.Equal(10f, playerSnapshot.X);
        Assert.Equal(5f, playerSnapshot.Y);
    }

    [Fact]
    public void InMemoryTestBridge_ExecutesDamageCommand()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var player = new Player("Test Player", 100);
        bridge.AddPlayer("player1", player);

        // Act
        var damageCommand = TestCommand.Damage("player1", 30);
        bridge.ExecuteCommand(damageCommand);
        var snapshot = bridge.GetSnapshot();

        // Assert
        var playerSnapshot = snapshot.Players[0];
        Assert.Equal(70, playerSnapshot.Health);
        Assert.True(playerSnapshot.IsAlive);
    }

    [Fact]
    public void InMemoryTestBridge_ExecutesHealCommand()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var player = new Player("Test Player", 100);
        player.TakeDamage(50);
        bridge.AddPlayer("player1", player);

        // Act
        var healCommand = TestCommand.Heal("player1", 30);
        bridge.ExecuteCommand(healCommand);
        var snapshot = bridge.GetSnapshot();

        // Assert
        var playerSnapshot = snapshot.Players[0];
        Assert.Equal(80, playerSnapshot.Health);
    }

    [Fact]
    public void TestSpecExecutor_ExecutesSimpleSpec()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        
        var spec = new TestSpec
        {
            Id = "simple-test",
            Name = "Simple Test",
            Description = "A simple test",
            Setup = new TestSetup
            {
                Players = new()
                {
                    new PlayerSetup
                    {
                        Id = "player1",
                        Name = "Test Player",
                        X = 0,
                        Y = 0,
                        Health = 100
                    }
                }
            },
            Steps = new()
            {
                new TestStep
                {
                    AdvanceSteps = 1,
                    Assertions = new()
                    {
                        new TestAssertion
                        {
                            Type = AssertionType.PlayerHealth,
                            TargetId = "player1",
                            Expected = 100
                        }
                    }
                }
            }
        };

        var executor = new TestSpecExecutor(bridge);

        // Act
        var result = executor.Execute(spec);

        // Assert
        Assert.True(result.Success, result.FailureReason ?? "No failure reason");
        Assert.Single(result.StepResults);
        Assert.True(result.StepResults[0].Success);
    }

}
