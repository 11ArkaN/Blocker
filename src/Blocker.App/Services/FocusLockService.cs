using Blocker.App.Constants;
using Blocker.App.Contracts;

namespace Blocker.App.Services;

public sealed class FocusLockService : IFocusLockService
{
    private static readonly TimeSpan FocusDuration = TimeSpan.FromMinutes(BlockerConstants.FocusLockMinutes);

    public DateTimeOffset ComputeLockEnd(DateTimeOffset activatedAtUtc)
    {
        return activatedAtUtc + FocusDuration;
    }

    public bool IsPhraseRequired(DateTimeOffset? lockEndUtc, DateTimeOffset nowUtc)
    {
        return lockEndUtc.HasValue && nowUtc < lockEndUtc.Value;
    }

    public bool ValidatePhrase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return string.Equals(
            input.Trim(),
            BlockerConstants.UnlockPhrase,
            StringComparison.OrdinalIgnoreCase);
    }
}
