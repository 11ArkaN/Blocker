using Blocker.App.Contracts;
using Blocker.App.Constants;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class BlockOrchestrator : IBlockOrchestrator
{
    private readonly IReadOnlyList<IBlockingProvider> _providers;
    private readonly IProcessWatchdog _watchdog;
    private readonly ProcessManager _processManager;
    private readonly TargetCatalog _targetCatalog;
    private readonly IStateStore _stateStore;
    private readonly IFocusLockService _focusLockService;
    private readonly IGuardianService _guardianService;
    private readonly ILogService _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _isActive;
    private DateTimeOffset? _activatedAtUtc;
    private DateTimeOffset? _focusLockUntilUtc;
    private string? _unlockPhrase;
    private bool _guardianExpectedRunning;

    public BlockOrchestrator(
        IEnumerable<IBlockingProvider> providers,
        IProcessWatchdog watchdog,
        ProcessManager processManager,
        TargetCatalog targetCatalog,
        IStateStore stateStore,
        IFocusLockService focusLockService,
        IGuardianService guardianService,
        ILogService logger)
    {
        _providers = providers.ToList();
        _watchdog = watchdog;
        _processManager = processManager;
        _targetCatalog = targetCatalog;
        _stateStore = stateStore;
        _focusLockService = focusLockService;
        _guardianService = guardianService;
        _logger = logger;
    }

    public async Task InitializeFromPersistedStateAsync(
        bool resumeActiveRuntime,
        CancellationToken cancellationToken = default)
    {
        var persisted = await _stateStore.LoadAsync(cancellationToken);
        _isActive = persisted.IsBlockActive;
        _activatedAtUtc = persisted.ActivatedAtUtc;
        _focusLockUntilUtc = persisted.FocusLockUntilUtc;
        _unlockPhrase = persisted.UnlockPhrase;
        _guardianExpectedRunning = persisted.GuardianExpectedRunning;

        // Backward compatibility for sessions persisted before phrase-per-session support.
        if (_isActive && string.IsNullOrWhiteSpace(_unlockPhrase))
        {
            _unlockPhrase = BlockerConstants.UnlockPhrase;
        }

        if (!_isActive || !resumeActiveRuntime)
        {
            return;
        }

        var targets = _targetCatalog.GetDefaultTargets();
        _processManager.KillProcesses(targets.ProcessNames);
        await _watchdog.StartAsync(targets, cancellationToken);

        if (_guardianExpectedRunning)
        {
            await _guardianService.StartAsync(cancellationToken);
        }

        _logger.Info("Resumed runtime services for active block session.");
    }

    public async Task<BlockResult> EnableAsync(string unlockPhrase, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var messages = new List<string>();
            var errors = new List<string>();
            var normalizedPhrase = unlockPhrase?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedPhrase))
            {
                errors.Add("Unlock phrase must be set before enabling block.");
                return BuildResult(messages, errors);
            }

            if (!AdminService.IsAdministrator())
            {
                errors.Add("Application must run as administrator.");
                return BuildResult(messages, errors);
            }

            var targets = _targetCatalog.GetDefaultTargets();

            await _watchdog.StopAsync(cancellationToken);
            await _guardianService.StopAsync(cancellationToken);

            foreach (var provider in _providers)
            {
                try
                {
                    await provider.RemoveAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Provider cleanup warning: {ex.Message}");
                }
            }

            foreach (var provider in _providers)
            {
                try
                {
                    await provider.ApplyAsync(targets, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    _logger.Error("Provider apply failed.", ex);
                }
            }

            if (errors.Count > 0)
            {
                await RollbackPartialEnableAsync(cancellationToken);
                return BuildResult(messages, errors);
            }

            var killed = _processManager.KillProcesses(targets.ProcessNames);
            if (killed.Count > 0)
            {
                messages.Add($"Closed processes: {string.Join(", ", killed)}");
            }

            await _watchdog.StartAsync(targets, cancellationToken);
            await _guardianService.StartAsync(cancellationToken);

            _isActive = true;
            _activatedAtUtc = DateTimeOffset.UtcNow;
            _focusLockUntilUtc = _focusLockService.ComputeLockEnd(_activatedAtUtc.Value);
            _unlockPhrase = normalizedPhrase;
            _guardianExpectedRunning = true;
            await PersistCurrentStateAsync(lastShutdownClean: false, cancellationToken);

            messages.Add("Blocking has been enabled.");
            _logger.Info("Blocking enabled.");
            return BuildResult(messages, errors);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<BlockResult> DisableAsync(
        string? unlockPhrase = null,
        bool bypassFocusLock = false,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var messages = new List<string>();
            var errors = new List<string>();

            if (!bypassFocusLock && _isActive && _focusLockService.IsPhraseRequired(_focusLockUntilUtc, DateTimeOffset.UtcNow))
            {
                if (!_focusLockService.ValidatePhrase(unlockPhrase, _unlockPhrase))
                {
                    errors.Add("Unlock phrase is required during focus lock.");
                    return BuildResult(messages, errors);
                }
            }

            await _watchdog.StopAsync(cancellationToken);
            await _guardianService.StopAsync(cancellationToken);

            foreach (var provider in _providers)
            {
                try
                {
                    await provider.RemoveAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    _logger.Error("Provider remove failed.", ex);
                }
            }

            _isActive = false;
            _activatedAtUtc = null;
            _focusLockUntilUtc = null;
            _unlockPhrase = null;
            _guardianExpectedRunning = false;
            await PersistCurrentStateAsync(lastShutdownClean: false, cancellationToken);

            messages.Add("Blocking has been disabled.");
            _logger.Info("Blocking disabled.");
            return BuildResult(messages, errors);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<BlockState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var nowUtc = DateTimeOffset.UtcNow;
        return Task.FromResult(new BlockState
        {
            IsActive = _isActive,
            ActivatedAt = _activatedAtUtc,
            FocusLockUntil = _focusLockUntilUtc,
            UnlockPhrase = _isActive ? _unlockPhrase : null,
            IsFocusUnlockPhraseRequired = _isActive && _focusLockService.IsPhraseRequired(_focusLockUntilUtc, nowUtc),
            IsGuardianHealthy = _isActive && _guardianService.IsHealthy(),
            IsAdmin = AdminService.IsAdministrator()
        });
    }

    private async Task RollbackPartialEnableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _watchdog.StopAsync(cancellationToken);
            await _guardianService.StopAsync(cancellationToken);
        }
        catch
        {
            // Best effort rollback.
        }

        foreach (var provider in _providers)
        {
            try
            {
                await provider.RemoveAsync(cancellationToken);
            }
            catch
            {
                // Best effort rollback.
            }
        }

        _isActive = false;
        _activatedAtUtc = null;
        _focusLockUntilUtc = null;
        _unlockPhrase = null;
        _guardianExpectedRunning = false;
        await PersistCurrentStateAsync(lastShutdownClean: false, cancellationToken);
    }

    private async Task PersistCurrentStateAsync(bool lastShutdownClean, CancellationToken cancellationToken)
    {
        await _stateStore.SaveAsync(
            new PersistedState
            {
                IsBlockActive = _isActive,
                ActivatedAtUtc = _activatedAtUtc,
                FocusLockUntilUtc = _focusLockUntilUtc,
                UnlockPhrase = _isActive ? _unlockPhrase : null,
                GuardianExpectedRunning = _guardianExpectedRunning,
                LastShutdownClean = lastShutdownClean,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    private static BlockResult BuildResult(List<string> messages, List<string> errors)
    {
        return new BlockResult
        {
            Success = errors.Count == 0,
            Messages = messages,
            Errors = errors
        };
    }
}
