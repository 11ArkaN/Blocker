namespace Blocker.App.Models;

public sealed class BlockTargets
{
    public IReadOnlyList<string> Domains { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ProcessNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExecutableHints { get; init; } = Array.Empty<string>();
}
