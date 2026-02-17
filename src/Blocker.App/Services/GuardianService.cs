using System.Diagnostics;
using Blocker.App.Constants;
using Blocker.App.Contracts;

namespace Blocker.App.Services;

public sealed class GuardianService : IGuardianService
{
    private readonly ILogService _logger;
    private readonly string _appExecutablePath;
    private readonly int _appProcessId;
    private readonly string _guardianExecutablePath;
    private Process? _guardianProcess;
    private string? _stopFilePath;

    public GuardianService(ILogService logger, string appExecutablePath)
    {
        _logger = logger;
        _appExecutablePath = appExecutablePath;
        _appProcessId = Environment.ProcessId;
        _guardianExecutablePath = Path.Combine(Path.GetDirectoryName(appExecutablePath) ?? string.Empty, "Blocker.Guardian.exe");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_guardianProcess is not null && !_guardianProcess.HasExited)
        {
            return;
        }

        if (!File.Exists(_guardianExecutablePath))
        {
            _logger.Warn($"Guardian executable not found at {_guardianExecutablePath}.");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        var guardianRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Blocker",
            BlockerConstants.GuardianFolderName);

        Directory.CreateDirectory(guardianRoot);
        _stopFilePath = Path.Combine(guardianRoot, token + BlockerConstants.GuardianStopFileExtension);
        if (File.Exists(_stopFilePath))
        {
            File.Delete(_stopFilePath);
        }

        var psi = new ProcessStartInfo
        {
            FileName = _guardianExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = $"--monitor-pid {_appProcessId} --app-path \"{_appExecutablePath}\" --token {token} --stop-file \"{_stopFilePath}\""
        };

        _guardianProcess = Process.Start(psi);
        if (_guardianProcess is null)
        {
            _logger.Warn("Guardian process could not be started.");
            return;
        }

        _logger.Info($"Guardian started with PID {_guardianProcess.Id}.");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_guardianProcess is null)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(_stopFilePath))
            {
                await File.WriteAllTextAsync(_stopFilePath, "stop", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to write guardian stop signal: {ex.Message}");
        }

        try
        {
            var waitTask = _guardianProcess.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            var completed = await Task.WhenAny(waitTask, timeoutTask);

            if (completed != waitTask && !_guardianProcess.HasExited)
            {
                _guardianProcess.Kill(true);
                _guardianProcess.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to stop guardian cleanly: {ex.Message}");
        }
        finally
        {
            _guardianProcess.Dispose();
            _guardianProcess = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(_stopFilePath) && File.Exists(_stopFilePath))
                {
                    File.Delete(_stopFilePath);
                }
            }
            catch
            {
                // Best effort cleanup.
            }

            _stopFilePath = null;
            _logger.Info("Guardian stopped.");
        }
    }

    public bool IsHealthy()
    {
        return _guardianProcess is not null && !_guardianProcess.HasExited;
    }
}
