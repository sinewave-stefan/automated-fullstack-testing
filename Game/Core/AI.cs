namespace Game.Core;

/// <summary>
/// Simple AI behavior for NPCs.
/// Platform-independent game logic.
/// </summary>
public class AI
{
    private readonly Random _random;

    public AI(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Calculate direction to move towards target
    /// </summary>
    public Vector2D SeekTarget(Vector2D currentPosition, Vector2D targetPosition, float speed)
    {
        var direction = (targetPosition - currentPosition).Normalize();
        return direction * speed;
    }

    /// <summary>
    /// Calculate direction to move away from threat
    /// </summary>
    public Vector2D FleeFrom(Vector2D currentPosition, Vector2D threatPosition, float speed)
    {
        var direction = (currentPosition - threatPosition).Normalize();
        return direction * speed;
    }

    /// <summary>
    /// Make a simple decision based on health
    /// </summary>
    public AIDecision MakeDecision(int currentHealth, int maxHealth, float distanceToTarget)
    {
        var healthPercentage = (float)currentHealth / maxHealth;

        // Flee if health is low
        if (healthPercentage < 0.3f)
            return AIDecision.Flee;

        // Attack if close and healthy
        if (distanceToTarget < 5.0f && healthPercentage > 0.5f)
            return AIDecision.Attack;

        // Otherwise seek
        return AIDecision.Seek;
    }

    /// <summary>
    /// Generate a random patrol point
    /// </summary>
    public Vector2D GeneratePatrolPoint(Vector2D center, float radius)
    {
        var angle = (float)(_random.NextDouble() * 2 * Math.PI);
        var distance = (float)(_random.NextDouble() * radius);
        
        return new Vector2D(
            center.X + MathF.Cos(angle) * distance,
            center.Y + MathF.Sin(angle) * distance
        );
    }
}

public enum AIDecision
{
    Seek,
    Flee,
    Attack,
    Patrol
}
