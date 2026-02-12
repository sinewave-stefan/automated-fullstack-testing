using Game.Core;
using Game.Core.Testing;
using System.Text.Json;

namespace Game.TestRunner;

/// <summary>
/// Simple test runner that executes test specs from JSON files.
/// This runner uses the InMemoryTestBridge for demonstration.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("=== Game Test Runner ===");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: TestRunner <spec-file.json> [spec-file2.json ...]");
            Console.WriteLine();
            Console.WriteLine("Running all test specs in current directory...");
            args = Directory.GetFiles(".", "*.json");
        }

        var totalTests = 0;
        var passedTests = 0;
        var failedTests = 0;

        foreach (var specFile in args)
        {
            if (!File.Exists(specFile))
            {
                Console.WriteLine($"ERROR: File not found: {specFile}");
                continue;
            }

            Console.WriteLine($"Running: {Path.GetFileName(specFile)}");
            
            try
            {
                var json = File.ReadAllText(specFile);
                var spec = JsonSerializer.Deserialize<TestSpec>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (spec == null)
                {
                    Console.WriteLine($"  ERROR: Failed to parse spec");
                    failedTests++;
                    continue;
                }

                // Create test bridge
                var bridge = new InMemoryTestBridge();

                // Execute test
                var executor = new TestSpecExecutor(bridge);
                var result = executor.Execute(spec);

                totalTests++;

                if (result.Success)
                {
                    Console.WriteLine($"  ✓ PASSED in {result.Duration.TotalMilliseconds:F0}ms");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine($"  ✗ FAILED: {result.FailureReason}");
                    failedTests++;
                    
                    // Print step details
                    for (int i = 0; i < result.StepResults.Count; i++)
                    {
                        var stepResult = result.StepResults[i];
                        if (!stepResult.Success)
                        {
                            Console.WriteLine($"    Step {i + 1}: {stepResult.FailureReason}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                failedTests++;
            }

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Total:  {totalTests}");
        Console.WriteLine($"Passed: {passedTests}");
        Console.WriteLine($"Failed: {failedTests}");

        return failedTests > 0 ? 1 : 0;
    }
}
