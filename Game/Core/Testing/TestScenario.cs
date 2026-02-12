namespace Game.Core.Testing;

/// <summary>
/// Fluent API for writing platform-agnostic test scenarios.
/// Provides a readable DSL for test authoring that works across all platforms.
/// </summary>
public class TestScenario
{
    private readonly ITestBridge _bridge;
    private readonly Dictionary<string, string> _playerIds = new();
    private readonly Dictionary<string, string> _entityIds = new();
    private int _nextPlayerId = 1;
    private int _nextEntityId = 1;

    public TestScenario(ITestBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    /// <summary>
    /// Resets the game state to initial conditions.
    /// </summary>
    public TestScenario Reset()
    {
        _bridge.Reset();
        _playerIds.Clear();
        _entityIds.Clear();
        _nextPlayerId = 1;
        _nextEntityId = 1;
        return this;
    }

    /// <summary>
    /// Creates a player with the specified configuration.
    /// Returns a player handle for use in subsequent operations.
    /// </summary>
    public PlayerHandle Player(string name, float x = 0, float y = 0, int health = 100)
    {
        var playerId = $"player{_nextPlayerId++}";
        _playerIds[name] = playerId;

        var spawnCommand = new TestCommand
        {
            Type = TestCommandType.Spawn,
            TargetId = playerId,
            Parameters = new()
            {
                { "name", name },
                { "x", x },
                { "y", y },
                { "health", health }
            }
        };

        _bridge.ExecuteCommand(spawnCommand);

        return new PlayerHandle(playerId, name, this);
    }

    /// <summary>
    /// Creates a generic entity (camera, light, model, etc.)
    /// </summary>
    public EntityHandle Entity(string name, string type, float x = 0, float y = 0, float z = 0)
    {
        var entityId = $"entity{_nextEntityId++}";
        _entityIds[name] = entityId;

        var command = TestCommand.SpawnEntity(entityId, name, type, x, y, z);
        _bridge.ExecuteCommand(command);

        return new EntityHandle(entityId, name, type, this);
    }

    /// <summary>
    /// Creates a camera entity.
    /// </summary>
    public EntityHandle Camera(string name, float x = 0, float y = 0, float z = 0)
    {
        return Entity(name, "Camera", x, y, z);
    }

    /// <summary>
    /// Initializes the rendering system with the specified number of camera slots.
    /// </summary>
    public TestScenario InitializeRendering(int cameraSlots = 1)
    {
        var command = TestCommand.InitializeRendering(cameraSlots);
        _bridge.ExecuteCommand(command);
        return this;
    }

    /// <summary>
    /// Advances the simulation by the specified number of frames.
    /// </summary>
    public TestScenario Step(int frames = 1)
    {
        _bridge.Step(frames);
        return this;
    }

    /// <summary>
    /// Gets the current game state snapshot.
    /// </summary>
    public TestSnapshot GetSnapshot()
    {
        return _bridge.GetSnapshot();
    }

    /// <summary>
    /// Creates a step builder for fluent command chaining.
    /// </summary>
    public StepBuilder Then()
    {
        return new StepBuilder(this);
    }

    /// <summary>
    /// Gets assertion helpers for the scenario.
    /// </summary>
    public AssertionHelper Assert => new AssertionHelper(this);

    internal void ExecuteCommand(TestCommand command)
    {
        _bridge.ExecuteCommand(command);
    }

    internal string GetPlayerId(string name)
    {
        return _playerIds.TryGetValue(name, out var id) ? id : name;
    }

    internal string GetEntityId(string name)
    {
        return _entityIds.TryGetValue(name, out var id) ? id : name;
    }
}

/// <summary>
/// Handle for a generic entity in the test scenario.
/// </summary>
public class EntityHandle
{
    private readonly string _entityId;
    private readonly string _entityName;
    private readonly string _entityType;
    private readonly TestScenario _scenario;

    internal EntityHandle(string entityId, string entityName, string entityType, TestScenario scenario)
    {
        _entityId = entityId;
        _entityName = entityName;
        _entityType = entityType;
        _scenario = scenario;
    }

    public string Id => _entityId;
    public string Name => _entityName;
    public string Type => _entityType;

    /// <summary>
    /// Sets this entity as the active camera (if it's a camera).
    /// </summary>
    public EntityHandle SetAsActiveCamera(int slotIndex = 0)
    {
        var command = TestCommand.SetActiveCamera(_entityId, slotIndex);
        _scenario.ExecuteCommand(command);
        return this;
    }

    /// <summary>
    /// Advances simulation after this operation.
    /// </summary>
    public EntityHandle ThenStep(int frames = 1)
    {
        _scenario.Step(frames);
        return this;
    }
}

/// <summary>
/// Handle for a player entity in the test scenario.
/// Provides fluent operations for player manipulation.
/// </summary>
public class PlayerHandle
{
    private readonly string _playerId;
    private readonly string _playerName;
    private readonly TestScenario _scenario;

    internal PlayerHandle(string playerId, string playerName, TestScenario scenario)
    {
        _playerId = playerId;
        _playerName = playerName;
        _scenario = scenario;
    }

    public string Id => _playerId;
    public string Name => _playerName;

    /// <summary>
    /// Moves the player by the specified delta.
    /// </summary>
    public PlayerHandle Move(float deltaX, float deltaY)
    {
        var command = TestCommand.Move(_playerId, deltaX, deltaY);
        _scenario.ExecuteCommand(command);
        return this;
    }

    /// <summary>
    /// Applies damage to the player.
    /// </summary>
    public PlayerHandle TakeDamage(int amount)
    {
        var command = TestCommand.Damage(_playerId, amount);
        _scenario.ExecuteCommand(command);
        return this;
    }

    /// <summary>
    /// Heals the player.
    /// </summary>
    public PlayerHandle Heal(int amount)
    {
        var command = TestCommand.Heal(_playerId, amount);
        _scenario.ExecuteCommand(command);
        return this;
    }

    /// <summary>
    /// Advances simulation after this operation.
    /// </summary>
    public PlayerHandle ThenStep(int frames = 1)
    {
        _scenario.Step(frames);
        return this;
    }
}

/// <summary>
/// Builder for creating test steps with fluent syntax.
/// </summary>
public class StepBuilder
{
    private readonly TestScenario _scenario;

    internal StepBuilder(TestScenario scenario)
    {
        _scenario = scenario;
    }

    /// <summary>
    /// Performs an operation with a player.
    /// </summary>
    public PlayerStepBuilder WithPlayer(PlayerHandle player)
    {
        return new PlayerStepBuilder(_scenario, player);
    }

    /// <summary>
    /// Advances the simulation.
    /// </summary>
    public TestScenario Step(int frames = 1)
    {
        return _scenario.Step(frames);
    }
}

/// <summary>
/// Builder for player-specific operations in a step.
/// </summary>
public class PlayerStepBuilder
{
    private readonly TestScenario _scenario;
    private readonly PlayerHandle _player;

    internal PlayerStepBuilder(TestScenario scenario, PlayerHandle player)
    {
        _scenario = scenario;
        _player = player;
    }

    /// <summary>
    /// Moves the player.
    /// </summary>
    public PlayerStepBuilder Move(float deltaX, float deltaY)
    {
        _player.Move(deltaX, deltaY);
        return this;
    }

    /// <summary>
    /// Applies damage.
    /// </summary>
    public PlayerStepBuilder TakeDamage(int amount)
    {
        _player.TakeDamage(amount);
        return this;
    }

    /// <summary>
    /// Heals the player.
    /// </summary>
    public PlayerStepBuilder Heal(int amount)
    {
        _player.Heal(amount);
        return this;
    }

    /// <summary>
    /// Advances to the next step.
    /// </summary>
    public StepBuilder ThenStep(int frames = 1)
    {
        _scenario.Step(frames);
        return new StepBuilder(_scenario);
    }

    /// <summary>
    /// Returns to the scenario for assertions or other operations.
    /// </summary>
    public TestScenario Done()
    {
        return _scenario;
    }
}

/// <summary>
/// Assertion helpers for test scenarios.
/// </summary>
public class AssertionHelper
{
    private readonly TestScenario _scenario;

    internal AssertionHelper(TestScenario scenario)
    {
        _scenario = scenario;
    }

    /// <summary>
    /// Asserts on a specific player.
    /// </summary>
    public PlayerAssertions Player(PlayerHandle player)
    {
        var snapshot = _scenario.GetSnapshot();
        var playerSnapshot = snapshot.Players.FirstOrDefault(p => p.Id == player.Id);
        
        if (playerSnapshot == null)
        {
            throw new InvalidOperationException($"Player {player.Name} not found in snapshot");
        }

        return new PlayerAssertions(playerSnapshot);
    }

    /// <summary>
    /// Asserts on a player by name.
    /// </summary>
    public PlayerAssertions Player(string playerName)
    {
        var playerId = _scenario.GetPlayerId(playerName);
        var snapshot = _scenario.GetSnapshot();
        var playerSnapshot = snapshot.Players.FirstOrDefault(p => p.Id == playerId);
        
        if (playerSnapshot == null)
        {
            throw new InvalidOperationException($"Player {playerName} not found in snapshot");
        }

        return new PlayerAssertions(playerSnapshot);
    }

    /// <summary>
    /// Asserts on the rendering system.
    /// </summary>
    public RenderingAssertions Rendering()
    {
        var snapshot = _scenario.GetSnapshot();
        return new RenderingAssertions(snapshot.Rendering);
    }

    /// <summary>
    /// Asserts on a specific entity.
    /// </summary>
    public EntityAssertions Entity(EntityHandle entity)
    {
        var snapshot = _scenario.GetSnapshot();
        var entitySnapshot = snapshot.Entities.FirstOrDefault(e => e.Id == entity.Id);
        
        if (entitySnapshot == null)
        {
            throw new InvalidOperationException($"Entity {entity.Name} not found in snapshot");
        }

        return new EntityAssertions(entitySnapshot);
    }

    /// <summary>
    /// Asserts on an entity by name.
    /// </summary>
    public EntityAssertions Entity(string entityName)
    {
        var entityId = _scenario.GetEntityId(entityName);
        var snapshot = _scenario.GetSnapshot();
        var entitySnapshot = snapshot.Entities.FirstOrDefault(e => e.Id == entityId || e.Name == entityName);
        
        if (entitySnapshot == null)
        {
            throw new InvalidOperationException($"Entity {entityName} not found in snapshot");
        }

        return new EntityAssertions(entitySnapshot);
    }
}

/// <summary>
/// Assertions for rendering system state.
/// </summary>
public class RenderingAssertions
{
    private readonly RenderingSnapshot _snapshot;

    internal RenderingAssertions(RenderingSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    /// <summary>
    /// Asserts the rendering system is initialized.
    /// </summary>
    public RenderingAssertions IsInitialized()
    {
        if (!_snapshot.IsInitialized)
        {
            throw new InvalidOperationException("Expected rendering system to be initialized");
        }
        return this;
    }

    /// <summary>
    /// Asserts the rendering system has at least the specified number of camera slots.
    /// </summary>
    public RenderingAssertions HasCameraSlots(int minCount = 1)
    {
        if (_snapshot.CameraSlotCount < minCount)
        {
            throw new InvalidOperationException(
                $"Expected at least {minCount} camera slot(s) but found {_snapshot.CameraSlotCount}");
        }
        return this;
    }

    /// <summary>
    /// Asserts an active camera is set.
    /// </summary>
    public RenderingAssertions HasActiveCamera()
    {
        if (string.IsNullOrEmpty(_snapshot.ActiveCameraId))
        {
            throw new InvalidOperationException("Expected an active camera to be set");
        }
        return this;
    }

    /// <summary>
    /// Asserts the viewport/canvas has the expected dimensions.
    /// </summary>
    public RenderingAssertions HasDimensions(int width, int height)
    {
        if (_snapshot.Width != width || _snapshot.Height != height)
        {
            throw new InvalidOperationException(
                $"Expected dimensions ({width}x{height}) but was ({_snapshot.Width}x{_snapshot.Height})");
        }
        return this;
    }
}

/// <summary>
/// Assertions for a specific entity.
/// </summary>
public class EntityAssertions
{
    private readonly EntitySnapshot _snapshot;

    internal EntityAssertions(EntitySnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    /// <summary>
    /// Asserts the entity exists.
    /// </summary>
    public EntityAssertions Exists()
    {
        // If we got here, entity exists
        return this;
    }

    /// <summary>
    /// Asserts the entity is of the expected type.
    /// </summary>
    public EntityAssertions IsOfType(string type)
    {
        if (_snapshot.Type != type)
        {
            throw new InvalidOperationException(
                $"Expected entity type '{type}' but was '{_snapshot.Type}'");
        }
        return this;
    }

    /// <summary>
    /// Asserts the entity is active.
    /// </summary>
    public EntityAssertions IsActive()
    {
        if (!_snapshot.IsActive)
        {
            throw new InvalidOperationException("Expected entity to be active");
        }
        return this;
    }

    /// <summary>
    /// Asserts the entity's position.
    /// </summary>
    public EntityAssertions HasPosition(float x, float y, float z = 0, float tolerance = 0.01f)
    {
        if (Math.Abs(_snapshot.X - x) > tolerance ||
            Math.Abs(_snapshot.Y - y) > tolerance ||
            Math.Abs(_snapshot.Z - z) > tolerance)
        {
            throw new InvalidOperationException(
                $"Expected position ({x}, {y}, {z}) but was ({_snapshot.X}, {_snapshot.Y}, {_snapshot.Z})");
        }
        return this;
    }
}

/// <summary>
/// Assertions for a specific player.
/// </summary>
public class PlayerAssertions
{
    private readonly PlayerSnapshot _snapshot;

    internal PlayerAssertions(PlayerSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    /// <summary>
    /// Asserts the player's position.
    /// </summary>
    public PlayerAssertions HasPosition(float x, float y, float tolerance = 0.01f)
    {
        var diffX = Math.Abs(_snapshot.X - x);
        var diffY = Math.Abs(_snapshot.Y - y);

        if (diffX > tolerance || diffY > tolerance)
        {
            throw new InvalidOperationException(
                $"Expected position ({x}, {y}) but was ({_snapshot.X}, {_snapshot.Y})");
        }

        return this;
    }

    /// <summary>
    /// Asserts the player's health.
    /// </summary>
    public PlayerAssertions HasHealth(int health)
    {
        if (_snapshot.Health != health)
        {
            throw new InvalidOperationException(
                $"Expected health {health} but was {_snapshot.Health}");
        }

        return this;
    }

    /// <summary>
    /// Asserts the player is alive.
    /// </summary>
    public PlayerAssertions IsAlive()
    {
        if (!_snapshot.IsAlive)
        {
            throw new InvalidOperationException("Expected player to be alive but was dead");
        }

        return this;
    }

    /// <summary>
    /// Asserts the player is dead.
    /// </summary>
    public PlayerAssertions IsDead()
    {
        if (_snapshot.IsAlive)
        {
            throw new InvalidOperationException("Expected player to be dead but was alive");
        }

        return this;
    }
}
