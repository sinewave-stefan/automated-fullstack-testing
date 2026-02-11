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
│   │   └── Vector2D.cs        # 2D vector math
│   ├── StrideApp/             # Native desktop rendering (Stride engine)
│   │   └── Program.cs         # References Core library
│   └── WebApp/                # Web rendering (Blazor WASM + WebGL)
│       ├── Pages/Game.razor   # Interactive game demo using Core library
│       └── Program.cs         # References Core library
└── tests/
    ├── UnitTests/             # Unit tests for Core library
    │   ├── PlayerTests.cs
    │   ├── PhysicsTests.cs
    │   ├── AITests.cs
    │   └── Vector2DTests.cs
    └── Integration/           # Platform integration tests
        └── GameIntegrationTests.cs
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

#### Native App (Console Placeholder)
```bash
dotnet run --project Game/StrideApp/Game.StrideApp.csproj
```

#### Web App (Blazor WASM)
```bash
dotnet run --project Game/WebApp/Game.WebApp.csproj
```

Then navigate to `https://localhost:5001/game` to see the interactive game demo.

## 🧪 Testing

The project includes comprehensive tests demonstrating code sharing:

### Unit Tests
Located in `tests/UnitTests/`, these test the Core library in isolation:
- `PlayerTests.cs` - Player entity behavior
- `PhysicsTests.cs` - Physics calculations
- `AITests.cs` - AI decision making
- `Vector2DTests.cs` - Vector math operations

### Integration Tests
Located in `tests/Integration/`, these demonstrate how the same core logic works across platforms:
- Complete game scenarios
- Multi-step physics simulations
- Cross-platform collision detection

Run tests with:
```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/UnitTests/Game.UnitTests.csproj
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

