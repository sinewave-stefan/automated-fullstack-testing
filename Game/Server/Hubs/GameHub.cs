using Game.Core;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Hubs;

/// <summary>
/// SignalR Hub for realtime game communication between server and clients.
/// Handles player updates, movement, and AI state synchronization.
/// </summary>
public class GameHub : Hub
{
    private static readonly Dictionary<string, Player> _players = new();
    private static readonly AI _ai = new(42); // Deterministic AI for consistent behavior
    private static Vector2D _aiPosition = new(0, 0);
    private static readonly object _lock = new();

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Create a player for the new connection
        lock (_lock)
        {
            var player = new Player($"Player_{Context.ConnectionId[..8]}", 100);
            _players[Context.ConnectionId] = player;
        }

        await Clients.Caller.SendAsync("PlayerCreated", Context.ConnectionId);
        await BroadcastGameState();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_lock)
        {
            _players.Remove(Context.ConnectionId);
        }

        await BroadcastGameState();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task MovePlayer(float deltaX, float deltaY)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(Context.ConnectionId, out var player))
            {
                player.Move(deltaX, deltaY);
            }
        }

        await BroadcastGameState();
    }

    public async Task TakeDamage(int amount)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(Context.ConnectionId, out var player))
            {
                player.TakeDamage(amount);
            }
        }

        await BroadcastGameState();
    }

    public async Task Heal(int amount)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(Context.ConnectionId, out var player))
            {
                player.Heal(amount);
            }
        }

        await BroadcastGameState();
    }

    public async Task UpdateAI()
    {
        lock (_lock)
        {
            // Get the first player or use origin
            var targetPlayer = _players.Values.FirstOrDefault();
            if (targetPlayer != null)
            {
                var distance = Vector2D.Distance(_aiPosition, targetPlayer.Position);
                var decision = _ai.MakeDecision(targetPlayer.Health, targetPlayer.MaxHealth, distance);

                // Move AI based on decision
                if (decision == AIDecision.Seek)
                {
                    var velocity = _ai.SeekTarget(_aiPosition, targetPlayer.Position, 1.0f);
                    _aiPosition = Physics.UpdatePosition(_aiPosition, velocity, 0.1f);
                }
                else if (decision == AIDecision.Flee)
                {
                    var velocity = _ai.FleeFrom(_aiPosition, targetPlayer.Position, 1.0f);
                    _aiPosition = Physics.UpdatePosition(_aiPosition, velocity, 0.1f);
                }
            }
        }

        await BroadcastGameState();
    }

    public async Task RequestGameState()
    {
        await BroadcastGameState();
    }

    private async Task BroadcastGameState()
    {
        GameStateDto state;
        lock (_lock)
        {
            state = new GameStateDto
            {
                Players = _players.Select(kvp => new PlayerDto
                {
                    Id = kvp.Key,
                    Name = kvp.Value.Name,
                    Health = kvp.Value.Health,
                    MaxHealth = kvp.Value.MaxHealth,
                    PositionX = kvp.Value.Position.X,
                    PositionY = kvp.Value.Position.Y,
                    IsAlive = kvp.Value.IsAlive
                }).ToList(),
                AIPosition = new PositionDto { X = _aiPosition.X, Y = _aiPosition.Y }
            };
        }

        await Clients.All.SendAsync("GameStateUpdated", state);
    }
}

/// <summary>
/// Data transfer object for game state
/// </summary>
public class GameStateDto
{
    public List<PlayerDto> Players { get; set; } = new();
    public PositionDto AIPosition { get; set; } = new();
}

/// <summary>
/// Data transfer object for player state
/// </summary>
public class PlayerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public bool IsAlive { get; set; }
}

/// <summary>
/// Data transfer object for position
/// </summary>
public class PositionDto
{
    public float X { get; set; }
    public float Y { get; set; }
}
