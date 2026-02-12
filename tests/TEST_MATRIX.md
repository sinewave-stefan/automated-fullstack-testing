# Running the Test Matrix

The test matrix runs the same tests across multiple platforms/bridges to ensure cross-platform compatibility.

## Test Matrix Overview

The test matrix consists of tests that run against multiple `ITestBridge` implementations:

1. **InMemoryTestBridge** - Fast unit tests, no runtime needed ✅
2. **StrideTestBridge** - Real Stride game engine instances ✅
3. **WebTestBridge** - Real Blazor WebAssembly instances ✅

## Running the Full Test Matrix

### Option 1: Run All Tests by Category

```powershell
# Run fluent API tests (user-facing API - InMemory only - fast)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~ScenarioApiTests"

# Run infrastructure tests (low-level bridge & executor - InMemory only - fast)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~InfrastructureTests"

# Run rendering tests (InMemory - fast)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~RenderingTests"

# Run platform integration tests (Stride + Web matrix)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~PlatformIntegrationTests"
```

### Option 2: Run Everything

```powershell
# Run all tests across all projects
dotnet test
```

This runs:
- Unit tests (InMemory)
- Integration tests (InMemory)
- Server tests
- Framework tests:
  - Fluent API tests (InMemory)
  - Infrastructure tests (InMemory)
  - Rendering tests (InMemory)
  - Platform integration tests (Stride + Web)

## Test Matrix Structure

### Rendering Tests (`tests/TestFrameworkTests/RenderingTests.cs`)

Fast InMemory tests for rendering system initialization:
- Rendering system initialization
- Camera slot creation
- Camera and entity creation
- Initialization order verification

These tests use `InMemoryTestBridge` for fast execution without requiring platform runtimes.

### Platform Integration Tests (`tests/TestFrameworkTests/PlatformIntegrationTests.cs`)

**Platform Integration Tests** - Matrix-based test suite that verifies the test framework works correctly across both Stride and Web platforms:
- Uses `[Theory]` with `[MemberData]` to run each test against multiple bridges
- Tests player movement, damage, and rendering initialization across platforms
- Automatically handles platform-specific setup (GameContext for Stride, Playwright for Web)

**Test Matrix:**
- ✅ **Stride**: Real Stride game engine instances with full initialization
- ✅ **Web**: Real Blazor WebAssembly instances with Playwright automation

**Running Platform Integration Tests:**
```powershell
# Run all platform integration tests (runs against both Stride and Web)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~PlatformIntegrationTests"
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
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~ScenarioApiTests|RenderingTests"
```

### Stride Tests Only (Real Engine)

```powershell
# Run only Stride integration tests (filter to Stride platform)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~IntegrationTests" --filter "TestCategory=Stride"
```

### Web Tests Only

```powershell
# Run only Web integration tests (filter to Web platform)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~IntegrationTests" --filter "TestCategory=Web"
```

### Platform Integration Tests (Stride + Web Matrix)

```powershell
# Run platform integration tests (runs against both Stride and Web)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj --filter "FullyQualifiedName~PlatformIntegrationTests"
```

## Test Output

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
2. **Framework Tests** - InMemory + Stride + Web integration
3. **Rendering Tests** - InMemory rendering system tests
4. **Platform Integration Tests** - Test framework cross-platform verification (Stride + Web)

See `.github/workflows/ci.yml` for the complete test matrix execution.

## Adding New Bridges to the Matrix

To add a new bridge to the platform integration test matrix, update `TestBridges()` in `tests/TestFrameworkTests/PlatformIntegrationTests.cs`:

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
        "Web" => WebTestBridge.CreateTestInstance(),
        "YourNewPlatform" => YourNewTestBridge.CreateInstance(),
        _ => throw new NotSupportedException($"Platform {platform} is not supported")
    };
}
```

All tests using `[MemberData(nameof(TestBridges))]` will automatically run against the new bridge.
