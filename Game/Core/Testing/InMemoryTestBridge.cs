namespace Game.Core.Testing;

/// <summary>
/// Simple in-memory test bridge for unit testing the test framework.
/// Maintains game state and allows deterministic stepping.
/// </summary>
public class InMemoryTestBridge : ITestBridge
{
    private int _currentFrame;
    private readonly Dictionary<string, Player> _players = new();
    private readonly Dictionary<string, (AI ai, Vector2D position)> _aiEntities = new();
    private const float FixedDeltaTime = 1.0f / 60.0f; // 60 FPS

    public bool IsTestMode => true;

    public void Step()
    {
        _currentFrame++;
        
        // Update physics for all players (simple example)
        foreach (var kvp in _players)
        {
            // In a real implementation, this would update physics, AI, etc.
            // For now, we just increment the frame counter
        }
    }

    public void Step(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Step();
        }
    }

    public void Reset()
    {
        _currentFrame = 0;
        _players.Clear();
        _aiEntities.Clear();
    }

    public TestSnapshot GetSnapshot()
    {
        var snapshot = new TestSnapshot
        {
            Frame = _currentFrame
        };

        foreach (var kvp in _players)
        {
            snapshot.Players.Add(new PlayerSnapshot
            {
                Id = kvp.Key,
                Name = kvp.Value.Name,
                X = kvp.Value.Position.X,
                Y = kvp.Value.Position.Y,
                Health = kvp.Value.Health,
                MaxHealth = kvp.Value.MaxHealth,
                IsAlive = kvp.Value.IsAlive
            });
        }

        foreach (var kvp in _aiEntities)
        {
            snapshot.AIEntities.Add(new AISnapshot
            {
                Id = kvp.Key,
                X = kvp.Value.position.X,
                Y = kvp.Value.position.Y,
                CurrentDecision = "Idle"
            });
        }

        return snapshot;
    }

    public void ExecuteCommand(TestCommand command)
    {
        switch (command.Type)
        {
            case TestCommandType.Move:
                ExecuteMoveCommand(command);
                break;

            case TestCommandType.Damage:
                ExecuteDamageCommand(command);
                break;

            case TestCommandType.Heal:
                ExecuteHealCommand(command);
                break;

            case TestCommandType.Spawn:
                ExecuteSpawnCommand(command);
                break;

            default:
                throw new NotSupportedException($"Command type {command.Type} not supported");
        }
    }

    private void ExecuteMoveCommand(TestCommand command)
    {
        if (command.TargetId == null || !_players.ContainsKey(command.TargetId))
            return;

        var deltaX = ConvertToFloat(command.Parameters["deltaX"]);
        var deltaY = ConvertToFloat(command.Parameters["deltaY"]);

        _players[command.TargetId].Move(deltaX, deltaY);
    }

    private void ExecuteDamageCommand(TestCommand command)
    {
        if (command.TargetId == null || !_players.ContainsKey(command.TargetId))
            return;

        var amount = ConvertToInt(command.Parameters["amount"]);
        _players[command.TargetId].TakeDamage(amount);
    }

    private void ExecuteHealCommand(TestCommand command)
    {
        if (command.TargetId == null || !_players.ContainsKey(command.TargetId))
            return;

        var amount = ConvertToInt(command.Parameters["amount"]);
        _players[command.TargetId].Heal(amount);
    }

    private void ExecuteSpawnCommand(TestCommand command)
    {
        var id = command.TargetId ?? Guid.NewGuid().ToString();
        var name = command.Parameters.ContainsKey("name") 
            ? command.Parameters["name"].ToString() ?? "Player"
            : "Player";
        var x = command.Parameters.ContainsKey("x") 
            ? ConvertToFloat(command.Parameters["x"]) 
            : 0f;
        var y = command.Parameters.ContainsKey("y") 
            ? ConvertToFloat(command.Parameters["y"]) 
            : 0f;
        var health = command.Parameters.ContainsKey("health") 
            ? ConvertToInt(command.Parameters["health"]) 
            : 100;

        var player = new Player(name, health);
        player.Move(x, y);
        _players[id] = player;
    }

    private static float ConvertToFloat(object value)
    {
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetSingle();
        }
        return Convert.ToSingle(value);
    }

    private static int ConvertToInt(object value)
    {
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetInt32();
        }
        return Convert.ToInt32(value);
    }

    /// <summary>
    /// Helper method to manually add a player for setup.
    /// </summary>
    public void AddPlayer(string id, Player player)
    {
        _players[id] = player;
    }

    /// <summary>
    /// Helper method to manually add an AI entity for setup.
    /// </summary>
    public void AddAI(string id, AI ai, Vector2D position)
    {
        _aiEntities[id] = (ai, position);
    }
}
