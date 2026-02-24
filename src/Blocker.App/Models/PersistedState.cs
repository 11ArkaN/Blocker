namespace Blocker.App.Models;

public sealed class PersistedState
{
    public bool IsBlockActive { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public DateTimeOffset? FocusLockUntilUtc { get; set; }
    public string? UnlockPhrase { get; set; }
    public bool GuardianExpectedRunning { get; set; }
    public bool LastShutdownClean { get; set; } = true;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
