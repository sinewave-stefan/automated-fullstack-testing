namespace Game.Core;

/// <summary>
/// Simple physics calculations for game entities.
/// Platform-independent and deterministic.
/// </summary>
public static class Physics
{
    public const float Gravity = 9.8f;
    public const float DefaultFriction = 0.1f;

    /// <summary>
    /// Calculate new position based on velocity and time
    /// </summary>
    public static Vector2D UpdatePosition(Vector2D currentPosition, Vector2D velocity, float deltaTime)
    {
        return currentPosition + velocity * deltaTime;
    }

    /// <summary>
    /// Apply friction to velocity
    /// </summary>
    public static Vector2D ApplyFriction(Vector2D velocity, float friction, float deltaTime)
    {
        var frictionForce = velocity * friction * deltaTime;
        var newVelocity = velocity - frictionForce;
        
        // Stop if velocity is very small
        if (newVelocity.Length < 0.01f)
            return new Vector2D(0, 0);
        
        return newVelocity;
    }

    /// <summary>
    /// Check if two circular entities collide
    /// </summary>
    public static bool CheckCollision(Vector2D pos1, float radius1, Vector2D pos2, float radius2)
    {
        var distance = Vector2D.Distance(pos1, pos2);
        return distance < (radius1 + radius2);
    }
}
