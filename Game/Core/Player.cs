namespace Game.Core;

/// <summary>
/// Represents a player in the game with position and health.
/// This is core game logic shared between native and web builds.
/// </summary>
public class Player
{
    public string Name { get; set; }
    public Vector2D Position { get; set; }
    public int Health { get; private set; }
    public int MaxHealth { get; }
    public bool IsAlive => Health > 0;

    public Player(string name, int maxHealth = 100)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MaxHealth = maxHealth;
        Health = maxHealth;
        Position = new Vector2D(0, 0);
    }

    public void Move(float deltaX, float deltaY)
    {
        Position = new Vector2D(Position.X + deltaX, Position.Y + deltaY);
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative", nameof(damage));
        
        Health = Math.Max(0, Health - damage);
    }

    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
        
        Health = Math.Min(MaxHealth, Health + amount);
    }
}
