using Game.Core;
using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("Stride Native Client - Multiplayer Mode");
Console.WriteLine("========================================");

// Connect to the game server
var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5200/gamehub")
    .WithAutomaticReconnect()
    .Build();

string? playerId = null;
var myPlayer = new { Name = "", Health = 0, MaxHealth = 0, PositionX = 0f, PositionY = 0f, IsAlive = false };
var allPlayers = new List<dynamic>();
var aiPosition = new { X = 0f, Y = 0f };

// Handle server events
hubConnection.On<string>("PlayerCreated", (connectionId) =>
{
    playerId = connectionId;
    Console.WriteLine($"✅ Connected as: {playerId}");
});

hubConnection.On<GameStateDto>("GameStateUpdated", (state) =>
{
    Console.Clear();
    Console.WriteLine("Stride Native Client - Multiplayer Mode");
    Console.WriteLine("========================================");
    Console.WriteLine($"Connected as: {playerId}");
    Console.WriteLine();
    
    Console.WriteLine("All Players:");
    foreach (var player in state.Players)
    {
        var marker = player.Id == playerId ? ">>> " : "    ";
        Console.WriteLine($"{marker}{player.Name} - HP: {player.Health}/{player.MaxHealth} - Pos: ({player.PositionX:F1}, {player.PositionY:F1})");
    }
    
    Console.WriteLine();
    Console.WriteLine($"Server AI Position: ({state.AIPosition.X:F1}, {state.AIPosition.Y:F1})");
    Console.WriteLine();
    Console.WriteLine("Commands: W/A/S/D (move), H (damage), J (heal), U (update AI), Q (quit)");
});

try
{
    await hubConnection.StartAsync();
    Console.WriteLine("Connecting to server...");
    
    // Wait for connection
    await Task.Delay(1000);
    
    // Game loop
    var running = true;
    while (running)
    {
        var key = Console.ReadKey(true);
        
        switch (key.Key)
        {
            case ConsoleKey.W:
                await hubConnection.SendAsync("MovePlayer", 0f, -1f);
                break;
            case ConsoleKey.S:
                await hubConnection.SendAsync("MovePlayer", 0f, 1f);
                break;
            case ConsoleKey.A:
                await hubConnection.SendAsync("MovePlayer", -1f, 0f);
                break;
            case ConsoleKey.D:
                await hubConnection.SendAsync("MovePlayer", 1f, 0f);
                break;
            case ConsoleKey.H:
                await hubConnection.SendAsync("TakeDamage", 10);
                break;
            case ConsoleKey.J:
                await hubConnection.SendAsync("Heal", 20);
                break;
            case ConsoleKey.U:
                await hubConnection.SendAsync("UpdateAI");
                break;
            case ConsoleKey.Q:
                running = false;
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Make sure the server is running on http://localhost:5200");
}
finally
{
    await hubConnection.DisposeAsync();
    Console.WriteLine("Disconnected.");
}

// DTOs matching server
public class GameStateDto
{
    public List<PlayerDto> Players { get; set; } = new();
    public PositionDto AIPosition { get; set; } = new();
}

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

public class PositionDto
{
    public float X { get; set; }
    public float Y { get; set; }
}
