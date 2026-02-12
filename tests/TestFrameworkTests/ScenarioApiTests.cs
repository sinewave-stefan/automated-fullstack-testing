using Game.Core;
using Game.Core.Testing;

namespace Game.TestFrameworkTests;

/// <summary>
/// Tests using the fluent scenario API (user-facing API).
/// Demonstrates code-based test authoring with readable DSL.
/// 
/// These tests exercise the framework through the high-level fluent API (TestScenario).
/// For low-level infrastructure tests (bridges, executors), see InfrastructureTests.cs.
/// </summary>
public class ScenarioApiTests
{
    [Fact]
    public void PlayerMovement_FluentApi_UpdatesPosition()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Act & Assert
        var player = scenario.Player("TestPlayer", x: 0, y: 0, health: 100);

        scenario.Step();
        scenario.Assert.Player(player).HasPosition(0, 0);

        player.Move(10, 5);
        scenario.Step();
        scenario.Assert.Player(player)
            .HasPosition(10, 5)
            .HasHealth(100)
            .IsAlive();
    }

    [Fact]
    public void PlayerDamage_FluentApi_ReducesHealth()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Act & Assert
        var player = scenario.Player("Warrior", health: 100);

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
    public void PlayerHealing_FluentApi_RestoresHealth()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Act & Assert
        var player = scenario.Player("Cleric", health: 100);
        
        player.TakeDamage(50).ThenStep();
        scenario.Assert.Player(player).HasHealth(50);

        player.Heal(30).ThenStep();
        scenario.Assert.Player(player).HasHealth(80);

        // Cannot exceed max health
        player.Heal(50).ThenStep();
        scenario.Assert.Player(player).HasHealth(100);
    }

    [Fact]
    public void CombatScenario_FluentApi_CompleteWorkflow()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Act - Create two players
        var warrior = scenario.Player("Warrior", x: 0, y: 0, health: 100);
        var mage = scenario.Player("Mage", x: 5, y: 5, health: 80);

        scenario.Step();

        // Warrior takes damage
        warrior.TakeDamage(30).ThenStep();
        scenario.Assert.Player(warrior).HasHealth(70).IsAlive();

        // Warrior heals
        warrior.Heal(20).ThenStep();
        scenario.Assert.Player(warrior).HasHealth(90);

        // Mage takes fatal damage
        mage.TakeDamage(100).ThenStep();
        scenario.Assert.Player(mage).HasHealth(0).IsDead();

        // Warrior moves
        warrior.Move(10, 15).ThenStep();
        scenario.Assert.Player(warrior)
            .HasPosition(10, 15)
            .HasHealth(90)
            .IsAlive();
    }

    [Fact]
    public void FluentChaining_StepBuilder_WorksCorrectly()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Act - Use fluent chaining
        var player = scenario.Player("Hero", x: 0, y: 0, health: 100);

        scenario.Step();
        
        player.Move(5, 5).ThenStep();
        scenario.Assert.Player(player).HasPosition(5, 5);
        
        player.TakeDamage(20).ThenStep();
        scenario.Assert.Player(player).HasHealth(80);
        
        player.Heal(10).ThenStep();

        // Assert
        scenario.Assert.Player(player)
            .HasPosition(5, 5)
            .HasHealth(90)
            .IsAlive();
    }

    [Fact]
    public void Reset_FluentApi_ClearsState()
    {
        // Arrange
        var bridge = new InMemoryTestBridge();
        var scenario = new TestScenario(bridge);

        // Create initial player
        var player1 = scenario.Player("Player1", health: 100);
        scenario.Step();

        // Reset and create new player
        scenario.Reset();
        var player2 = scenario.Player("Player2", health: 50);
        scenario.Step();

        // Assert - only player2 should exist
        var snapshot = scenario.GetSnapshot();
        Assert.Single(snapshot.Players);
        Assert.Equal("Player2", snapshot.Players[0].Name);
        Assert.Equal(50, snapshot.Players[0].Health);
    }
}
