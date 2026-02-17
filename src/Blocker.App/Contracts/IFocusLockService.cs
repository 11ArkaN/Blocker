namespace Blocker.App.Contracts;

public interface IFocusLockService
{
    DateTimeOffset ComputeLockEnd(DateTimeOffset activatedAtUtc);
    bool IsPhraseRequired(DateTimeOffset? lockEndUtc, DateTimeOffset nowUtc);
    bool ValidatePhrase(string? input);
}
