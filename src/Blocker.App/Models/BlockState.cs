namespace Blocker.App.Models;

public sealed class BlockState
{
    public bool IsActive { get; init; }
    public DateTimeOffset? ActivatedAt { get; init; }
    public DateTimeOffset? FocusLockUntil { get; init; }
    public string? UnlockPhrase { get; init; }
    public bool IsFocusUnlockPhraseRequired { get; init; }
    public bool IsGuardianHealthy { get; init; }
    public bool IsAdmin { get; init; }
}
