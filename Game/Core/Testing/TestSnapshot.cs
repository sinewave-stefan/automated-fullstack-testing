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
    /// Generic entities in the scene (cameras, lights, objects, etc.)
    /// </summary>
    public List<EntitySnapshot> Entities { get; set; } = new();

    /// <summary>
    /// Rendering system state.
    /// </summary>
    public RenderingSnapshot Rendering { get; set; } = new();

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

/// <summary>
/// Snapshot of a generic entity in the scene.
/// </summary>
public class EntitySnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Camera", "Light", "Model", etc.
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Platform-agnostic rendering system state.
/// Works for both Stride (GraphicsCompositor) and Web (Canvas/WebGL).
/// </summary>
public class RenderingSnapshot
{
    /// <summary>
    /// Whether the rendering system is initialized and ready.
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Number of camera slots/viewports available.
    /// </summary>
    public int CameraSlotCount { get; set; }

    /// <summary>
    /// Number of render stages/passes configured.
    /// </summary>
    public int RenderStageCount { get; set; }

    /// <summary>
    /// Active camera entity ID (if any).
    /// </summary>
    public string? ActiveCameraId { get; set; }

    /// <summary>
    /// Render target width (viewport/canvas).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Render target height (viewport/canvas).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Platform-specific rendering info.
    /// </summary>
    public Dictionary<string, object> PlatformInfo { get; set; } = new();
}
