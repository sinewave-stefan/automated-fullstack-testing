namespace Game.Core.Testing;

/// <summary>
/// Fluent API for writing platform-agnostic test scenarios.
/// Provides a readable DSL for test authoring that works across all platforms.
/// </summary>
public class TestScenario
{
    private readonly ITestBridge _bridge;
    private readonly Dictionary<string, string> _playerIds = new();
    private int _nextPlayerId = 1;

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
        _nextPlayerId = 1;
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
            throw new AssertionException(
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
            throw new AssertionException(
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
            throw new AssertionException("Expected player to be alive but was dead");
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
            throw new AssertionException("Expected player to be dead but was alive");
        }

        return this;
    }
}

/// <summary>
/// Exception thrown when a scenario assertion fails.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message)
    {
    }
}
