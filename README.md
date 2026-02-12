# Automated Fullstack Testing

A demonstration project showing how to maximize code reuse between native (Stride) and web (Blazor WebAssembly) game builds using shared C# core logic.

## 🎯 Project Goals

- **Single Language**: C# / .NET for all code
- **Maximum Code Reuse**: Share game logic, physics, AI, and tests between native and web builds
- **Minimal Duplication**: Only rendering layers are platform-specific
- **Deterministic Testing**: Core logic is fully testable and produces identical results across platforms

## 📁 Project Structure

```
/
├── .github/workflows/
│   └── ci.yml                  # CI/CD pipeline
├── Game/
│   ├── Core/                   # ⭐ Shared C# library (single source of truth)
│   │   ├── Player.cs          # Player entity with health and position
│   │   ├── Physics.cs         # Platform-independent physics calculations
│   │   ├── AI.cs              # AI decision making and behaviors
│   │   ├── Vector2D.cs        # 2D vector math
│   │   └── Testing/           # 🧪 Unified test framework
│   │       ├── ITestBridge.cs       # Test control interface
│   │       ├── TestScenario.cs      # Fluent API for test authoring
│   │       ├── TestSnapshot.cs      # Game state capture
│   │       ├── TestCommand.cs       # Platform-agnostic commands
│   │       ├── TestSpec.cs          # JSON test specification (optional)
│   │       ├── TestSpecExecutor.cs  # JSON test executor (optional)
│   │       └── InMemoryTestBridge.cs # Reference implementation
│   ├── Server/                # 🌐 Realtime game server (SignalR)
│   │   ├── Hubs/GameHub.cs   # SignalR hub for client-server communication
│   │   └── Program.cs         # ASP.NET Core server configuration
│   ├── StrideApp/             # 🎮 Native desktop client (Stride 3D engine)
│   │   └── Program.cs         # Full Stride game with 3D rendering
│   └── WebApp/                # 🌍 Web client (Blazor WASM)
│       ├── Pages/Game.razor   # Single-player game demo
│       ├── Pages/Multiplayer.razor # Multiplayer client
│       └── Program.cs         # Blazor configuration
└── tests/
    ├── UnitTests/             # Unit tests for Core library
    │   ├── PlayerTests.cs
    │   ├── PhysicsTests.cs
    │   ├── AITests.cs
    │   └── Vector2DTests.cs
    ├── Integration/           # Platform integration tests
    │   └── GameIntegrationTests.cs
    ├── ServerTests/           # 🧪 Server integration tests
    │   └── GameHubTests.cs    # SignalR hub tests
    ├── TestFrameworkTests/    # ⚙️ Unified test framework validation
    │   ├── ScenarioApiTests.cs # Fluent API tests
    │   └── TestFrameworkTests.cs # Infrastructure tests
    └── TestSpecs/             # 📋 Test framework documentation
        ├── README.md          # Test framework overview
        └── FLUENT_API_EXAMPLES.md # Fluent API examples
```

## 🏗️ Architecture

### Layer Separation

| Layer | Implementation | Notes |
|-------|---------------|-------|
| **Core Gameplay Logic** | .NET Standard / .NET 8+ library | Physics, AI, rules, scoring, RNG — fully testable |
| **Native Rendering** | Stride project references Core | Desktop build (placeholder) |
| **Web Rendering** | Blazor WASM + WebGL | Browser build references same Core library |

### Key Principles

✅ **Core library** = Single source of truth for all game logic  
✅ **Only rendering code** is duplicated between platforms  
✅ **All tests** run on the same shared logic  
✅ **Deterministic** behavior across native and web builds  
✅ **Realtime server** enables multiplayer across web and native clients

### Client-Server Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│  Web Client     │         │  Game Server     │         │ Native Client   │
│  (Blazor WASM)  │◄────────┤  (SignalR Hub)   ├────────►│ (Console/Stride)│
│                 │  WebSocket  Uses Core     │  WebSocket │                 │
│  Uses Core ✅   │         │  Library ✅      │         │  Uses Core ✅   │
└─────────────────┘         └──────────────────┘         └─────────────────┘
```

**Server Features:**
- SignalR hub for realtime bidirectional communication
- Manages game state using shared Core library
- Broadcasts updates to all connected clients
- Supports multiple simultaneous players
- Server-side AI using same Core.AI logic

**Client Features:**
- Both web and native clients use identical Core library
- Real-time synchronization of player positions and health
- Shared AI behavior visible to all clients
- Platform-specific rendering only

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later

### Build the Project

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Run the Applications

#### 🌐 Multiplayer Server (Required for multiplayer)
```bash
dotnet run --project Game/Server/Game.Server.csproj --urls "http://localhost:5200"
```

The server runs on `http://localhost:5200` and provides the `/gamehub` SignalR endpoint.

#### 🎮 Native Client (Stride 3D Engine)
```bash
dotnet run --project Game/StrideApp/Game.StrideApp.csproj --configuration Release
```

**Features:**
- Full 3D rendering using Stride game engine
- Real-time multiplayer with 3D visualization of all players
- Interactive controls: WASD (move), H (damage), J (heal), U (update AI), ESC (quit)
- Blue cube = Your player, Green cubes = Other players, Red cube = Server AI
- Connects to multiplayer server for synchronized gameplay

#### 🌍 Web App (Blazor WASM)
```bash
dotnet run --project Game/WebApp/Game.WebApp.csproj
```

Then navigate to:
- `http://localhost:5000/game` - Single-player demo (local only)
- `http://localhost:5000/multiplayer` - Multiplayer client (connects to server)

## 🧪 Testing

The project includes comprehensive tests demonstrating code sharing:

### Unit Tests
Located in `tests/UnitTests/`, these test the Core library in isolation:
- `PlayerTests.cs` - Player entity behavior (8 tests)
- `PhysicsTests.cs` - Physics calculations (5 tests)
- `AITests.cs` - AI decision making (7 tests)
- `Vector2DTests.cs` - Vector math operations (6 tests)

### Integration Tests
Located in `tests/Integration/`, these demonstrate how the same core logic works across platforms:
- Complete game scenarios
- Multi-step physics simulations
- Cross-platform collision detection

### Server Tests
Located in `tests/ServerTests/`, these test the SignalR server and client-server communication:
- `GameHubTests.cs` - SignalR hub integration tests (6 tests)
  - Player connection and creation
  - Movement synchronization
  - Health management
  - AI updates
  - Multiple simultaneous clients

### Unified Test Framework (NEW)

The project now includes a **unified test framework** that allows writing platform-agnostic tests that can run on both browser (Blazor) and native (Stride) builds.

**Components:**
- **ITestBridge** - Common interface for test control across platforms
- **TestScenario** - Fluent API for writing readable test scenarios
- **TestSnapshot** - Platform-agnostic state capture
- **InMemoryTestBridge** - Reference implementation for testing

**Fluent API Example:**
```csharp
var scenario = new TestScenario(bridge);

var warrior = scenario.Player("Warrior", x: 0, y: 0, health: 100);

warrior.Move(10, 5).ThenStep();
scenario.Assert.Player(warrior).HasPosition(10, 5);

warrior.TakeDamage(30).ThenStep();
scenario.Assert.Player(warrior).HasHealth(70).IsAlive();
```

**Cross-Platform Testing:**
```csharp
// Same test code runs on any platform implementing ITestBridge
private void TestPlayerMovement(ITestBridge bridge)
{
    var scenario = new TestScenario(bridge);
    var player = scenario.Player("Hero", x: 0, y: 0);
    player.Move(10, 5).ThenStep();
    scenario.Assert.Player(player).HasPosition(10, 5);
}

[Fact] void Test_InMemory() => TestPlayerMovement(new InMemoryTestBridge());
[Fact] void Test_Browser() => TestPlayerMovement(new BrowserTestBridge());
[Fact] void Test_Stride() => TestPlayerMovement(new StrideTestBridge());
```

**Running Tests:**
```bash
# Run fluent API tests
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj
```

See `tests/TestSpecs/FLUENT_API_EXAMPLES.md` for comprehensive examples.

Run tests with:
```bash
# All tests (48 total: 26 unit + 3 integration + 6 server + 13 framework)
dotnet test

# Specific test project
dotnet test tests/UnitTests/Game.UnitTests.csproj
dotnet test tests/ServerTests/Game.ServerTests.csproj
```

## 📊 CI/CD Pipeline

The project includes a GitHub Actions workflow (`.github/workflows/ci.yml`) that:

1. ✅ Builds all projects
2. ✅ Runs all tests (unit + integration)
3. 📦 Publishes both native and web builds
4. 🚀 Prepares artifacts for deployment

## 🎮 Core Library Features

### Player System
- Health management (damage, healing)
- Position tracking and movement
- State queries (alive/dead)

### Physics Engine
- Position updates based on velocity
- Friction application
- Collision detection (circular entities)

### AI System
- Seek/flee behaviors
- Health-based decision making
- Deterministic random generation (for testing)

### Math Library
- 2D vector operations
- Normalization, distance calculations
- Operator overloading for clean syntax

## 🔧 Development Workflow

1. **Write core logic** in `Game/Core/` (platform-independent C#)
2. **Add tests** in `tests/UnitTests/` or `tests/Integration/`
3. **Implement rendering** in platform-specific projects:
   - `Game/StrideApp/` for native
   - `Game/WebApp/` for web
4. **Verify** that tests pass on both platforms

## 📝 Next Steps

To expand this project:

1. **Add actual Stride integration** in `StrideApp/`
2. **Enhance WebGL rendering** in `WebApp/Pages/Game.razor`
3. **Add more game systems** (inventory, combat, etc.) to Core
4. **Implement snapshot testing** for deterministic state comparison
5. **Set up deployment** to GitHub Pages or Azure Static Web Apps

## 🤝 Contributing

This is a demonstration project. Feel free to use it as a template for your own cross-platform game development!

## 📄 License

This project is provided as-is for educational and demonstration purposes.

## 🔗 References

- [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- [Stride Game Engine](https://www.stride3d.net/)
- [.NET Testing](https://docs.microsoft.com/dotnet/core/testing/)

