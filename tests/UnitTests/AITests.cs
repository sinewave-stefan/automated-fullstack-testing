using Game.Core;

namespace Game.UnitTests;

public class AITests
{
    [Fact]
    public void AI_SeekTarget_ReturnsCorrectDirection()
    {
        var ai = new AI();
        var current = new Vector2D(0, 0);
        var target = new Vector2D(10, 0);
        var velocity = ai.SeekTarget(current, target, 5.0f);
        
        Assert.Equal(5.0f, velocity.X, 2);
        Assert.Equal(0, velocity.Y, 2);
    }

    [Fact]
    public void AI_FleeFrom_ReturnsCorrectDirection()
    {
        var ai = new AI();
        var current = new Vector2D(0, 0);
        var threat = new Vector2D(10, 0);
        var velocity = ai.FleeFrom(current, threat, 5.0f);
        
        Assert.True(velocity.X < 0); // Should move away (negative X)
    }

    [Fact]
    public void AI_MakeDecision_FleesWhenLowHealth()
    {
        var ai = new AI();
        var decision = ai.MakeDecision(20, 100, 10.0f);
        
        Assert.Equal(AIDecision.Flee, decision);
    }

    [Fact]
    public void AI_MakeDecision_AttacksWhenCloseAndHealthy()
    {
        var ai = new AI();
        var decision = ai.MakeDecision(80, 100, 3.0f);
        
        Assert.Equal(AIDecision.Attack, decision);
    }

    [Fact]
    public void AI_MakeDecision_SeeksWhenModerateHealthAndDistance()
    {
        var ai = new AI();
        var decision = ai.MakeDecision(60, 100, 10.0f);
        
        Assert.Equal(AIDecision.Seek, decision);
    }

    [Fact]
    public void AI_GeneratePatrolPoint_IsDeterministicWithSeed()
    {
        var ai1 = new AI(42);
        var ai2 = new AI(42);
        var center = new Vector2D(0, 0);
        
        var point1 = ai1.GeneratePatrolPoint(center, 10);
        var point2 = ai2.GeneratePatrolPoint(center, 10);
        
        Assert.Equal(point1.X, point2.X);
        Assert.Equal(point1.Y, point2.Y);
    }

    [Fact]
    public void AI_GeneratePatrolPoint_StaysWithinRadius()
    {
        var ai = new AI(42);
        var center = new Vector2D(5, 5);
        var radius = 10.0f;
        
        for (int i = 0; i < 100; i++)
        {
            var point = ai.GeneratePatrolPoint(center, radius);
            var distance = Vector2D.Distance(center, point);
            Assert.True(distance <= radius);
        }
    }
}
