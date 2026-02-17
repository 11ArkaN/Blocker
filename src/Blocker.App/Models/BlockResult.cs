namespace Blocker.App.Models;

public sealed class BlockResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
