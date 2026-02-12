# Running the Test Matrix

The test matrix runs the same tests across multiple platforms/bridges to ensure cross-platform compatibility.

## Test Matrix Overview

The test matrix consists of tests that run against multiple `ITestBridge` implementations:

1. **InMemoryTestBridge** - Fast unit tests, no runtime needed ✅
2. **StrideTestBridge** - Real Stride game engine instances ⚠️ (requires full game initialization - see Stride Integration Tests)
3. **WebTestBridge** - Real Blazor WebAssembly instances (when configured) ⏸️

## Running the Full Test Matrix

### Option 1: Run All Cross-Platform Tests

The cross-platform tests automatically run against all available bridges:

```powershell
# Run cross-platform rendering tests (currently runs against InMemory only)
dotnet test tests/StrideAppTests/Game.StrideApp.Tests.csproj --filter "FullyQualifiedName~CrossPlatformTests"
```

**Current Status:** Cross-platform tests run with `InMemoryTestBridge` only. Stride integration requires full game initialization with `GameContext`, which is handled separately in dedicated integration tests (see Option 2).

### Option 2: Run All Tests by Category

```powershell
# Run all fluent API tests (InMemory only - fast)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~ScenarioApiTests"

# Run Stride integration tests (real Stride instances)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~StrideIntegrationTests"

# Run cross-platform tests (InMemory + Stride)
dotnet test tests/StrideAppTests/Game.StrideApp.Tests.csproj --filter "FullyQualifiedName~CrossPlatformTests"
```

### Option 3: Run Everything

```powershell
# Run all tests across all projects
dotnet test
```

This runs:
- Unit tests (InMemory)
- Integration tests (InMemory)
- Server tests
- Framework tests (InMemory + Stride integration)
- Cross-platform tests (InMemory + Stride)

## Test Matrix Structure

### Cross-Platform Tests (`tests/StrideAppTests/RenderingInitializationTests.cs`)

These tests use xUnit's `[Theory]` with `[MemberData]` to run against multiple bridges:

```csharp
public static IEnumerable<object[]> TestBridges()
{
    yield return new object[] { new InMemoryTestBridge() };
    yield return new object[] { StrideTestBridge.CreateTestInstance() };
    // Blazor tests require running app server - skipped for now
}
```

**Current Matrix:**
- ✅ InMemoryTestBridge (used in cross-platform tests)
- ⚠️ StrideTestBridge (requires full game initialization - see StrideIntegrationTests.cs)
- ⏸️ WebTestBridge (requires test server setup)

### Integration Tests

**Stride Integration Tests** (`tests/TestFrameworkTests/StrideIntegrationTests.cs`):
- ⚠️ Currently require full Stride game initialization with `GameContext`
- Full initialization requires creating a `GameContext` which needs platform-specific windowing code
- These tests demonstrate the intended integration but need special setup to run
- Test player movement, damage, rendering initialization

### What's Needed to Make Stride Tests Run

**See `STRIDE_INTEGRATION_SETUP.md` for detailed implementation guide.**

Quick summary:
1. **Create a GameContext**: Stride's `Game.Initialize()` requires a `GameContext` parameter
2. **Platform-Specific**: Use `GameContextWindows` (Windows) or appropriate context for your platform
3. **Use `CreateFullyInitializedInstance()`**: New method in `StrideTestBridge` that accepts a `GameContext`
4. **Current Status**: Tests use minimal initialization (`InitializeForTesting()`) which works for logic but not rendering

**Example**:
```csharp
var gameContext = new GameContextWindows(null, 800, 600, "Test");
var bridge = StrideTestBridge.CreateFullyInitializedInstance(gameContext);
```

**Blazor Integration Tests** (`tests/TestFrameworkTests/WebIntegrationTests.cs`):
- ✅ Now enabled with Playwright + Blazor test server
- Automatically launches Blazor dev server and runs tests against real browser instances
- Uses Playwright to control Chromium browser and interact with Blazor WebAssembly app
- Tests verify fluent API works with actual browser runtime

## Running Specific Bridge Types

### InMemory Tests Only (Fast)

```powershell
# Run only InMemory bridge tests
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~ScenarioApiTests"
```

### Stride Tests Only (Real Engine)

```powershell
# Run only Stride integration tests
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~StrideIntegrationTests"

# Or run cross-platform tests and filter to Stride
dotnet test tests/StrideAppTests/Game.StrideApp.Tests.csproj --filter "FullyQualifiedName~CrossPlatformTests"
```

### Blazor Tests (When Configured)

```powershell
# Run Blazor integration tests (requires running app)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~WebIntegrationTests"
```

## Test Output

When running cross-platform tests, you'll see output like:

```
RenderingInitialization_ShouldWork_OnAllPlatforms [InMemoryTestBridge] ✓
RenderingInitialization_ShouldWork_OnAllPlatforms [StrideTestBridge] ✓
CameraCreation_ShouldWork_OnAllPlatforms [InMemoryTestBridge] ✓
CameraCreation_ShouldWork_OnAllPlatforms [StrideTestBridge] ✓
```

Each test runs once per bridge type, ensuring the same behavior across platforms.

## CI/CD Test Matrix

The CI/CD pipeline runs:

1. **Unit Tests** - Fast InMemory tests
2. **Framework Tests** - InMemory + Stride integration
3. **Cross-Platform Tests** - InMemory + Stride
4. **Integration Tests** - Various bridge types

See `.github/workflows/ci.yml` for the complete test matrix execution.

## Adding New Bridges to the Matrix

To add a new bridge to the cross-platform test matrix, update `TestBridges()` in `tests/StrideAppTests/RenderingInitializationTests.cs`:

```csharp
public static IEnumerable<object[]> TestBridges()
{
    yield return new object[] { new InMemoryTestBridge() };
    yield return new object[] { StrideTestBridge.CreateTestInstance() };
    yield return new object[] { new WebTestBridge(page, appUrl) }; // When ready
}
```

All tests using `[MemberData(nameof(TestBridges))]` will automatically run against the new bridge.
