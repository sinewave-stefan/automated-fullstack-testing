# Setting Up Stride Integration Tests

## Current Status

The Stride integration tests (`tests/TestFrameworkTests/StrideIntegrationTests.cs`) currently fail because they require full Stride game initialization, which needs a `GameContext`. The current implementation uses `InitializeForTesting()` which only sets up game state objects (Player, AI) without initializing Stride's rendering/graphics systems.

## What's Needed

To make Stride integration tests run with real Stride instances, you need to:

### 1. Create a GameContext

Stride's `Game.Initialize()` method requires a `GameContext` parameter. This is typically created automatically when you call `game.Run()`, but for testing we need to create it manually.

### 2. Platform-Specific GameContext Creation

The `GameContext` type varies by platform:

#### Windows
```csharp
using Stride.Games;

var gameContext = new GameContextWindows(null, 800, 600, "Test");
```

#### Linux
```csharp
// May need GameContextSDL or similar
// Check Stride.Games namespace for available implementations
```

#### Headless/CI Environments
For CI/CD environments without a display, you may need:
- A null window implementation
- Headless graphics context
- Or skip graphics initialization entirely

### 3. Implementation Steps

Update `StrideTestBridge.CreateTestInstance()` to:

```csharp
public static StrideTestBridge CreateTestInstance()
{
    var game = new MultiplayerGame
    {
        IsTestMode = true
    };
    
    // Create GameContext (platform-specific)
    GameContext gameContext;
    
    #if WINDOWS
        gameContext = new GameContextWindows(null, 800, 600, "Test");
    #elif LINUX
        // Use appropriate Linux context
        gameContext = new GameContextSDL(...);
    #else
        // Fallback or headless mode
        throw new PlatformNotSupportedException("GameContext creation not implemented for this platform");
    #endif
    
    // Initialize the game with the context
    game.Initialize(gameContext);
    
    // Trigger BeginRun to set up game systems
    game.TestBeginRun();
    
    return new StrideTestBridge(game);
}
```

### 4. Alternative: Headless Mode

If you want to avoid window creation entirely, you could:

1. **Skip Graphics Initialization**: Modify `MultiplayerGame` to skip graphics systems when in test mode
2. **Use Null Window**: Create a GameContext with a null window handle
3. **Mock Systems**: Mock or stub systems that require graphics (InputManager, etc.)

Example approach:
```csharp
// In MultiplayerGame, override Initialize to skip graphics in test mode
protected override void Initialize()
{
    if (IsTestMode)
    {
        // Skip base initialization which requires GameContext
        // Manually initialize only what we need
        return;
    }
    base.Initialize();
}
```

### 5. Current Workaround

The current implementation (`InitializeForTesting()`) works for:
- ✅ Testing game logic (Player movement, damage, healing)
- ✅ Testing AI decision-making
- ✅ Testing state management

But does NOT work for:
- ❌ Testing rendering initialization
- ❌ Testing camera setup
- ❌ Testing scene management
- ❌ Testing graphics compositor

### 6. Recommended Approach

**Option A: Full Initialization (Recommended for CI/CD)**
- Create a headless/null GameContext
- Initialize all systems
- Run full integration tests

**Option B: Conditional Initialization**
- Check if GameContext is available
- If yes, do full initialization
- If no, use minimal initialization (current approach)

**Option C: Separate Test Suites**
- Keep current minimal tests for logic
- Add separate integration tests that require full initialization
- Mark full integration tests as requiring special setup

### 7. Example: Windows Implementation

```csharp
using Stride.Games;

public static StrideTestBridge CreateTestInstance()
{
    var game = new MultiplayerGame
    {
        IsTestMode = true
    };
    
    try
    {
        // Try to create a GameContext
        var gameContext = new GameContextWindows(null, 800, 600, "Test");
        game.Initialize(gameContext);
        game.TestBeginRun();
    }
    catch (PlatformNotSupportedException)
    {
        // Fallback to minimal initialization
        game.InitializeForTesting();
    }
    
    return new StrideTestBridge(game);
}
```

### 8. CI/CD Considerations

For GitHub Actions or other CI environments:

1. **Install Display Server**: May need Xvfb or similar for headless graphics
2. **Use Headless Context**: Create a GameContext that doesn't require a window
3. **Skip Graphics Tests**: Mark rendering tests as requiring a display
4. **Use Docker**: Run tests in a container with graphics support

Example GitHub Actions setup:
```yaml
- name: Setup Xvfb
  run: |
    sudo apt-get update
    sudo apt-get install -y xvfb

- name: Run Stride Integration Tests
  run: |
    xvfb-run -a dotnet test --filter "FullyQualifiedName~StrideIntegrationTests"
```

## Next Steps

1. **Research Stride's GameContext API**: Check Stride documentation for headless/null window support
2. **Implement Platform Detection**: Add platform-specific GameContext creation
3. **Add Fallback Logic**: Keep current minimal initialization as fallback
4. **Update Tests**: Mark tests that require full initialization
5. **Update CI/CD**: Add necessary setup for graphics/display if needed

## References

- Stride Engine Documentation: https://doc.stride3d.net/
- Stride GitHub: https://github.com/stride3d/stride
- GameContext API: Check `Stride.Games` namespace
