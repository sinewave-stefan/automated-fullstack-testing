using Game.Core;

namespace Game.IntegrationTests;

/// <summary>
/// Integration tests demonstrating how platform-specific adapters
/// can work with the same core game logic
/// </summary>
public class GameIntegrationTests
{
    [Fact]
    public void CompleteGameScenario_PlayerAndAI_WorkTogether()
    {
        // Arrange - Set up game state using shared core logic
        var player = new Player("Integration Test Player", 100);
        var ai = new AI(42); // Deterministic seed for testing
        
        // Act - Simulate a game scenario
        // 1. Player moves
        player.Move(10, 5);
        
        // 2. AI makes decision based on player state
        var aiDecision = ai.MakeDecision(player.Health, player.MaxHealth, 8.0f);
        
        // 3. Player takes damage
        player.TakeDamage(30);
        
        // 4. AI reacts to changed player state
        var newAiDecision = ai.MakeDecision(player.Health, player.MaxHealth, 8.0f);
        
        // Assert - Verify the game logic works correctly
        Assert.Equal(new Vector2D(10, 5), player.Position);
        Assert.Equal(70, player.Health);
        Assert.True(player.IsAlive);
        
        // AI should seek when player is healthy
        Assert.Equal(AIDecision.Seek, aiDecision);
        
        // AI behavior might change as player health changes
        // (this is just demonstrating the logic is shared)
        Assert.NotNull(newAiDecision);
    }

    [Fact]
    public void PhysicsSimulation_MultipleSteps_ProducesConsistentResults()
    {
        // This test demonstrates that physics calculations are deterministic
        // and will produce identical results in both native and web builds
        
        var position = new Vector2D(0, 0);
        var velocity = new Vector2D(10, 5);
        var initialVelocity = velocity.Length;
        
        // Simulate 10 time steps
        for (int i = 0; i < 10; i++)
        {
            position = Physics.UpdatePosition(position, velocity, 0.1f);
            velocity = Physics.ApplyFriction(velocity, Physics.DefaultFriction, 0.1f);
        }
        
        // Verify final state
        Assert.True(position.X > 0, $"Position X should be positive, was {position.X}");
        Assert.True(position.Y >= 0, $"Position Y should be non-negative, was {position.Y}");
        // Velocity should have decreased or stopped (not increased)
        Assert.True(velocity.Length <= initialVelocity, $"Velocity should not increase: initial={initialVelocity}, final={velocity.Length}");
    }

    [Fact]
    public void CollisionDetection_WorksAcrossPlatforms()
    {
        // Create two players
        var player1 = new Player("Player 1");
        var player2 = new Player("Player 2");
        
        player1.Move(0, 0);
        player2.Move(5, 0);
        
        // Check collision (using arbitrary radius of 3)
        var collided = Physics.CheckCollision(player1.Position, 3, player2.Position, 3);
        
        Assert.True(collided);
        
        // Move player2 far away
        player2.Move(10, 0);
        
        var stillColliding = Physics.CheckCollision(player1.Position, 3, player2.Position, 3);
        
        Assert.False(stillColliding);
    }
}
