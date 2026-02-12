namespace Game.Core.Testing;

/// <summary>
/// Executes test specifications against a test bridge implementation.
/// Platform-agnostic test runner.
/// </summary>
public class TestSpecExecutor
{
    private readonly ITestBridge _bridge;

    public TestSpecExecutor(ITestBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    /// <summary>
    /// Executes a test specification and returns the result.
    /// </summary>
    public TestResult Execute(TestSpec spec)
    {
        var result = new TestResult
        {
            TestId = spec.Id,
            TestName = spec.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Reset to clean state
            _bridge.Reset();

            // Setup initial state from spec
            SetupInitialState(spec.Setup);

            // Execute each step
            foreach (var step in spec.Steps)
            {
                var stepResult = ExecuteStep(step);
                result.StepResults.Add(stepResult);

                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.FailureReason = $"Step failed: {stepResult.FailureReason}";
                    break;
                }
            }

            if (result.StepResults.All(s => s.Success))
            {
                result.Success = true;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailureReason = $"Exception during test execution: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private void SetupInitialState(TestSetup setup)
    {
        // Spawn players from setup
        foreach (var playerSetup in setup.Players)
        {
            var spawnCommand = new TestCommand
            {
                Type = TestCommandType.Spawn,
                TargetId = playerSetup.Id,
                Parameters = new()
                {
                    { "name", playerSetup.Name },
                    { "x", playerSetup.X },
                    { "y", playerSetup.Y },
                    { "health", playerSetup.Health }
                }
            };
            _bridge.ExecuteCommand(spawnCommand);
        }

        // TODO: Setup AI entities when needed
    }

    private TestStepResult ExecuteStep(TestStep step)
    {
        var stepResult = new TestStepResult();

        try
        {
            // Advance simulation
            if (step.AdvanceSteps > 0)
            {
                _bridge.Step(step.AdvanceSteps);
            }

            // Execute command if present
            if (step.Command != null)
            {
                _bridge.ExecuteCommand(step.Command);
            }

            // Get snapshot
            var snapshot = _bridge.GetSnapshot();
            stepResult.Snapshot = snapshot;

            // Verify assertions
            foreach (var assertion in step.Assertions)
            {
                var assertionResult = VerifyAssertion(assertion, snapshot);
                stepResult.AssertionResults.Add(assertionResult);

                if (!assertionResult.Success)
                {
                    stepResult.Success = false;
                    stepResult.FailureReason = assertionResult.Message;
                    return stepResult;
                }
            }

            stepResult.Success = true;
        }
        catch (Exception ex)
        {
            stepResult.Success = false;
            stepResult.FailureReason = $"Exception: {ex.Message}";
        }

        return stepResult;
    }

    private AssertionResult VerifyAssertion(TestAssertion assertion, TestSnapshot snapshot)
    {
        var result = new AssertionResult
        {
            Type = assertion.Type,
            TargetId = assertion.TargetId
        };

        try
        {
            switch (assertion.Type)
            {
                case AssertionType.PlayerPositionX:
                    result.Success = VerifyPlayerPositionX(assertion, snapshot, out var msgX);
                    result.Message = msgX;
                    break;

                case AssertionType.PlayerPositionY:
                    result.Success = VerifyPlayerPositionY(assertion, snapshot, out var msgY);
                    result.Message = msgY;
                    break;

                case AssertionType.PlayerHealth:
                    result.Success = VerifyPlayerHealth(assertion, snapshot, out var msgH);
                    result.Message = msgH;
                    break;

                case AssertionType.PlayerIsAlive:
                    result.Success = VerifyPlayerIsAlive(assertion, snapshot, out var msgA);
                    result.Message = msgA;
                    break;

                case AssertionType.PlayerIsDead:
                    result.Success = VerifyPlayerIsDead(assertion, snapshot, out var msgD);
                    result.Message = msgD;
                    break;

                case AssertionType.PlayerCount:
                    result.Success = VerifyPlayerCount(assertion, snapshot, out var msgC);
                    result.Message = msgC;
                    break;

                default:
                    result.Success = false;
                    result.Message = $"Unknown assertion type: {assertion.Type}";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Assertion error: {ex.Message}";
        }

        return result;
    }

    private bool VerifyPlayerPositionX(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var player = snapshot.Players.FirstOrDefault(p => p.Id == assertion.TargetId);
        if (player == null)
        {
            message = $"Player {assertion.TargetId} not found";
            return false;
        }

        var expected = ConvertToFloat(assertion.Expected);
        var actual = player.X;
        var diff = Math.Abs(actual - expected);

        if (diff <= assertion.Tolerance)
        {
            message = $"Player {assertion.TargetId} position X: {actual} (expected {expected})";
            return true;
        }

        message = $"Player {assertion.TargetId} position X: {actual} (expected {expected}, diff {diff})";
        return false;
    }

    private bool VerifyPlayerPositionY(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var player = snapshot.Players.FirstOrDefault(p => p.Id == assertion.TargetId);
        if (player == null)
        {
            message = $"Player {assertion.TargetId} not found";
            return false;
        }

        var expected = ConvertToFloat(assertion.Expected);
        var actual = player.Y;
        var diff = Math.Abs(actual - expected);

        if (diff <= assertion.Tolerance)
        {
            message = $"Player {assertion.TargetId} position Y: {actual} (expected {expected})";
            return true;
        }

        message = $"Player {assertion.TargetId} position Y: {actual} (expected {expected}, diff {diff})";
        return false;
    }

    private bool VerifyPlayerHealth(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var player = snapshot.Players.FirstOrDefault(p => p.Id == assertion.TargetId);
        if (player == null)
        {
            message = $"Player {assertion.TargetId} not found";
            return false;
        }

        var expected = ConvertToInt(assertion.Expected);
        var actual = player.Health;

        if (actual == expected)
        {
            message = $"Player {assertion.TargetId} health: {actual}";
            return true;
        }

        message = $"Player {assertion.TargetId} health: {actual} (expected {expected})";
        return false;
    }

    private bool VerifyPlayerIsAlive(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var player = snapshot.Players.FirstOrDefault(p => p.Id == assertion.TargetId);
        if (player == null)
        {
            message = $"Player {assertion.TargetId} not found";
            return false;
        }

        if (player.IsAlive)
        {
            message = $"Player {assertion.TargetId} is alive";
            return true;
        }

        message = $"Player {assertion.TargetId} is dead";
        return false;
    }

    private bool VerifyPlayerIsDead(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var player = snapshot.Players.FirstOrDefault(p => p.Id == assertion.TargetId);
        if (player == null)
        {
            message = $"Player {assertion.TargetId} not found";
            return false;
        }

        if (!player.IsAlive)
        {
            message = $"Player {assertion.TargetId} is dead";
            return true;
        }

        message = $"Player {assertion.TargetId} is alive";
        return false;
    }

    private bool VerifyPlayerCount(TestAssertion assertion, TestSnapshot snapshot, out string message)
    {
        var expected = ConvertToInt(assertion.Expected);
        var actual = snapshot.Players.Count;

        if (actual == expected)
        {
            message = $"Player count: {actual}";
            return true;
        }

        message = $"Player count: {actual} (expected {expected})";
        return false;
    }

    private static float ConvertToFloat(object? value)
    {
        if (value == null) return 0f;
        
        // Handle JsonElement from deserialization
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetSingle();
        }
        
        return Convert.ToSingle(value);
    }

    private static int ConvertToInt(object? value)
    {
        if (value == null) return 0;
        
        // Handle JsonElement from deserialization
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetInt32();
        }
        
        return Convert.ToInt32(value);
    }
}

/// <summary>
/// Result of executing a test specification.
/// </summary>
public class TestResult
{
    public string TestId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<TestStepResult> StepResults { get; set; } = new();
}

/// <summary>
/// Result of executing a single test step.
/// </summary>
public class TestStepResult
{
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public TestSnapshot? Snapshot { get; set; }
    public List<AssertionResult> AssertionResults { get; set; } = new();
}

/// <summary>
/// Result of a single assertion.
/// </summary>
public class AssertionResult
{
    public AssertionType Type { get; set; }
    public string? TargetId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
