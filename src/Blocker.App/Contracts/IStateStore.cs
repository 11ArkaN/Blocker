using Blocker.App.Models;

namespace Blocker.App.Contracts;

public interface IStateStore
{
    Task<PersistedState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PersistedState state, CancellationToken cancellationToken = default);
    Task MarkSessionStartedAsync(CancellationToken cancellationToken = default);
    Task MarkSessionEndedAsync(
        bool isBlockActive,
        DateTimeOffset? activatedAt,
        DateTimeOffset? focusLockUntil,
        string? unlockPhrase,
        bool guardianExpectedRunning,
        bool lastShutdownClean,
        CancellationToken cancellationToken = default);
}
