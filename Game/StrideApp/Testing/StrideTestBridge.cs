using Game.Core.Testing;
using Stride.Engine;
using Stride.Core.Mathematics;

namespace Game.StrideApp.Testing;

/// <summary>
/// Test bridge implementation for Stride game engine.
/// Allows tests to control and inspect the Stride game through the platform-agnostic ITestBridge interface.
/// </summary>
public class StrideTestBridge : ITestBridge
{
    private readonly MultiplayerGame _game;
    private int _currentFrame;

    public StrideTestBridge(MultiplayerGame game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public bool IsTestMode => true;

    public void Step()
    {
        _currentFrame++;
        // In a full implementation, this would step the game's Update loop
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
        // Reset game state - would clear all entities and reinitialize
    }

    public TestSnapshot GetSnapshot()
    {
        var snapshot = new TestSnapshot
        {
            Frame = _currentFrame
        };

        // Capture rendering state
        var compositor = _game.SceneSystem?.GraphicsCompositor;
        snapshot.Rendering = new RenderingSnapshot
        {
            IsInitialized = compositor != null,
            CameraSlotCount = compositor?.Cameras.Count ?? 0,
            RenderStageCount = compositor?.RenderStages.Count ?? 0,
            Width = _game.GraphicsDevice?.Presenter?.BackBuffer?.Width ?? 0,
            Height = _game.GraphicsDevice?.Presenter?.BackBuffer?.Height ?? 0
        };

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
                        CurrentDecision = "Idle"
                    });
                }
                else
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
            case TestCommandType.InitializeRendering:
                // Rendering is already initialized by the game
                break;

            case TestCommandType.Spawn:
                // Entity spawning would be implemented here
                break;

            case TestCommandType.SetActiveCamera:
                // Camera activation would be implemented here
                break;

            default:
                // Other commands not yet implemented for Stride
                break;
        }
    }
}
