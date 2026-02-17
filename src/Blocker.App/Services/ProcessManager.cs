using System.Diagnostics;
using Blocker.App.Contracts;

namespace Blocker.App.Services;

public sealed class ProcessManager
{
    private readonly ILogService _logger;

    public ProcessManager(ILogService logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> KillProcesses(IEnumerable<string> processNames)
    {
        var killed = new List<string>();
        var normalizedNames = processNames
            .Select(name => Path.GetFileNameWithoutExtension(name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var processName in normalizedNames)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Unable to list processes for {processName}: {ex.Message}");
                continue;
            }

            foreach (var process in processes)
            {
                try
                {
                    process.Kill(true);
                    process.WaitForExit(2000);
                    killed.Add($"{process.ProcessName} ({process.Id})");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Unable to terminate process {process.ProcessName} ({process.Id}): {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return killed;
    }
}
