# Unified Test Specifications

This directory contains platform-agnostic test specifications that can be executed on both browser (Blazor WebAssembly) and native (Stride) game builds.

## Overview

The unified test framework consists of:

1. **ITestBridge** - Interface for test control and telemetry
2. **TestSpec** - JSON-based test specification format
3. **TestSpecExecutor** - Executes test specs against any ITestBridge implementation
4. **Test Runners** - Platform-specific runners that execute test specs

## Test Specification Format

Test specs are written in JSON and include:

- **id**: Unique identifier for the test
- **name**: Human-readable test name
- **description**: What the test validates
- **setup**: Initial game state (players, AI entities)
- **steps**: Sequence of commands and assertions

### Example Test Spec

```json
{
  "id": "player-movement-test",
  "name": "Player Movement Test",
  "description": "Verifies that player movement updates position correctly",
  "setup": {
    "players": [
      {
        "id": "player1",
        "name": "Test Player",
        "x": 0.0,
        "y": 0.0,
        "health": 100
      }
    ]
  },
  "steps": [
    {
      "advanceSteps": 1,
      "command": {
        "type": "Move",
        "targetId": "player1",
        "parameters": {
          "deltaX": 10.0,
          "deltaY": 5.0
        }
      },
      "assertions": [
        {
          "type": "PlayerPositionX",
          "targetId": "player1",
          "expected": 10.0,
          "tolerance": 0.01
        }
      ]
    }
  ]
}
```

## Supported Commands

- **Move**: Move a player entity
- **Damage**: Apply damage to a player
- **Heal**: Heal a player
- **UpdateAI**: Trigger AI update
- **Spawn**: Spawn a new entity
- **Remove**: Remove an entity

## Supported Assertions

- **PlayerPositionX**: Assert player X coordinate
- **PlayerPositionY**: Assert player Y coordinate
- **PlayerHealth**: Assert player health value
- **PlayerIsAlive**: Assert player is alive
- **PlayerIsDead**: Assert player is dead
- **PlayerCount**: Assert total player count
- **AIPositionX**: Assert AI X coordinate
- **AIPositionY**: Assert AI Y coordinate

## Running Tests

### Using the Test Runner

```bash
# Run all test specs
dotnet run --project tests/TestRunner/Game.TestRunner.csproj

# Run specific test specs
dotnet run --project tests/TestRunner/Game.TestRunner.csproj tests/TestSpecs/player-movement.json
```

### Using xUnit Tests

The TestFrameworkTests project demonstrates how to load and execute test specs programmatically:

```bash
dotnet test tests/TestFrameworkTests/Game.TestFrameworkTests.csproj
```

## Test Bridge Implementations

### InMemoryTestBridge

A simple in-memory implementation used for testing the framework itself. Maintains game state in memory and supports basic commands.

### BrowserTestBridge (Future)

Will integrate with Blazor WebAssembly using JavaScript interop for test control.

### StrideTestBridge (Future)

Will integrate with Stride game engine using:
- Fixed timestep mode (`GameTime.IsFixedTimeStep = true`)
- Custom `IGameSystem` scheduler
- TCP server for test control
- Offscreen rendering

## Adding New Tests

1. Create a new JSON file in `tests/TestSpecs/`
2. Follow the test specification format
3. Add the test to CI pipeline if needed
4. Run the test runner to validate

## Benefits

- **Write once, run everywhere**: Same test specs work on browser and native builds
- **Deterministic**: Fixed timestep ensures reproducible results
- **Platform-agnostic**: Tests are independent of rendering implementation
- **Easy to read**: JSON format is human-readable and version-control friendly
- **Automated**: Can be run in CI/CD pipelines
