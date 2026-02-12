using Game.Core;
using Game.Core.Testing;
using Stride.Engine;
using Stride.Core.Mathematics;
using Stride.Games;
using System.Reflection;
using Stride.Graphics;
using Stride.Rendering.Compositing;

namespace Game.StrideApp.Testing;

/// <summary>
/// Test bridge implementation for Stride game engine.
/// Allows tests to control and inspect the Stride game through the platform-agnostic ITestBridge interface.
/// </summary>
public class StrideTestBridge : ITestBridge
{
    private readonly MultiplayerGame _game;
    private int _currentFrame;
    private const double FixedTimestep = 1.0 / 60.0; // 60 FPS fixed timestep
    private string? _currentPlayerId; // Track the ID of the spawned player

    public StrideTestBridge(MultiplayerGame game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        if (!_game.IsTestMode)
        {
            throw new InvalidOperationException("StrideTestBridge requires a game instance in test mode");
        }
    }

    /// <summary>
    /// Creates a test game instance configured for testing.
    /// The game will run in test mode without connecting to a server.
    /// Note: The game instance must be properly disposed after testing.
    /// 
    /// This method attempts to create a fully initialized Stride instance.
    /// On Windows, it creates a GameContextWindows. On other platforms,
    /// it falls back to minimal initialization.
    /// </summary>
    public static StrideTestBridge CreateTestInstance()
    {
        var game = new MultiplayerGame
        {
            IsTestMode = true
        };
        
        // Try to create a fully initialized instance
        try
        {
            GameContext? gameContext = CreateGameContext();
            if (gameContext != null)
            {
                return CreateFullyInitializedInstance(gameContext);
            }
        }
        catch (Exception ex)
        {
            // If GameContext creation fails, fall back to minimal initialization
            // This allows tests to run even without full graphics initialization
            System.Diagnostics.Debug.WriteLine($"Could not create GameContext, using minimal initialization: {ex.Message}");
        }
        
        // Fallback: Initialize test state only (no graphics/rendering)
        // This works for logic tests but not rendering tests
        game.InitializeForTesting();
        
        return new StrideTestBridge(game);
    }
    
    /// <summary>
    /// Creates a platform-specific GameContext for testing.
    /// Returns null if GameContext creation is not supported on this platform.
    /// </summary>
    private static GameContext? CreateGameContext()
    {
        // Try Windows first (most common platform)
        // GameContextWindows should be available in Stride.Games namespace
        try
        {
            // Use reflection to find and create GameContextWindows
            // This avoids compile-time dependency on platform-specific types
            var gamesAssembly = typeof(GameContext).Assembly;
            var gameContextWindowsType = gamesAssembly.GetType("Stride.Games.GameContextWindows");
            
            if (gameContextWindowsType != null)
            {
                // Constructor signature: GameContextWindows(IntPtr controlHandle, int width, int height, string title)
                // IntPtr.Zero means create a new window
                var constructor = gameContextWindowsType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(IntPtr), typeof(int), typeof(int), typeof(string) },
                    null);
                    
                if (constructor != null)
                {
                    // Create a window for testing (will be hidden/offscreen)
                    return (GameContext)constructor.Invoke(new object[] { IntPtr.Zero, 800, 600, "Stride Test" });
                }
            }
        }
        catch (Exception ex)
        {
            // GameContextWindows not available or creation failed
            System.Diagnostics.Debug.WriteLine($"Could not create GameContextWindows: {ex.Message}");
        }
        
        // If we can't create a context, return null to use fallback
        return null;
    }
    
    /// <summary>
    /// Creates a fully initialized Stride game instance for integration testing.
    /// This requires a GameContext which can be created using platform-specific code.
    /// 
    /// Example for Windows:
    ///   var gameContext = new GameContextWindows(null, 800, 600, "Test");
    ///   var bridge = StrideTestBridge.CreateFullyInitializedInstance(gameContext);
    /// 
    /// For headless/CI environments, you may need to use a null window or
    /// a headless graphics context.
    /// 
    /// Note: This uses reflection to call the protected Initialize() method.
    /// </summary>
    public static StrideTestBridge CreateFullyInitializedInstance(GameContext gameContext)
    {
        var game = new MultiplayerGame
        {
            IsTestMode = true
        };
        
        // Initialize the game with the provided context using reflection
        // (Initialize() is protected, so we can't call it directly)
        var initializeMethod = typeof(Stride.Engine.Game).GetMethod("Initialize", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (initializeMethod != null)
        {
            initializeMethod.Invoke(game, new object[] { gameContext });
        }
        else
        {
            throw new InvalidOperationException("Could not find Initialize method via reflection");
        }
        
        // Trigger BeginRun to set up game systems
        game.TestBeginRun();
        
        return new StrideTestBridge(game);
    }
    
    /// <summary>
    /// Disposes the underlying game instance.
    /// Call this when done with testing.
    /// </summary>
    public void Dispose()
    {
        _game?.Dispose();
    }

    public bool IsTestMode => true;

    public void Step()
    {
        // Create GameTime using the correct Stride API
        var totalTime = TimeSpan.FromSeconds(_currentFrame * FixedTimestep);
        var elapsedTime = TimeSpan.FromSeconds(FixedTimestep);
        var gameTime = new GameTime(totalTime, elapsedTime);
        
        // Call Update via reflection or create a public wrapper method
        // Since Update is protected, we'll need to add a public method to MultiplayerGame
        _game.TestUpdate(gameTime);
        _currentFrame++;
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
        _currentPlayerId = null; // Reset tracked player ID
        // Reset game state by clearing scene and reinitializing
        if (_game.SceneSystem?.SceneInstance?.RootScene != null)
        {
            var scene = _game.SceneSystem.SceneInstance.RootScene;
            var entitiesToRemove = scene.Entities.ToList();
            foreach (var entity in entitiesToRemove)
            {
                scene.Entities.Remove(entity);
            }
        }
        // Reinitialize test state
        _game.InitializeTestState();
    }

    public TestSnapshot GetSnapshot()
    {
        var snapshot = new TestSnapshot
        {
            Frame = _currentFrame
        };

        // Capture rendering state
        // Note: SceneSystem may not be initialized in test mode
        var compositor = _game.SceneSystem?.GraphicsCompositor;
        snapshot.Rendering = new RenderingSnapshot
        {
            IsInitialized = compositor != null,
            CameraSlotCount = compositor?.Cameras.Count ?? 0,
            RenderStageCount = compositor?.RenderStages.Count ?? 0,
            Width = _game.GraphicsDevice?.Presenter?.BackBuffer?.Width ?? 0,
            Height = _game.GraphicsDevice?.Presenter?.BackBuffer?.Height ?? 0
        };
        
        // If SceneSystem is not initialized, provide default rendering state
        if (compositor == null)
        {
            snapshot.Rendering.IsInitialized = false;
            snapshot.Rendering.CameraSlotCount = 0;
        }

        // Capture player state from test mode
        // Use the tracked player ID if available, otherwise default to "TestPlayer"
        if (_game.TestPlayer != null)
        {
            snapshot.Players.Add(new PlayerSnapshot
            {
                Id = _currentPlayerId ?? "TestPlayer",
                Name = _game.TestPlayer.Name,
                X = _game.TestPlayer.Position.X,
                Y = _game.TestPlayer.Position.Y,
                Health = _game.TestPlayer.Health,
                MaxHealth = _game.TestPlayer.MaxHealth,
                IsAlive = _game.TestPlayer.IsAlive
            });
        }

        // Capture entities from scene
        if (_game.SceneSystem?.SceneInstance?.RootScene != null)
        {
            var scene = _game.SceneSystem.SceneInstance.RootScene;
            
            foreach (var entity in scene.Entities)
            {
                // Determine entity type
                string entityType = "Unknown";
                if (entity.Get<CameraComponent>() != null)
                {
                    entityType = "Camera";
                    if (snapshot.Rendering.ActiveCameraId == null)
                    {
                        snapshot.Rendering.ActiveCameraId = entity.Name;
                    }
                }
                else if (entity.Get<ModelComponent>() != null)
                {
                    entityType = "Model";
                }

                // Add to appropriate collection
                if (entity.Name.StartsWith("AI"))
                {
                    snapshot.AIEntities.Add(new AISnapshot
                    {
                        Id = entity.Name,
                        X = entity.Transform.Position.X,
                        Y = entity.Transform.Position.Z,
                        CurrentDecision = _game.TestAI != null ? "Idle" : "Unknown"
                    });
                }
                else if (entity.Name != "TestPlayer") // Already captured above
                {
                    snapshot.Entities.Add(new EntitySnapshot
                    {
                        Id = entity.Name,
                        Name = entity.Name,
                        Type = entityType,
                        X = entity.Transform.Position.X,
                        Y = entity.Transform.Position.Y,
                        Z = entity.Transform.Position.Z,
                        IsActive = true
                    });
                }
            }
        }

        return snapshot;
    }

    public void ExecuteCommand(TestCommand command)
    {
        switch (command.Type)
        {
            case TestCommandType.Move:
                // Handle move for the current tracked player (or any player if TestPlayer exists)
                if ((command.TargetId == _currentPlayerId || command.TargetId == "TestPlayer" || _currentPlayerId == null) 
                    && command.Parameters.ContainsKey("deltaX") && command.Parameters.ContainsKey("deltaY"))
                {
                    var deltaX = Convert.ToSingle(command.Parameters["deltaX"]);
                    var deltaY = Convert.ToSingle(command.Parameters["deltaY"]);
                    _game.TestMovePlayer(deltaX, deltaY);
                }
                break;

            case TestCommandType.Damage:
                // Handle damage for the current tracked player
                if ((command.TargetId == _currentPlayerId || command.TargetId == "TestPlayer" || _currentPlayerId == null)
                    && command.Parameters.ContainsKey("amount"))
                {
                    var amount = Convert.ToInt32(command.Parameters["amount"]);
                    _game.TestTakeDamage(amount);
                }
                break;

            case TestCommandType.Heal:
                // Handle heal for the current tracked player
                if ((command.TargetId == _currentPlayerId || command.TargetId == "TestPlayer" || _currentPlayerId == null)
                    && command.Parameters.ContainsKey("amount"))
                {
                    var amount = Convert.ToInt32(command.Parameters["amount"]);
                    _game.TestHeal(amount);
                }
                break;

            case TestCommandType.UpdateAI:
                // AI updates happen automatically in Update loop
                break;

            case TestCommandType.InitializeRendering:
                // Initialize or update the GraphicsCompositor with the requested number of camera slots
                var cameraSlots = command.Parameters.ContainsKey("cameraSlots")
                    ? Convert.ToInt32(command.Parameters["cameraSlots"])
                    : 1;
                
                if (_game.SceneSystem != null)
                {
                    // Get or create GraphicsCompositor
                    var compositor = _game.SceneSystem.GraphicsCompositor;
                    if (compositor == null)
                    {
                        compositor = new GraphicsCompositor();
                        _game.SceneSystem.GraphicsCompositor = compositor;
                    }
                    
                    // Ensure we have the requested number of camera slots
                    while (compositor.Cameras.Count < cameraSlots)
                    {
                        compositor.Cameras.Add(new SceneCameraSlot());
                    }
                    
                    // Remove excess slots if fewer are requested (optional - usually we just add)
                    // For now, we'll leave extra slots as-is
                }
                else
                {
                    // SceneSystem is null - this means the game wasn't fully initialized
                    // This can happen if GameContext creation failed and we fell back to minimal initialization
                    // For rendering tests to work, full initialization is required
                    throw new InvalidOperationException(
                        "Cannot initialize rendering: SceneSystem is null. " +
                        "Rendering initialization requires full game initialization with GameContext. " +
                        "Ensure CreateTestInstance() successfully created a GameContext.");
                }
                break;

            case TestCommandType.Spawn:
                if (command.TargetId != null)
                {
                    var name = command.Parameters.ContainsKey("name") ? command.Parameters["name"].ToString() ?? "Entity" : "Entity";
                    var type = command.Parameters.ContainsKey("type") ? command.Parameters["type"].ToString() ?? "Unknown" : "Unknown";
                    var x = command.Parameters.ContainsKey("x") ? Convert.ToSingle(command.Parameters["x"]) : 0f;
                    var y = command.Parameters.ContainsKey("y") ? Convert.ToSingle(command.Parameters["y"]) : 0f;
                    var z = command.Parameters.ContainsKey("z") ? Convert.ToSingle(command.Parameters["z"]) : 0f;
                    
                    // Check if this is a player spawn (no type specified or type is Player/Unknown)
                    if (string.IsNullOrEmpty(type) || type == "Unknown" || type == "Player")
                    {
                        // This is a player spawn - create/update the game's test player
                        var health = command.Parameters.ContainsKey("health") ? Convert.ToInt32(command.Parameters["health"]) : 100;
                        
                        // Ensure test state is initialized first
                        if (_game.TestPlayer == null)
                        {
                            _game.InitializeTestState();
                        }
                        
                        // Update the player with spawn parameters
                        if (_game.TestPlayer != null)
                        {
                            // Move player to absolute position (x, y)
                            // Player.Move() is relative, so calculate delta from current position
                            var currentX = _game.TestPlayer.Position.X;
                            var currentY = _game.TestPlayer.Position.Y;
                            var deltaX = x - currentX;
                            var deltaY = y - currentY;
                            _game.TestPlayer.Move(deltaX, deltaY);
                            
                            // Set health by resetting to max, then applying damage if needed
                            // First, reset health to max
                            while (_game.TestPlayer.Health < _game.TestPlayer.MaxHealth)
                            {
                                _game.TestPlayer.Heal(_game.TestPlayer.MaxHealth - _game.TestPlayer.Health);
                            }
                            // Then apply damage if needed to reach target health
                            if (health < _game.TestPlayer.MaxHealth)
                            {
                                _game.TestPlayer.TakeDamage(_game.TestPlayer.MaxHealth - health);
                            }
                        }
                        
                        // Track the player ID
                        _currentPlayerId = command.TargetId;
                        
                        // Create or update the entity in the scene
                        if (_game.SceneSystem?.SceneInstance?.RootScene != null)
                        {
                            // Remove old entity if it exists
                            var oldEntity = _game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(e => e.Name == name || e.Name == command.TargetId || e.Name == "TestPlayer");
                            if (oldEntity != null)
                            {
                                _game.SceneSystem.SceneInstance.RootScene.Entities.Remove(oldEntity);
                            }
                            
                            // Create new entity with the spawn command's TargetId as the name
                            var entity = _game.CreateSimpleEntity(command.TargetId, Stride.Core.Mathematics.Color.Blue);
                            entity.Transform.Position = new Stride.Core.Mathematics.Vector3(x, y, z);
                            _game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
                        }
                    }
                    else
                    {
                        // Regular entity spawn
                        if (_game.SceneSystem?.SceneInstance?.RootScene != null)
                        {
                            var entity = _game.CreateSimpleEntity(name, Stride.Core.Mathematics.Color.White);
                            entity.Transform.Position = new Stride.Core.Mathematics.Vector3(x, y, z);
                            _game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
                        }
                    }
                }
                break;

            case TestCommandType.SetActiveCamera:
                if (command.TargetId != null && _game.SceneSystem?.GraphicsCompositor != null)
                {
                    var cameraEntity = _game.SceneSystem.SceneInstance?.RootScene?.Entities.FirstOrDefault(e => e.Name == command.TargetId);
                    if (cameraEntity != null)
                    {
                        var cameraComponent = cameraEntity.Get<CameraComponent>();
                        if (cameraComponent != null && _game.SceneSystem.GraphicsCompositor.Cameras.Count > 0)
                        {
                            var slotIndex = command.Parameters.ContainsKey("slotIndex") ? Convert.ToInt32(command.Parameters["slotIndex"]) : 0;
                            if (slotIndex < _game.SceneSystem.GraphicsCompositor.Cameras.Count)
                            {
                                cameraComponent.Slot = _game.SceneSystem.GraphicsCompositor.Cameras[slotIndex].ToSlotId();
                            }
                        }
                    }
                }
                break;

            case TestCommandType.Remove:
                if (command.TargetId != null && _game.SceneSystem?.SceneInstance?.RootScene != null)
                {
                    var entity = _game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(e => e.Name == command.TargetId);
                    if (entity != null)
                    {
                        _game.SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
                    }
                }
                break;

            default:
                throw new NotSupportedException($"Command type {command.Type} not supported by StrideTestBridge");
        }
    }
}
