using Blocker.App.Models;

namespace Blocker.App.Contracts;

public interface IProcessWatchdog
{
    Task StartAsync(BlockTargets targets, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
