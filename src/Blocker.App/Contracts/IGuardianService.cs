namespace Blocker.App.Contracts;

public interface IGuardianService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsHealthy();
}
