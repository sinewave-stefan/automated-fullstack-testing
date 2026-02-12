# Platform-Agnostic Rendering Tests

## Overview

This test project contains **platform-agnostic tests** that verify rendering system behavior across ALL platforms (Stride, Web/Blazor, etc.) using the unified test framework.

## Key Principle: NO Platform-Specific Dependencies

These tests reference ONLY `Game.Core` and use the `ITestBridge` interface. They can run on:
- **InMemoryTestBridge** - Fast unit tests, no runtime needed
- **StrideTestBridge** - Stride game engine (when available)
- **BrowserTestBridge** - Blazor WebAssembly (when available)

## Test Structure

```csharp
// Tests use ONLY core interfaces - NO Stride or Web types!
[Fact]
public void RenderingSystem_ShouldBeInitialized()
{
    var bridge = new InMemoryTestBridge();  // Any bridge works!
    var scenario = new TestScenario(bridge);
    
    scenario.InitializeRendering(cameraSlots: 1);
    
    scenario.Assert.Rendering()
        .IsInitialized()
        .HasCameraSlots(1);
}
```

## Cross-Platform Testing

```csharp
public static IEnumerable<object[]> TestBridges()
{
    yield return new object[] { new InMemoryTestBridge() };
    // Future: yield return new object[] { new BrowserTestBridge() };
    // Future: yield return new object[] { new StrideTestBridge(game) };
}

[Theory]
[MemberData(nameof(TestBridges))]
public void Test_OnAllPlatforms(ITestBridge bridge)
{
    var scenario = new TestScenario(bridge);
    // Same test code runs on ALL platforms!
}
```

## Core Testing API

### TestScenario
- `InitializeRendering(cameraSlots)` - Initialize rendering system
- `Camera(name, x, y, z)` - Create a camera entity
- `Entity(name, type, x, y, z)` - Create a generic entity
- `Step(frames)` - Advance simulation

### Assertions
- `Assert.Rendering().IsInitialized()` - Rendering system ready
- `Assert.Rendering().HasCameraSlots(n)` - Camera slots available
- `Assert.Rendering().HasActiveCamera()` - Active camera set
- `Assert.Entity(e).Exists()` - Entity exists
- `Assert.Entity(e).IsOfType("Camera")` - Entity type check
- `Assert.Entity(e).HasPosition(x, y, z)` - Position check

## Original Bug Coverage

The original bug was:
```csharp
// This threw ArgumentOutOfRangeException because Cameras was empty!
Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId()
```

The fix ensures `InitializeGraphicsCompositor()` is called BEFORE accessing camera slots.

The test `InitializationOrder_ShouldAllowCameraCreation_AfterRenderingInit` verifies this:
```csharp
scenario.InitializeRendering(cameraSlots: 1);  // Must be first!
var camera = scenario.Camera("Camera");
camera.SetAsActiveCamera(slotIndex: 0);        // Now this works

scenario.Assert.Rendering()
    .IsInitialized()
    .HasCameraSlots(1)
    .HasActiveCamera();
```

## Benefits

? **Platform-Agnostic** - Same tests for Stride, Web, etc.  
? **No Runtime Dependencies** - InMemoryTestBridge for fast tests  
? **Dual-Targeting** - Parameterized tests across platforms  
? **Fluent API** - Readable, type-safe assertions  
? **Regression Coverage** - Prevents the camera slot bug from recurring

## Running Tests

```bash
# Run all platform-agnostic rendering tests
dotnet test tests/StrideAppTests/Game.StrideApp.Tests.csproj
