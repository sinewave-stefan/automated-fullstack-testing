namespace Game.Core.Testing;

/// <summary>
/// Unified test control interface for both browser and Stride builds.
/// Provides deterministic test execution and state inspection.
/// </summary>
public interface ITestBridge
{
    /// <summary>
    /// Advances the game simulation by one fixed time step.
    /// </summary>
    void Step();

    /// <summary>
    /// Advances the game simulation by multiple fixed time steps.
    /// </summary>
    /// <param name="count">Number of steps to advance</param>
    void Step(int count);

    /// <summary>
    /// Resets the game state to initial conditions.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets a snapshot of the current game state for verification.
    /// </summary>
    /// <returns>Current game state snapshot</returns>
    TestSnapshot GetSnapshot();

    /// <summary>
    /// Executes a test command (e.g., player input, AI trigger).
    /// </summary>
    /// <param name="command">Command to execute</param>
    void ExecuteCommand(TestCommand command);

    /// <summary>
    /// Indicates whether the test bridge is running in test mode.
    /// </summary>
    bool IsTestMode { get; }
}
