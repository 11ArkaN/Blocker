using System.Text;
using Blocker.App.Constants;
using Blocker.App.Contracts;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class FirewallProgramBlockingProvider : IBlockingProvider
{
    private readonly ProcessRunner _processRunner;
    private readonly ILogService _logger;

    public FirewallProgramBlockingProvider(ProcessRunner processRunner, ILogService logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task ApplyAsync(BlockTargets targets, CancellationToken cancellationToken = default)
    {
        await RemoveAsync(cancellationToken);

        var executablePaths = FilePatternResolver
            .ResolvePaths(targets.ExecutableHints)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (executablePaths.Count == 0)
        {
            _logger.Warn("No Discord/Messenger executables found for firewall program rules.");
            return;
        }

        var script = BuildAddRulesScript(executablePaths);
        var result = await RunPowerShellAsync(script, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Firewall apply failed: {result.StandardError}".Trim());
        }

        _logger.Info($"Firewall rules applied for {executablePaths.Count} executable(s).");
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        var script = $@"
Get-NetFirewallRule -DisplayName '{BlockerConstants.FirewallRulePrefix}*' -ErrorAction SilentlyContinue |
    Remove-NetFirewallRule -ErrorAction SilentlyContinue
";
        var result = await RunPowerShellAsync(script, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.Warn($"Firewall cleanup reported issues: {result.StandardError}".Trim());
        }
    }

    private async Task<CommandResult> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}";
        return await _processRunner.RunAsync("powershell", args, cancellationToken: cancellationToken);
    }

    private static string BuildAddRulesScript(IReadOnlyList<string> executablePaths)
    {
        var lines = new List<string>
        {
            "$ErrorActionPreference = 'Stop'",
            $"$rulePrefix = '{BlockerConstants.FirewallRulePrefix}Program_'",
            "$paths = @("
        };

        foreach (var path in executablePaths)
        {
            lines.Add($"    '{EscapePowerShellSingleQuote(path)}'");
        }

        lines.Add(")");
        lines.Add("$index = 1");
        lines.Add("foreach ($path in $paths) {");
        lines.Add("    $outName = '{0}{1:D3}_Out' -f $rulePrefix, $index");
        lines.Add("    $inName  = '{0}{1:D3}_In'  -f $rulePrefix, $index");
        lines.Add("    New-NetFirewallRule -DisplayName $outName -Direction Outbound -Action Block -Program $path -Profile Any -Enabled True | Out-Null");
        lines.Add("    New-NetFirewallRule -DisplayName $inName  -Direction Inbound  -Action Block -Program $path -Profile Any -Enabled True | Out-Null");
        lines.Add("    $index++");
        lines.Add("}");

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapePowerShellSingleQuote(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
