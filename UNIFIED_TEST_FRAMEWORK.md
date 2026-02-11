# Unified Test Framework - Implementation Summary

## Overview

This implementation provides a unified test framework that allows writing platform-agnostic tests that can run on both browser (Blazor WebAssembly) and native (Stride) game builds.

## What Was Implemented

### Core Components (Game.Core.Testing namespace)

1. **ITestBridge** - The central interface that all test implementations must implement
   - `Step()` / `Step(count)` - Advance simulation by fixed timesteps
   - `Reset()` - Reset game state to initial conditions
   - `GetSnapshot()` - Capture current game state
   - `ExecuteCommand(command)` - Execute platform-agnostic commands
   - `IsTestMode` - Indicates test mode is active

2. **TestSnapshot** - Platform-agnostic state capture
   - Frame number
   - Player states (position, health, alive status)
   - AI entity states
   - Custom metadata

3. **TestCommand** - Command system for test actions
   - Move - Player movement
   - Damage - Apply damage
   - Heal - Restore health
   - Spawn - Create entities
   - Remove - Delete entities
   - UpdateAI - Trigger AI update

4. **TestSpec** - JSON-based test specification
   - Setup phase (initial players and AI)
   - Test steps (commands + assertions)
   - Flexible assertion system

5. **TestSpecExecutor** - Executes test specs
   - Loads and parses JSON specs
   - Runs steps sequentially
   - Validates assertions
   - Returns detailed results

6. **InMemoryTestBridge** - Reference implementation
   - Simple in-memory game state
   - Full command support
   - Used for framework validation

### Infrastructure

1. **TestRunner** (Console Application)
   - Loads test specs from JSON files
   - Executes specs using InMemoryTestBridge
   - Provides pass/fail reporting
   - Returns appropriate exit codes for CI

2. **TestFrameworkTests** (xUnit Test Project)
   - 7 comprehensive tests validating the framework
   - Tests command execution
   - Tests spec loading and parsing
   - Tests assertion verification

3. **Test Specifications** (JSON files)
   - `player-movement.json` - Movement validation
   - `player-damage.json` - Damage and death scenarios  
   - `player-combat.json` - Complex multi-player combat

### CI/CD Integration

Updated `.github/workflows/ci.yml` to include:
- Test framework validation tests
- Unified test spec execution with InMemory bridge
- All existing tests continue to pass

## How It Works

### Test Specification Flow

```
┌─────────────────┐
│  Test Spec.json │
│  (Platform-     │
│   agnostic)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ TestSpecExecutor│
│                 │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  ITestBridge    │◄──── InMemoryTestBridge (current)
│  Interface      │◄──── BrowserTestBridge (future)
│                 │◄──── StrideTestBridge (future)
└─────────────────┘
```

### Example Test Spec

```json
{
  "id": "player-movement-test",
  "name": "Player Movement Test",
  "setup": {
    "players": [
      {"id": "player1", "name": "Test", "x": 0, "y": 0, "health": 100}
    ]
  },
  "steps": [
    {
      "command": {"type": "Move", "targetId": "player1", 
                  "parameters": {"deltaX": 10, "deltaY": 5}},
      "assertions": [
        {"type": "PlayerPositionX", "targetId": "player1", "expected": 10.0}
      ]
    }
  ]
}
```

## Test Results

All tests passing:
- **Unit Tests**: 26 passing ✓
- **Integration Tests**: 3 passing ✓
- **Server Tests**: 6 passing ✓  
- **Test Framework Tests**: 7 passing ✓
- **Unified Test Specs**: 3 passing ✓

**Total: 45 tests, 0 failures**

## Key Benefits

1. **Write Once, Run Everywhere**
   - Same test specs work on browser, native, and future platforms
   - No code duplication for platform-specific tests

2. **Deterministic & Reproducible**
   - Fixed timestep ensures consistent behavior
   - JSON specs are version-control friendly
   - Easy to review and understand

3. **Platform Agnostic**
   - Tests are independent of rendering
   - Focus on game logic, not graphics
   - Easy to debug failures

4. **CI/CD Ready**
   - Automated execution in GitHub Actions
   - Clear pass/fail reporting
   - Exit codes for build systems

5. **Extensible**
   - Easy to add new commands
   - Simple to add new assertions
   - Custom test bridges for any platform

## Future Enhancements

While the core framework is complete, these enhancements would enable full cross-platform testing:

### BrowserTestBridge (Blazor WebAssembly)
- JavaScript interop for test control
- DOM manipulation for commands
- State extraction via JSInterop
- Playwright/Puppeteer runner integration

### StrideTestBridge (Native Game Engine)
- Fixed timestep mode (`GameTime.IsFixedTimeStep = true`)
- Custom `IGameSystem` for test control
- TCP server for remote commands (e.g., port 8765)
- Command-line `--test-mode` flag
- Offscreen rendering or null graphics device

### Test Runners
- **Playwright Runner** - Launches browser, injects test bridge, executes specs
- **Stride Runner** - Launches game in test mode, connects via TCP, executes specs
- Both runners execute the same test specs

### CI Pipeline
```yaml
test-browser:
  - Start browser with test mode
  - Run Playwright runner with test specs
  - Report results

test-stride:  
  - Build Stride app with test mode
  - Run Stride runner with test specs
  - Report results
```

## Usage Examples

### Running Tests Locally

```bash
# Run all test specs
dotnet run --project tests/TestRunner/Game.TestRunner.csproj

# Run specific spec
dotnet run --project tests/TestRunner/Game.TestRunner.csproj tests/TestSpecs/player-combat.json

# Run framework validation
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj
```

### Creating New Tests

1. Create a JSON file in `tests/TestSpecs/`
2. Define setup (initial players, AI)
3. Add test steps (commands + assertions)
4. Run with TestRunner to validate
5. Add to CI if needed

See `tests/TestSpecs/README.md` for detailed format documentation.

## Architecture Decisions

### Why JSON for Test Specs?
- Human-readable and easy to review
- Version control friendly
- Language/platform agnostic
- Easy to generate programmatically
- No compilation required

### Why ITestBridge Interface?
- Enables multiple implementations (memory, browser, native)
- Clear contract for test control
- Easy to mock and test
- Supports future platforms

### Why Fixed Timestep?
- Ensures deterministic behavior
- Same results every time
- Easy to debug failures
- Platform-independent timing

### Why Separate Test Specs from Runner?
- Same specs run on all platforms
- Specs can be shared between teams
- Runners can be optimized per platform
- Clear separation of concerns

## Conclusion

The unified test framework provides a solid foundation for cross-platform testing. The core infrastructure is complete and validated, with clear extension points for future platform implementations. All tests are passing and the framework is ready for use.
