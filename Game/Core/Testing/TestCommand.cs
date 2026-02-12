using System.Text.Json.Serialization;

namespace Game.Core.Testing;

/// <summary>
/// Platform-agnostic test commands that can be executed on any build.
/// </summary>
public class TestCommand
{
    /// <summary>
    /// Type of command to execute.
    /// </summary>
    public TestCommandType Type { get; set; }

    /// <summary>
    /// Target entity ID (e.g., player ID).
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Command parameters as key-value pairs.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Creates a player move command.
    /// </summary>
    public static TestCommand Move(string playerId, float deltaX, float deltaY)
    {
        return new TestCommand
        {
            Type = TestCommandType.Move,
            TargetId = playerId,
            Parameters = new()
            {
                { "deltaX", deltaX },
                { "deltaY", deltaY }
            }
        };
    }

    /// <summary>
    /// Creates a damage command.
    /// </summary>
    public static TestCommand Damage(string playerId, int amount)
    {
        return new TestCommand
        {
            Type = TestCommandType.Damage,
            TargetId = playerId,
            Parameters = new()
            {
                { "amount", amount }
            }
        };
    }

    /// <summary>
    /// Creates a heal command.
    /// </summary>
    public static TestCommand Heal(string playerId, int amount)
    {
        return new TestCommand
        {
            Type = TestCommandType.Heal,
            TargetId = playerId,
            Parameters = new()
            {
                { "amount", amount }
            }
        };
    }

    /// <summary>
    /// Creates an AI update command.
    /// </summary>
    public static TestCommand UpdateAI(string aiId)
    {
        return new TestCommand
        {
            Type = TestCommandType.UpdateAI,
            TargetId = aiId,
            Parameters = new()
        };
    }

    /// <summary>
    /// Creates a spawn entity command.
    /// </summary>
    public static TestCommand SpawnEntity(string entityId, string name, string type, float x = 0, float y = 0, float z = 0)
    {
        return new TestCommand
        {
            Type = TestCommandType.Spawn,
            TargetId = entityId,
            Parameters = new()
            {
                { "name", name },
                { "type", type },
                { "x", x },
                { "y", y },
                { "z", z }
            }
        };
    }

    /// <summary>
    /// Creates a command to initialize the rendering system.
    /// </summary>
    public static TestCommand InitializeRendering(int cameraSlots = 1)
    {
        return new TestCommand
        {
            Type = TestCommandType.InitializeRendering,
            Parameters = new()
            {
                { "cameraSlots", cameraSlots }
            }
        };
    }

    /// <summary>
    /// Creates a command to set the active camera.
    /// </summary>
    public static TestCommand SetActiveCamera(string cameraEntityId, int slotIndex = 0)
    {
        return new TestCommand
        {
            Type = TestCommandType.SetActiveCamera,
            TargetId = cameraEntityId,
            Parameters = new()
            {
                { "slotIndex", slotIndex }
            }
        };
    }
}

/// <summary>
/// Types of test commands supported by the test bridge.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TestCommandType
{
    /// <summary>
    /// Move a player entity.
    /// </summary>
    Move,

    /// <summary>
    /// Apply damage to a player.
    /// </summary>
    Damage,

    /// <summary>
    /// Heal a player.
    /// </summary>
    Heal,

    /// <summary>
    /// Trigger AI update.
    /// </summary>
    UpdateAI,

    /// <summary>
    /// Spawn a new entity.
    /// </summary>
    Spawn,

    /// <summary>
    /// Remove an entity.
    /// </summary>
    Remove,

    /// <summary>
    /// Initialize the rendering system with specified configuration.
    /// </summary>
    InitializeRendering,

    /// <summary>
    /// Set the active camera for rendering.
    /// </summary>
    SetActiveCamera
}
