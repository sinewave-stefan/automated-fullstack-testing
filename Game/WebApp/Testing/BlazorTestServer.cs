using System.Diagnostics;
using System.Net.Http;

namespace Game.WebApp.Testing;

/// <summary>
/// Helper class to launch and manage a Blazor WebAssembly dev server for testing.
/// </summary>
public class BlazorTestServer : IDisposable
{
    private Process? _process;
    private string? _baseUrl;
    private readonly string _projectPath;
    private readonly int _port;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public BlazorTestServer(string projectPath, int port = 5291)
    {
        _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        _port = port;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public string BaseUrl => _baseUrl ?? throw new InvalidOperationException("Server not started");

    /// <summary>
    /// Starts the Blazor dev server and waits for it to be ready.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process != null)
        {
            throw new InvalidOperationException("Server already started");
        }

        var projectDirectory = Path.GetDirectoryName(_projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {projectDirectory}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectPath}\" --urls http://localhost:{_port}",
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            }
        };

        _process = Process.Start(startInfo);
        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start Blazor dev server");
        }

        // Consume output asynchronously to avoid blocking
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _baseUrl = $"http://localhost:{_port}";

        // Wait for server to be ready
        await WaitForServerReadyAsync(cancellationToken);
    }

    private async Task WaitForServerReadyAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = 30;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i < maxAttempts; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Server startup cancelled");
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    // Give it a bit more time for Blazor to fully initialize
                    await Task.Delay(2000, cancellationToken);
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet, continue waiting
            }
            catch (TaskCanceledException)
            {
                // Timeout, continue waiting
            }

            await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException($"Blazor dev server did not become ready within {maxAttempts * delay.TotalSeconds} seconds");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();

        if (_process != null && !_process.HasExited)
        {
            try
            {
                // Try graceful shutdown first
                _process.Kill();
                if (!_process.WaitForExit(5000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        _disposed = true;
    }
}
