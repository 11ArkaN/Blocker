using Blocker.App.Contracts;
using Blocker.App.Constants;

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

    public bool ValidatePhrase(string? input, string? expectedPhrase)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(expectedPhrase))
        {
            return false;
        }

        return string.Equals(
            input.Trim(),
            expectedPhrase.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
