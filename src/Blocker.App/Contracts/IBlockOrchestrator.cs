using Blocker.App.Models;

namespace Blocker.App.Contracts;

public interface IBlockOrchestrator
{
    Task<BlockResult> EnableAsync(CancellationToken cancellationToken = default);
    Task<BlockResult> DisableAsync(string? unlockPhrase = null, CancellationToken cancellationToken = default);
    Task<BlockState> GetStateAsync(CancellationToken cancellationToken = default);
}
