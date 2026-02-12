# Fluent Scenario API Examples

This document provides examples of using the fluent scenario API for writing cross-platform game tests.

## Basic Usage

### Simple Player Movement

```csharp
var scenario = new TestScenario(bridge);

var player = scenario.Player("Hero", x: 0, y: 0, health: 100);

player.Move(10, 5).ThenStep();

scenario.Assert.Player(player)
    .HasPosition(10, 5)
    .HasHealth(100)
    .IsAlive();
```

### Player Combat

```csharp
var scenario = new TestScenario(bridge);

var warrior = scenario.Player("Warrior", health: 100);

warrior.TakeDamage(30).ThenStep();
scenario.Assert.Player(warrior).HasHealth(70).IsAlive();

warrior.TakeDamage(80).ThenStep();
scenario.Assert.Player(warrior).HasHealth(0).IsDead();
```

### Healing

```csharp
var scenario = new TestScenario(bridge);

var cleric = scenario.Player("Cleric", health: 100);

cleric.TakeDamage(50).ThenStep();
scenario.Assert.Player(cleric).HasHealth(50);

cleric.Heal(30).ThenStep();
scenario.Assert.Player(cleric).HasHealth(80);
```

## Complete Combat Scenario

```csharp
var bridge = new InMemoryTestBridge();
var scenario = new TestScenario(bridge);

// Setup - Create two players
var warrior = scenario.Player("Warrior", x: 0, y: 0, health: 100);
var mage = scenario.Player("Mage", x: 5, y: 5, health: 80);

scenario.Step();

// Warrior takes damage and heals
warrior.TakeDamage(30).ThenStep();
scenario.Assert.Player(warrior).HasHealth(70).IsAlive();

warrior.Heal(20).ThenStep();
scenario.Assert.Player(warrior).HasHealth(90);

// Mage takes fatal damage
mage.TakeDamage(100).ThenStep();
scenario.Assert.Player(mage).HasHealth(0).IsDead();

// Warrior moves to victory
warrior.Move(10, 15).ThenStep();
scenario.Assert.Player(warrior)
    .HasPosition(10, 15)
    .HasHealth(90)
    .IsAlive();
```

## Method Chaining

The fluent API supports method chaining for concise test authoring:

```csharp
var player = scenario.Player("Hero", health: 100);

player
    .Move(5, 5)
    .ThenStep()
    .TakeDamage(20)
    .ThenStep()
    .Heal(10)
    .ThenStep();

scenario.Assert.Player(player)
    .HasPosition(5, 5)
    .HasHealth(90)
    .IsAlive();
```

## Cross-Platform Testing

The same scenario code runs on any platform implementing `ITestBridge`:

```csharp
// Test on InMemory bridge
[Fact]
public void PlayerMovement_InMemory()
{
    var bridge = new InMemoryTestBridge();
    RunPlayerMovementTest(bridge);
}

// Test on Browser bridge (future)
[Fact]
public void PlayerMovement_Browser()
{
    var bridge = new BrowserTestBridge();
    RunPlayerMovementTest(bridge);
}

// Test on Stride bridge (future)
[Fact]
public void PlayerMovement_Stride()
{
    var bridge = new StrideTestBridge();
    RunPlayerMovementTest(bridge);
}

// Shared test logic
private void RunPlayerMovementTest(ITestBridge bridge)
{
    var scenario = new TestScenario(bridge);
    
    var player = scenario.Player("Hero", x: 0, y: 0);
    player.Move(10, 5).ThenStep();
    
    scenario.Assert.Player(player).HasPosition(10, 5);
}
```

## API Reference

### TestScenario

- `Player(name, x, y, health)` - Create a player
- `Step(frames)` - Advance simulation
- `Reset()` - Reset game state
- `GetSnapshot()` - Get current state
- `Assert` - Access assertion helpers

### PlayerHandle

- `Move(deltaX, deltaY)` - Move player
- `TakeDamage(amount)` - Apply damage
- `Heal(amount)` - Restore health
- `ThenStep(frames)` - Advance simulation after operation

### AssertionHelper

- `Player(handle)` - Get assertions for player
- `Player(name)` - Get assertions for player by name

### PlayerAssertions

- `HasPosition(x, y, tolerance)` - Assert position
- `HasHealth(health)` - Assert health value
- `IsAlive()` - Assert player is alive
- `IsDead()` - Assert player is dead

## Comparison: JSON vs Fluent API

### JSON Approach (Old)

```json
{
  "id": "player-movement-test",
  "setup": {
    "players": [{"id": "p1", "x": 0, "y": 0, "health": 100}]
  },
  "steps": [{
    "command": {"type": "Move", "targetId": "p1", 
                "parameters": {"deltaX": 10, "deltaY": 5}},
    "assertions": [
      {"type": "PlayerPositionX", "targetId": "p1", "expected": 10.0}
    ]
  }]
}
```

### Fluent API Approach (New)

```csharp
var scenario = new TestScenario(bridge);
var player = scenario.Player("TestPlayer", x: 0, y: 0, health: 100);

player.Move(10, 5).ThenStep();

scenario.Assert.Player(player).HasPosition(10, 5);
```

## Benefits

✅ **Type-safe** - Compile-time checking, refactoring support  
✅ **IntelliSense** - Auto-completion in IDE  
✅ **Readable** - Natural language-like syntax  
✅ **Compact** - Less verbose than JSON  
✅ **Debuggable** - Step through test code  
✅ **Composable** - Reuse test fragments  
✅ **Cross-platform** - Same code, multiple bridges
