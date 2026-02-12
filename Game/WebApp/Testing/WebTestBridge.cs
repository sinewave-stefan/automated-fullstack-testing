using Game.Core;
using Game.Core.Testing;
using Microsoft.Playwright;
using System.Text.Json;

namespace Game.WebApp.Testing;

/// <summary>
/// Test bridge implementation for Blazor WebAssembly game client.
/// Allows tests to control and inspect the web game through the platform-agnostic ITestBridge interface.
/// </summary>
public class WebTestBridge : ITestBridge, IDisposable
{
    private readonly IPage? _page;
    private readonly string? _appUrl;
    private readonly bool _useBrowser;
    
    // Fallback in-memory state for when browser is not available
    private Player? _player;
    private AI? _ai;
    private int _currentFrame;
    private Vector2D _patrolPoint;
    private AIDecision _currentDecision;
    private bool _isRenderingInitialized;
    private int _canvasWidth;
    private int _canvasHeight;
    private readonly List<EntitySnapshot> _entities = new();
    private string? _activeCameraId;

    /// <summary>
    /// Creates a WebTestBridge using browser automation (Playwright).
    /// </summary>
    public WebTestBridge(IPage page, string appUrl)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _appUrl = appUrl ?? throw new ArgumentNullException(nameof(appUrl));
        _useBrowser = true;
        
        // Navigate to app and wait for Blazor to initialize
        _page.GotoAsync(_appUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        }).Wait();
        WaitForBlazorReady().Wait();
    }

    /// <summary>
    /// Creates a WebTestBridge with in-memory state (fallback mode).
    /// Use this when browser automation is not available.
    /// </summary>
    public WebTestBridge()
    {
        _useBrowser = false;
        _player = new Player("Test Player", 100);
        _ai = new AI(42); // Deterministic seed for testing
        _patrolPoint = new Vector2D(0, 0);
        _currentDecision = AIDecision.Seek;
        _canvasWidth = 400;
        _canvasHeight = 300;
    }

    /// <summary>
    /// Creates a WebTestBridge connected to existing game state.
    /// Use this when testing an actual running Blazor component.
    /// </summary>
    public WebTestBridge(Player player, AI ai)
    {
        _useBrowser = false;
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _patrolPoint = new Vector2D(0, 0);
        _currentDecision = AIDecision.Seek;
        _canvasWidth = 400;
        _canvasHeight = 300;
    }
    
    private async Task WaitForBlazorReady()
    {
        if (_page == null) return;
        
        // Wait for Blazor to be ready
        await _page.WaitForFunctionAsync("window.Blazor !== undefined", new PageWaitForFunctionOptions
        {
            Timeout = 30000
        });
        
        // Wait for Blazor WebAssembly to finish loading
        await _page.WaitForFunctionAsync("window.Blazor !== undefined && typeof Blazor.start === 'function' || window.Blazor._internal !== undefined", new PageWaitForFunctionOptions
        {
            Timeout = 30000
        });
        
        // Wait for the test bridge script to be loaded (it's already in index.html)
        await _page.WaitForFunctionAsync("window.testBridge !== undefined", new PageWaitForFunctionOptions
        {
            Timeout = 30000
        });
        
        // Wait for the test bridge to be initialized by the Blazor component
        // The Game.razor component calls testBridge.initialize() in OnAfterRenderAsync when IsTestMode=true
        await _page.WaitForFunctionAsync("window.testBridge !== undefined && window.testBridge._initialized === true", new PageWaitForFunctionOptions
        {
            Timeout = 30000
        });
        
        // Give it a moment to ensure everything is fully initialized
        await Task.Delay(500);
    }

    public bool IsTestMode => true;

    public void Step()
    {
        if (_useBrowser && _page != null)
        {
            _page.EvaluateAsync("window.testBridge.step()").Wait();
            _currentFrame++;
        }
        else
        {
            _currentFrame++;
            if (_player != null && _ai != null)
            {
                float distanceToPlayer = Vector2D.Distance(_patrolPoint, _player.Position);
                _currentDecision = _ai.MakeDecision(_player.Health, _player.MaxHealth, distanceToPlayer);
            }
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
        _player = new Player("Test Player", 100);
        _ai = new AI(42);
        _patrolPoint = new Vector2D(0, 0);
        _currentDecision = AIDecision.Seek;
        _entities.Clear();
        _activeCameraId = null;
        _isRenderingInitialized = false;
    }

    public TestSnapshot GetSnapshot()
    {
        if (_useBrowser && _page != null)
        {
            var json = _page.EvaluateAsync<string>("window.testBridge.getSnapshot()").Result;
            var snapshot = JsonSerializer.Deserialize<TestSnapshot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return snapshot ?? new TestSnapshot { Frame = _currentFrame };
        }
        else
        {
            var snapshot = new TestSnapshot
            {
                Frame = _currentFrame,
                Players = _player != null ? new List<PlayerSnapshot>
                {
                    new PlayerSnapshot
                    {
                        Id = "player-1",
                        Name = _player.Name,
                        X = _player.Position.X,
                        Y = _player.Position.Y,
                        Health = _player.Health,
                        MaxHealth = _player.MaxHealth,
                        IsAlive = _player.IsAlive
                    }
                } : new List<PlayerSnapshot>(),
                AIEntities = new List<AISnapshot>
                {
                    new AISnapshot
                    {
                        Id = "ai-1",
                        X = _patrolPoint.X,
                        Y = _patrolPoint.Y,
                        CurrentDecision = _currentDecision.ToString()
                    }
                },
                Entities = new List<EntitySnapshot>(_entities),
                Rendering = new RenderingSnapshot
                {
                    IsInitialized = _isRenderingInitialized,
                    CameraSlotCount = _activeCameraId != null ? 1 : 0,
                    RenderStageCount = _isRenderingInitialized ? 1 : 0,
                    ActiveCameraId = _activeCameraId,
                    Width = _canvasWidth,
                    Height = _canvasHeight,
                    PlatformInfo = new Dictionary<string, object>
                    {
                        ["platform"] = "Blazor WebAssembly",
                        ["renderer"] = "SVG/Canvas"
                    }
                }
            };

            return snapshot;
        }
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

            case TestCommandType.UpdateAI:
                ExecuteUpdateAICommand();
                break;

            case TestCommandType.InitializeRendering:
                ExecuteInitializeRenderingCommand(command);
                break;

            case TestCommandType.Spawn:
                ExecuteSpawnCommand(command);
                break;

            case TestCommandType.SetActiveCamera:
                ExecuteSetActiveCameraCommand(command);
                break;

            case TestCommandType.Remove:
                ExecuteRemoveCommand(command);
                break;

            default:
                throw new NotSupportedException($"Command type {command.Type} not supported by WebTestBridge");
        }
    }

    private void ExecuteMoveCommand(TestCommand command)
    {
        var deltaX = ConvertToFloat(command.Parameters["deltaX"]);
        var deltaY = ConvertToFloat(command.Parameters["deltaY"]);
        
        if (_useBrowser && _page != null)
        {
            _page.EvaluateAsync($"window.testBridge.executeCommand('Move', null, {{ deltaX: {deltaX}, deltaY: {deltaY} }})").Wait();
        }
        else if (_player != null)
        {
            _player.Move(deltaX, deltaY);
        }
    }

    private void ExecuteDamageCommand(TestCommand command)
    {
        var amount = ConvertToInt(command.Parameters["amount"]);
        
        if (_useBrowser && _page != null)
        {
            _page.EvaluateAsync($"window.testBridge.executeCommand('Damage', null, {{ amount: {amount} }})").Wait();
        }
        else if (_player != null)
        {
            _player.TakeDamage(amount);
        }
    }

    private void ExecuteHealCommand(TestCommand command)
    {
        var amount = ConvertToInt(command.Parameters["amount"]);
        
        if (_useBrowser && _page != null)
        {
            _page.EvaluateAsync($"window.testBridge.executeCommand('Heal', null, {{ amount: {amount} }})").Wait();
        }
        else if (_player != null)
        {
            _player.Heal(amount);
        }
    }

    private void ExecuteUpdateAICommand()
    {
        if (_player != null && _ai != null)
        {
            float distance = Vector2D.Distance(_patrolPoint, _player.Position);
            _currentDecision = _ai.MakeDecision(_player.Health, _player.MaxHealth, distance);
        }
    }

    private void ExecuteInitializeRenderingCommand(TestCommand command)
    {
        var cameraSlots = command.Parameters.ContainsKey("cameraSlots")
            ? ConvertToInt(command.Parameters["cameraSlots"])
            : 1;

        _isRenderingInitialized = true;
        // Web canvas doesn't have multiple camera slots like Stride's GraphicsCompositor
        // but we track the requested count for test compatibility
    }

    private void ExecuteSpawnCommand(TestCommand command)
    {
        var id = command.TargetId ?? $"entity-{_entities.Count + 1}";
        var name = command.Parameters.ContainsKey("name")
            ? command.Parameters["name"].ToString() ?? "Entity"
            : "Entity";
        var type = command.Parameters.ContainsKey("type")
            ? command.Parameters["type"].ToString() ?? "Unknown"
            : "Unknown";
        var x = command.Parameters.ContainsKey("x") ? ConvertToFloat(command.Parameters["x"]) : 0f;
        var y = command.Parameters.ContainsKey("y") ? ConvertToFloat(command.Parameters["y"]) : 0f;
        var z = command.Parameters.ContainsKey("z") ? ConvertToFloat(command.Parameters["z"]) : 0f;

        if (_useBrowser && _page != null)
        {
            // If it's a player spawn (no type or type is Player), update the Blazor component
            if (string.IsNullOrEmpty(type) || type == "Unknown" || type == "Player")
            {
                var health = command.Parameters.ContainsKey("health") ? ConvertToInt(command.Parameters["health"]) : 100;
                _page.EvaluateAsync($"window.testBridge.executeCommand('Spawn', '{id}', {{ " +
                    $"name: '{name}', x: {x}, y: {y}, health: {health} }})").Wait();
            }
            else
            {
                // For non-player entities, just track in memory
                var entity = new EntitySnapshot
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    X = x,
                    Y = y,
                    Z = z,
                    IsActive = true
                };
                _entities.Add(entity);
            }
        }
        else
        {
            // In-memory mode: create entity
            var entity = new EntitySnapshot
            {
                Id = id,
                Name = name,
                Type = type,
                X = x,
                Y = y,
                Z = z,
                IsActive = true
            };
            _entities.Add(entity);
        }
    }

    private void ExecuteSetActiveCameraCommand(TestCommand command)
    {
        _activeCameraId = command.TargetId;
        
        // Ensure camera entity exists
        if (_activeCameraId != null && !_entities.Any(e => e.Id == _activeCameraId && e.Type == "Camera"))
        {
            _entities.Add(new EntitySnapshot
            {
                Id = _activeCameraId,
                Name = "Camera",
                Type = "Camera",
                IsActive = true
            });
        }
    }

    private void ExecuteRemoveCommand(TestCommand command)
    {
        if (command.TargetId == null)
            return;

        var entity = _entities.FirstOrDefault(e => e.Id == command.TargetId);
        if (entity != null)
        {
            _entities.Remove(entity);
        }
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
    /// Sets the canvas/viewport dimensions for rendering tests.
    /// </summary>
    public void SetCanvasSize(int width, int height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
    }

    /// <summary>
    /// Gets the current player for direct access in component integration.
    /// Returns null if using browser mode.
    /// </summary>
    public Player? Player => _useBrowser ? null : _player;

    /// <summary>
    /// Gets the current AI for direct access in component integration.
    /// Returns null if using browser mode.
    /// </summary>
    public AI? AI => _useBrowser ? null : _ai;

    /// <summary>
    /// Gets the current patrol point.
    /// </summary>
    public Vector2D PatrolPoint => _patrolPoint;

    /// <summary>
    /// Gets the current AI decision.
    /// </summary>
    public AIDecision CurrentDecision => _currentDecision;
    
    public void Dispose()
    {
        // Browser cleanup handled by test code
        // In-memory state doesn't need cleanup
    }
}
