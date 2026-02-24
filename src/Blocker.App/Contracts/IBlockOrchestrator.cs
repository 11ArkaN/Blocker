using Blocker.App.Models;

namespace Blocker.App.Contracts;

public interface IBlockOrchestrator
{
    Task<BlockResult> EnableAsync(string unlockPhrase, CancellationToken cancellationToken = default);
    Task<BlockResult> DisableAsync(
        string? unlockPhrase = null,
        bool bypassFocusLock = false,
        CancellationToken cancellationToken = default);
    Task<BlockState> GetStateAsync(CancellationToken cancellationToken = default);
}
