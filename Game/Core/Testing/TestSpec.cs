using System.Text.Json.Serialization;

namespace Game.Core.Testing;

/// <summary>
/// Represents a complete test specification with steps and assertions.
/// Platform-agnostic test definition that can be executed on any build.
/// </summary>
public class TestSpec
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable test name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Test description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Initial game state setup.
    /// </summary>
    public TestSetup Setup { get; set; } = new();

    /// <summary>
    /// Test steps to execute in sequence.
    /// </summary>
    public List<TestStep> Steps { get; set; } = new();
}

/// <summary>
/// Initial setup for a test.
/// </summary>
public class TestSetup
{
    /// <summary>
    /// Players to spawn at the start.
    /// </summary>
    public List<PlayerSetup> Players { get; set; } = new();

    /// <summary>
    /// AI entities to spawn at the start.
    /// </summary>
    public List<AISetup> AIEntities { get; set; } = new();
}

/// <summary>
/// Player initialization configuration.
/// </summary>
public class PlayerSetup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public int Health { get; set; } = 100;
}

/// <summary>
/// AI initialization configuration.
/// </summary>
public class AISetup
{
    public string Id { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public int Seed { get; set; }
}

/// <summary>
/// A single test step with optional command and assertions.
/// </summary>
public class TestStep
{
    /// <summary>
    /// Number of simulation steps to advance before executing command.
    /// </summary>
    public int AdvanceSteps { get; set; }

    /// <summary>
    /// Optional command to execute at this step.
    /// </summary>
    public TestCommand? Command { get; set; }

    /// <summary>
    /// Assertions to verify after this step.
    /// </summary>
    public List<TestAssertion> Assertions { get; set; } = new();
}

/// <summary>
/// An assertion to verify game state.
/// </summary>
public class TestAssertion
{
    /// <summary>
    /// Type of assertion.
    /// </summary>
    public AssertionType Type { get; set; }

    /// <summary>
    /// Target entity ID for the assertion.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Expected value or condition.
    /// </summary>
    public object? Expected { get; set; }

    /// <summary>
    /// Tolerance for floating-point comparisons.
    /// </summary>
    public float Tolerance { get; set; } = 0.01f;
}

/// <summary>
/// Types of assertions supported.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssertionType
{
    /// <summary>
    /// Assert player position X coordinate.
    /// </summary>
    PlayerPositionX,

    /// <summary>
    /// Assert player position Y coordinate.
    /// </summary>
    PlayerPositionY,

    /// <summary>
    /// Assert player health value.
    /// </summary>
    PlayerHealth,

    /// <summary>
    /// Assert player is alive.
    /// </summary>
    PlayerIsAlive,

    /// <summary>
    /// Assert player is dead.
    /// </summary>
    PlayerIsDead,

    /// <summary>
    /// Assert AI position X coordinate.
    /// </summary>
    AIPositionX,

    /// <summary>
    /// Assert AI position Y coordinate.
    /// </summary>
    AIPositionY,

    /// <summary>
    /// Assert total player count.
    /// </summary>
    PlayerCount,

    /// <summary>
    /// Custom metadata assertion.
    /// </summary>
    Metadata
}
