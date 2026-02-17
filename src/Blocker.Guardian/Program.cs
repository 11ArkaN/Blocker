using System.Diagnostics;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var parsed = ParseArgs(args);
        if (!parsed.TryGetValue("--monitor-pid", out var monitorPidRaw) ||
            !parsed.TryGetValue("--app-path", out var appPath) ||
            !parsed.TryGetValue("--token", out var token) ||
            !parsed.TryGetValue("--stop-file", out var stopFile))
        {
            return 2;
        }

        if (!int.TryParse(monitorPidRaw, out var monitorPid) || monitorPid <= 0)
        {
            return 3;
        }

        using var mutex = new Mutex(initiallyOwned: true, name: $"Local\\BlockerGuardian_{token}", createdNew: out var createdNew);
        if (!createdNew)
        {
            return 0;
        }

        while (true)
        {
            if (File.Exists(stopFile))
            {
                return 0;
            }

            if (IsProcessAlive(monitorPid))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }

            TryRestartApplication(appPath, token);
            return 0;
        }
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var alive = !process.HasExited;
            process.Dispose();
            return alive;
        }
        catch
        {
            return false;
        }
    }

    private static void TryRestartApplication(string appPath, string token)
    {
        if (!File.Exists(appPath))
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = appPath,
            Arguments = $"--tray --resume-guarded --guardian-token {token}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);
    }

    private static Dictionary<string, string> ParseArgs(IReadOnlyList<string> args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Count; i++)
        {
            var key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Count)
            {
                continue;
            }

            result[key] = args[i + 1];
            i++;
        }

        return result;
    }
}
