namespace Game.Core;

/// <summary>
/// Simple 2D vector for position and movement calculations.
/// Deterministic and platform-independent.
/// </summary>
public record struct Vector2D(float X, float Y)
{
    public float Length => MathF.Sqrt(X * X + Y * Y);

    public Vector2D Normalize()
    {
        var length = Length;
        return length > 0 ? new Vector2D(X / length, Y / length) : this;
    }

    public static Vector2D operator +(Vector2D a, Vector2D b) 
        => new(a.X + b.X, a.Y + b.Y);

    public static Vector2D operator -(Vector2D a, Vector2D b) 
        => new(a.X - b.X, a.Y - b.Y);

    public static Vector2D operator *(Vector2D v, float scalar) 
        => new(v.X * scalar, v.Y * scalar);

    public static float Distance(Vector2D a, Vector2D b)
        => (b - a).Length;
}
