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

**Unified Integration Tests** (`tests/TestFrameworkTests/IntegrationTests.cs`):
- ✅ **Matrix-based test suite** that runs the same tests against both Stride and Web platforms
- Uses `[Theory]` with `[MemberData]` to run each test against multiple bridges
- Tests player movement, damage, and rendering initialization across platforms
- Automatically handles platform-specific setup (GameContext for Stride, Playwright for Web)

**Test Matrix:**
- ✅ **Stride**: Real Stride game engine instances with full initialization
- ✅ **Web**: Real Blazor WebAssembly instances with Playwright automation

**Running Integration Tests:**
```powershell
# Run all integration tests (runs against both Stride and Web)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~IntegrationTests"
```


**Stride Integration Details:**
- Requires full Stride game initialization with `GameContext`
- See `STRIDE_INTEGRATION_SETUP.md` for detailed implementation guide
- On Windows, automatically creates `GameContextWindows` for full initialization
- Falls back to minimal initialization if GameContext creation fails

**Web Integration Details:**
- Automatically launches Blazor dev server using `BlazorTestServer`
- Uses Playwright to control Chromium browser
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

### Integration Tests (Stride + Web Matrix)

```powershell
# Run unified integration tests (runs against both Stride and Web)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~IntegrationTests"
```

## Test Output

When running cross-platform tests, you'll see output like:

```
RenderingInitialization_ShouldWork_OnAllPlatforms [InMemoryTestBridge] ✓
RenderingInitialization_ShouldWork_OnAllPlatforms [StrideTestBridge] ✓
CameraCreation_ShouldWork_OnAllPlatforms [InMemoryTestBridge] ✓
CameraCreation_ShouldWork_OnAllPlatforms [StrideTestBridge] ✓
```

When running integration tests, you'll see output like:

```
PlayerMovement_UpdatesPosition [Stride] ✓
PlayerMovement_UpdatesPosition [Web] ✓
PlayerDamage_ReducesHealth [Stride] ✓
PlayerDamage_ReducesHealth [Web] ✓
RenderingInitialization_Works [Stride] ✓
```

Each test runs once per bridge type, ensuring the same behavior across platforms.

## CI/CD Test Matrix

The CI/CD pipeline runs:

1. **Unit Tests** - Fast InMemory tests
2. **Framework Tests** - InMemory + Stride integration
3. **Cross-Platform Tests** - InMemory + Stride
4. **Integration Tests** - Unified matrix (Stride + Web)

See `.github/workflows/ci.yml` for the complete test matrix execution.

## Adding New Bridges to the Matrix

### Cross-Platform Tests

To add a new bridge to the cross-platform test matrix, update `TestBridges()` in `tests/StrideAppTests/RenderingInitializationTests.cs`:

```csharp
public static IEnumerable<object[]> TestBridges()
{
    yield return new object[] { new InMemoryTestBridge() };
    yield return new object[] { StrideTestBridge.CreateTestInstance() };
    yield return new object[] { new WebTestBridge(page, appUrl) }; // When ready
}
```

### Integration Tests

To add a new bridge to the integration test matrix, update `TestBridges()` in `tests/TestFrameworkTests/IntegrationTests.cs`:

```csharp
public static IEnumerable<object[]> TestBridges()
{
    yield return new object[] { "Stride" };
    yield return new object[] { "Web" };
    yield return new object[] { "YourNewPlatform" }; // Add your platform here
}
```

Then update `CreateBridge()` to handle the new platform:

```csharp
private ITestBridge CreateBridge(string platform)
{
    return platform switch
    {
        "Stride" => StrideTestBridge.CreateTestInstance(),
        "Web" => CreateWebBridge(),
        "YourNewPlatform" => YourNewTestBridge.CreateInstance(),
        _ => throw new NotSupportedException($"Platform {platform} is not supported")
    };
}
```

All tests using `[MemberData(nameof(TestBridges))]` will automatically run against the new bridge.
