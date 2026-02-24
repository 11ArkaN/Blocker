using System.Text;
using System.Text.Json;
using Blocker.App.Contracts;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class JsonStateStore : IStateStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogService _logger;
    private readonly string _statePath;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public JsonStateStore(ILogService logger)
    {
        _logger = logger;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Blocker");
        Directory.CreateDirectory(root);
        _statePath = Path.Combine(root, "state.json");
    }

    public async Task<PersistedState> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(PersistedState state, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveUnsafeAsync(state, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkSessionStartedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadUnsafeAsync(cancellationToken);
            state.LastShutdownClean = false;
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveUnsafeAsync(state, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkSessionEndedAsync(
        bool isBlockActive,
        DateTimeOffset? activatedAt,
        DateTimeOffset? focusLockUntil,
        string? unlockPhrase,
        bool guardianExpectedRunning,
        bool lastShutdownClean,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadUnsafeAsync(cancellationToken);
            state.IsBlockActive = isBlockActive;
            state.ActivatedAtUtc = activatedAt?.ToUniversalTime();
            state.FocusLockUntilUtc = focusLockUntil?.ToUniversalTime();
            state.UnlockPhrase = unlockPhrase;
            state.GuardianExpectedRunning = guardianExpectedRunning;
            state.LastShutdownClean = lastShutdownClean;
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await SaveUnsafeAsync(state, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PersistedState> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_statePath))
        {
            return new PersistedState();
        }

        try
        {
            await using var stream = new FileStream(_statePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var state = await JsonSerializer.DeserializeAsync<PersistedState>(stream, _serializerOptions, cancellationToken);
            return state ?? new PersistedState();
        }
        catch (Exception ex)
        {
            _logger.Error("Could not read persisted state. Falling back to defaults.", ex);
            return new PersistedState();
        }
    }

    private async Task SaveUnsafeAsync(PersistedState state, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, _serializerOptions);
            await File.WriteAllTextAsync(_statePath, json, new UTF8Encoding(false), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Could not save persisted state.", ex);
            throw;
        }
    }
}
