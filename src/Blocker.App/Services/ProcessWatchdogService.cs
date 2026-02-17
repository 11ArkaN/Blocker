using Blocker.App.Contracts;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class ProcessWatchdogService : IProcessWatchdog, IDisposable
{
    private readonly ProcessManager _processManager;
    private readonly ILogService _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public ProcessWatchdogService(ProcessManager processManager, ILogService logger)
    {
        _processManager = processManager;
        _logger = logger;
    }

    public async Task StartAsync(BlockTargets targets, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync();

            var names = targets.ProcessNames
                .Select(name => Path.GetFileNameWithoutExtension(name))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
            {
                return;
            }

            _loopCts = new CancellationTokenSource();
            _loopTask = Task.Run(() => LoopAsync(names, _loopCts.Token), CancellationToken.None);
            _logger.Info("Process watchdog started.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _gate.Dispose();
            _loopCts?.Dispose();
        }
    }

    private async Task StopInternalAsync()
    {
        if (_loopCts is null)
        {
            return;
        }

        _loopCts.Cancel();
        try
        {
            if (_loopTask is not null)
            {
                await _loopTask;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            _loopTask = null;
            _loopCts.Dispose();
            _loopCts = null;
            _logger.Info("Process watchdog stopped.");
        }
    }

    private async Task LoopAsync(IReadOnlyCollection<string> processNames, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var killed = _processManager.KillProcesses(processNames);
            if (killed.Count > 0)
            {
                _logger.Info($"Watchdog closed process(es): {string.Join(", ", killed)}");
            }
        }
    }
}
