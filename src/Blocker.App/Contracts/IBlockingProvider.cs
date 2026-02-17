using Blocker.App.Models;

namespace Blocker.App.Contracts;

public interface IBlockingProvider
{
    Task ApplyAsync(BlockTargets targets, CancellationToken cancellationToken = default);
    Task RemoveAsync(CancellationToken cancellationToken = default);
}
