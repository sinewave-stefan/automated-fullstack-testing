# Test Specifications

This directory contains documentation and examples for the unified test framework.

## Primary Approach: Fluent API

**The recommended way to write tests is using the fluent `TestScenario` API.** This provides type-safe, IntelliSense-enabled, debuggable test authoring.

See [`FLUENT_API_EXAMPLES.md`](./FLUENT_API_EXAMPLES.md) for comprehensive examples.

### Quick Example

```csharp
var bridge = new InMemoryTestBridge();
var scenario = new TestScenario(bridge);

var warrior = scenario.Player("Warrior", x: 0, y: 0, health: 100);

warrior.Move(10, 5).ThenStep();
scenario.Assert.Player(warrior).HasPosition(10, 5);

warrior.TakeDamage(30).ThenStep();
scenario.Assert.Player(warrior).HasHealth(70).IsAlive();
```

### Running Fluent API Tests

```bash
# Run all tests (includes fluent API tests)
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj
```

## JSON Test Specs (Legacy/Infrastructure)

The `TestSpec` JSON format and `TestSpecExecutor` infrastructure exists for potential future use cases:
- Non-C# test runners (e.g., JavaScript/TypeScript for browser testing)
- External test specification tools
- Platform-specific integrations

**However, for C# test development, use the fluent API instead.**

The JSON infrastructure (`TestSpec`, `TestSpecExecutor`) is still maintained and tested, but JSON test spec files are no longer included in this repository. If you need to use JSON specs programmatically, you can create `TestSpec` objects in code (see `InfrastructureTests.cs` for examples).

## Test Bridge Implementations

### InMemoryTestBridge

A simple in-memory implementation used for testing. Maintains game state in memory and supports basic commands.

### BrowserTestBridge (Future)

Will integrate with Blazor WebAssembly using JavaScript interop for test control.

### StrideTestBridge (Future)

Will integrate with Stride game engine using:
- Fixed timestep mode (`GameTime.IsFixedTimeStep = true`)
- Custom `IGameSystem` scheduler
- TCP server for test control
- Offscreen rendering

## Benefits of Fluent API

✅ **Type-safe** - Compile-time checking, refactoring support  
✅ **IntelliSense** - Auto-completion in IDE  
✅ **Readable** - Natural language-like syntax  
✅ **Compact** - Less verbose than JSON  
✅ **Debuggable** - Step through test code  
✅ **Composable** - Reuse test fragments  
✅ **Cross-platform** - Same code, multiple bridges
