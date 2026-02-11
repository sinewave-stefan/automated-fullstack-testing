namespace Game.Core.Testing;

/// <summary>
/// Captures the current state of the game for testing and verification.
/// Platform-agnostic representation of game state.
/// </summary>
public class TestSnapshot
{
    /// <summary>
    /// Current frame/step number in the simulation.
    /// </summary>
    public int Frame { get; set; }

    /// <summary>
    /// Players in the game with their current state.
    /// </summary>
    public List<PlayerSnapshot> Players { get; set; } = new();

    /// <summary>
    /// AI entities with their current state.
    /// </summary>
    public List<AISnapshot> AIEntities { get; set; } = new();

    /// <summary>
    /// Additional metadata for diagnostics.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Snapshot of a player's state.
/// </summary>
public class PlayerSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public bool IsAlive { get; set; }
}

/// <summary>
/// Snapshot of an AI entity's state.
/// </summary>
public class AISnapshot
{
    public string Id { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public string CurrentDecision { get; set; } = string.Empty;
}
