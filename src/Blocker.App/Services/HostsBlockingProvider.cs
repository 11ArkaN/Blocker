using System.Text;
using Blocker.App.Constants;
using Blocker.App.Contracts;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class HostsBlockingProvider : IBlockingProvider
{
    private readonly ILogService _logger;
    private readonly string _hostsPath;

    public HostsBlockingProvider(ILogService logger)
    {
        _logger = logger;
        _hostsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers",
            "etc",
            "hosts");
    }

    public async Task ApplyAsync(BlockTargets targets, CancellationToken cancellationToken = default)
    {
        var existingLines = await ReadHostsLinesAsync(cancellationToken);
        var cleaned = RemoveManagedSection(existingLines);

        var domains = targets.Domains
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Select(domain => domain.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(domain => domain, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleaned.Count > 0 && !string.IsNullOrWhiteSpace(cleaned[^1]))
        {
            cleaned.Add(string.Empty);
        }

        cleaned.Add(BlockerConstants.HostsSectionBegin);
        foreach (var domain in domains)
        {
            cleaned.Add($"0.0.0.0 {domain}");
            cleaned.Add($"::1 {domain}");
        }

        cleaned.Add(BlockerConstants.HostsSectionEnd);
        await File.WriteAllLinesAsync(_hostsPath, cleaned, new UTF8Encoding(false), cancellationToken);
        _logger.Info($"Hosts section applied with {domains.Count} blocked domain(s).");
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        var existingLines = await ReadHostsLinesAsync(cancellationToken);
        var cleaned = RemoveManagedSection(existingLines);
        await File.WriteAllLinesAsync(_hostsPath, cleaned, new UTF8Encoding(false), cancellationToken);
        _logger.Info("Hosts section removed.");
    }

    private async Task<List<string>> ReadHostsLinesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_hostsPath))
        {
            return new List<string>();
        }

        var lines = await File.ReadAllLinesAsync(_hostsPath, cancellationToken);
        return lines.ToList();
    }

    private static List<string> RemoveManagedSection(IReadOnlyList<string> lines)
    {
        var output = new List<string>();
        var insideSection = false;

        foreach (var line in lines)
        {
            if (line.Trim().Equals(BlockerConstants.HostsSectionBegin, StringComparison.Ordinal))
            {
                insideSection = true;
                continue;
            }

            if (insideSection && line.Trim().Equals(BlockerConstants.HostsSectionEnd, StringComparison.Ordinal))
            {
                insideSection = false;
                continue;
            }

            if (!insideSection)
            {
                output.Add(line);
            }
        }

        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[^1]))
        {
            output.RemoveAt(output.Count - 1);
        }

        return output;
    }
}
