using Game.Core;

namespace Game.UnitTests;

public class PhysicsTests
{
    [Fact]
    public void Physics_UpdatePosition_CalculatesCorrectly()
    {
        var position = new Vector2D(0, 0);
        var velocity = new Vector2D(10, 5);
        var result = Physics.UpdatePosition(position, velocity, 1.0f);
        
        Assert.Equal(10, result.X);
        Assert.Equal(5, result.Y);
    }

    [Fact]
    public void Physics_ApplyFriction_ReducesVelocity()
    {
        var velocity = new Vector2D(10, 0);
        var result = Physics.ApplyFriction(velocity, 0.5f, 1.0f);
        
        Assert.True(result.Length < velocity.Length);
    }

    [Fact]
    public void Physics_ApplyFriction_StopsAtLowVelocity()
    {
        var velocity = new Vector2D(0.005f, 0.005f);
        var result = Physics.ApplyFriction(velocity, 0.1f, 1.0f);
        
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void Physics_CheckCollision_DetectsCollision()
    {
        var pos1 = new Vector2D(0, 0);
        var pos2 = new Vector2D(5, 0);
        
        Assert.True(Physics.CheckCollision(pos1, 3, pos2, 3));
    }

    [Fact]
    public void Physics_CheckCollision_DetectsNoCollision()
    {
        var pos1 = new Vector2D(0, 0);
        var pos2 = new Vector2D(10, 0);
        
        Assert.False(Physics.CheckCollision(pos1, 2, pos2, 2));
    }
}
