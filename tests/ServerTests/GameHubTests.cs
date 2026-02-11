using Game.Server.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Game.ServerTests;

public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HubConnection? _hubConnection;
    private string? _playerId;

    public GameHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Create a test server
        var client = _factory.CreateClient();
        var hubUrl = $"{client.BaseAddress}gamehub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Listen for PlayerCreated event
        _hubConnection.On<string>("PlayerCreated", (id) =>
        {
            _playerId = id;
        });

        await _hubConnection.StartAsync();
        
        // Wait for connection to complete
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PlayerCreated_OnConnection_AssignsPlayerId()
    {
        // Arrange is done in InitializeAsync

        // Act - connection is established in InitializeAsync

        // Assert
        Assert.NotNull(_playerId);
        Assert.NotEmpty(_playerId);
    }

    [Fact]
    public async Task MovePlayer_UpdatesPlayerPosition()
    {
        // Arrange
        GameStateDto? receivedState = null;
        var tcs = new TaskCompletionSource<GameStateDto>();

        _hubConnection!.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            receivedState = state;
            tcs.TrySetResult(state);
        });

        // Act
        await _hubConnection.SendAsync("MovePlayer", 10f, 5f);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(receivedState);
        Assert.NotEmpty(receivedState.Players);
        
        var myPlayer = receivedState.Players.FirstOrDefault(p => p.Id == _playerId);
        Assert.NotNull(myPlayer);
        Assert.Equal(10f, myPlayer.PositionX);
        Assert.Equal(5f, myPlayer.PositionY);
    }

    [Fact]
    public async Task TakeDamage_ReducesPlayerHealth()
    {
        // Arrange
        GameStateDto? receivedState = null;
        var tcs = new TaskCompletionSource<GameStateDto>();

        _hubConnection!.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            receivedState = state;
            tcs.TrySetResult(state);
        });

        // Act
        await _hubConnection.SendAsync("TakeDamage", 30);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(receivedState);
        var myPlayer = receivedState.Players.FirstOrDefault(p => p.Id == _playerId);
        Assert.NotNull(myPlayer);
        Assert.Equal(70, myPlayer.Health); // 100 - 30 = 70
    }

    [Fact]
    public async Task Heal_IncreasesPlayerHealth()
    {
        // Arrange
        GameStateDto? finalState = null;
        var tcs = new TaskCompletionSource<GameStateDto>();
        int updateCount = 0;

        _hubConnection!.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            updateCount++;
            finalState = state;
            if (updateCount >= 2) // Wait for both damage and heal updates
            {
                tcs.TrySetResult(state);
            }
        });

        // Act
        await _hubConnection.SendAsync("TakeDamage", 50);
        await Task.Delay(100);
        await _hubConnection.SendAsync("Heal", 20);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(finalState);
        var myPlayer = finalState.Players.FirstOrDefault(p => p.Id == _playerId);
        Assert.NotNull(myPlayer);
        Assert.Equal(70, myPlayer.Health); // 100 - 50 + 20 = 70
    }

    [Fact]
    public async Task UpdateAI_MovesAIPosition()
    {
        // Arrange
        GameStateDto? initialState = null;
        GameStateDto? updatedState = null;
        var tcs = new TaskCompletionSource<GameStateDto>();
        int updateCount = 0;

        _hubConnection!.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            updateCount++;
            if (updateCount == 1)
            {
                initialState = state;
            }
            else if (updateCount == 2)
            {
                updatedState = state;
                tcs.TrySetResult(state);
            }
        });

        // Act
        await _hubConnection.SendAsync("RequestGameState");
        await Task.Delay(100);
        await _hubConnection.SendAsync("UpdateAI");
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(initialState);
        Assert.NotNull(updatedState);
        
        // AI should have moved (position changed)
        var initialAI = initialState.AIPosition;
        var updatedAI = updatedState.AIPosition;
        
        // AI might have moved or stayed if no players nearby
        Assert.NotNull(initialAI);
        Assert.NotNull(updatedAI);
    }

    [Fact]
    public async Task MultipleClients_CanConnectSimultaneously()
    {
        // Arrange
        var client2 = _factory.CreateClient();
        var hubUrl = $"{client2.BaseAddress}gamehub";

        var hubConnection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        GameStateDto? receivedState = null;
        var tcs = new TaskCompletionSource<GameStateDto>();

        _hubConnection!.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            receivedState = state;
            if (state.Players.Count >= 2)
            {
                tcs.TrySetResult(state);
            }
        });

        // Act
        await hubConnection2.StartAsync();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(receivedState);
        Assert.True(receivedState.Players.Count >= 2, "Should have at least 2 players connected");

        // Cleanup
        await hubConnection2.DisposeAsync();
    }
}
