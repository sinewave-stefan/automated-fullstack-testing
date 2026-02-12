using Game.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Stride.Engine;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Input;
using Stride.Games;
using Stride.Rendering.Compositing;

/// <summary>
/// Stride multiplayer game client that connects to the game server
/// and renders all players and AI in 3D using simple cube models
/// </summary>
class MultiplayerGame : Stride.Engine.Game
{
    private HubConnection? hubConnection;
    private string? playerId;
    private List<PlayerDto> allPlayers = new();
    private PositionDto aiPosition = new() { X = 0, Y = 0 };
    private Dictionary<string, Entity> playerEntities = new();
    private Entity? aiEntity;
    private Entity? cameraEntity;

    protected override void BeginRun()
    {
        base.BeginRun();

        // Initialize GraphicsCompositor with camera slots
        InitializeGraphicsCompositor();

        // Setup camera
        cameraEntity = new Entity("Camera")
        {
            new CameraComponent
            {
                Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId()
            }
        };
        cameraEntity.Transform.Position = new Vector3(0, 10, 20);
        cameraEntity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30));
        SceneSystem.SceneInstance.RootScene.Entities.Add(cameraEntity);

        // Add AI entity
        aiEntity = CreateSimpleEntity("AI", Color.Red);
        SceneSystem.SceneInstance.RootScene.Entities.Add(aiEntity);

        // Connect to multiplayer server (async)
        _ = ConnectToServer();
    }

    private void InitializeGraphicsCompositor()
    {
        // Create a basic graphics compositor with camera slot
        var compositor = new GraphicsCompositor();
        
        // Add a camera slot
        var cameraSlot = new SceneCameraSlot();
        compositor.Cameras.Add(cameraSlot);

        // Create a simple forward renderer
        var opaqueRenderStage = new RenderStage("Opaque", "Main");
        compositor.RenderStages.Add(opaqueRenderStage);

        var transparentRenderStage = new RenderStage("Transparent", "Main");
        compositor.RenderStages.Add(transparentRenderStage);

        // Create render features
        var meshRenderFeature = new MeshRenderFeature();
        meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
        {
            EffectName = "StrideForwardShadingEffect",
            RenderStage = opaqueRenderStage
        });
        meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
        {
            EffectName = "StrideForwardShadingEffect.Transparent",
            RenderStage = transparentRenderStage
        });
        compositor.RenderFeatures.Add(meshRenderFeature);

        // Create a simple forward renderer
        var sceneRenderer = new SceneCameraRenderer
        {
            Mode = new CameraRendererModeForward(),
            Camera = cameraSlot,
            Child = new SceneRendererCollection
            {
                new ClearRenderFrameRenderer
                {
                    Color = Color.CornflowerBlue,
                    Name = "Clear"
                },
                new RenderStageRenderer(opaqueRenderStage),
                new RenderStageRenderer(transparentRenderStage)
            }
        };

        compositor.SingleView = sceneRenderer;

        SceneSystem.GraphicsCompositor = compositor;
    }

    private async Task ConnectToServer()
    {
        try
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5200/gamehub")
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<string>("PlayerCreated", (connectionId) =>
            {
                playerId = connectionId;
                Window.Title = $"Stride Multiplayer - {playerId[..8]}";
            });

            hubConnection.On<GameStateDto>("GameStateUpdated", (state) =>
            {
                allPlayers = state.Players;
                aiPosition = state.AIPosition;
                UpdateGameEntities();
            });

            await hubConnection.StartAsync();
            Window.Title = "Stride Multiplayer - Connecting...";
        }
        catch (Exception ex)
        {
            Window.Title = $"Stride - Connection Failed: {ex.Message}";
        }
    }

    private void UpdateGameEntities()
    {
        // Update AI position
        if (aiEntity != null)
        {
            aiEntity.Transform.Position = new Vector3(aiPosition.X, 0, aiPosition.Y);
        }

        // Update or create player entities
        var currentPlayerIds = new HashSet<string>(allPlayers.Select(p => p.Id));
        
        // Remove disconnected players
        var toRemove = playerEntities.Keys.Where(id => !currentPlayerIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            if (playerEntities.TryGetValue(id, out var entity))
            {
                SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
                playerEntities.Remove(id);
            }
        }

        // Update existing or create new player entities
        foreach (var player in allPlayers)
        {
            if (!playerEntities.ContainsKey(player.Id))
            {
                var color = player.Id == playerId ? Color.Blue : Color.Green;
                var entity = CreateSimpleEntity(player.Name, color);
                playerEntities[player.Id] = entity;
                SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            }

            var playerEntity = playerEntities[player.Id];
            playerEntity.Transform.Position = new Vector3(player.PositionX, 0, player.PositionY);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (hubConnection?.State == HubConnectionState.Connected && playerId != null)
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        var input = Input;
        
        // Movement with WASD
        if (input.IsKeyPressed(Keys.W))
        {
            _ = hubConnection?.SendAsync("MovePlayer", 0f, -1f);
        }
        if (input.IsKeyPressed(Keys.S))
        {
            _ = hubConnection?.SendAsync("MovePlayer", 0f, 1f);
        }
        if (input.IsKeyPressed(Keys.A))
        {
            _ = hubConnection?.SendAsync("MovePlayer", -1f, 0f);
        }
        if (input.IsKeyPressed(Keys.D))
        {
            _ = hubConnection?.SendAsync("MovePlayer", 1f, 0f);
        }

        // Health management
        if (input.IsKeyPressed(Keys.H))
        {
            _ = hubConnection?.SendAsync("TakeDamage", 10);
        }
        if (input.IsKeyPressed(Keys.J))
        {
            _ = hubConnection?.SendAsync("Heal", 20);
        }

        // Update AI
        if (input.IsKeyPressed(Keys.U))
        {
            _ = hubConnection?.SendAsync("UpdateAI");
        }

        // Quit
        if (input.IsKeyPressed(Keys.Escape))
        {
            Exit();
        }
    }

    private Entity CreateSimpleEntity(string name, Color color)
    {
        // Create simple material
        var material = Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature()
            }
        });

        // Create simple cube mesh manually
        var mesh = CreateCubeMesh();
        
        var model = new Model
        {
            mesh
        };
        model.Materials.Add(material);

        var entity = new Entity(name)
        {
            new ModelComponent(model)
        };

        return entity;
    }

    private Mesh CreateCubeMesh()
    {
        // Create a simple cube mesh
        var vertices = new VertexPositionNormalTexture[]
        {
            // Front face
            new VertexPositionNormalTexture(new Vector3(-0.5f, -0.5f, -0.5f), Vector3.UnitZ, Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3(-0.5f,  0.5f, -0.5f), Vector3.UnitZ, Vector2.UnitY),
            new VertexPositionNormalTexture(new Vector3( 0.5f,  0.5f, -0.5f), Vector3.UnitZ, Vector2.One),
            new VertexPositionNormalTexture(new Vector3( 0.5f, -0.5f, -0.5f), Vector3.UnitZ, Vector2.UnitX),
        };

        var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, vertices);
        var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices);

        var meshDraw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
            IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
            DrawCount = indices.Length
        };

        return new Mesh { Draw = meshDraw, MaterialIndex = 0 };
    }

    protected override void Destroy()
    {
        hubConnection?.DisposeAsync().AsTask().Wait();
        base.Destroy();
    }
}

static class Program
{
    static void Main()
    {
        using (var game = new MultiplayerGame())
        {
            game.Run();
        }
    }
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
