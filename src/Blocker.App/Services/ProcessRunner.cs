using System.Diagnostics;
using System.Text;
using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class ProcessRunner
{
    public async Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        int timeoutMs = 45000,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                stdOut.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                stdErr.AppendLine(args.Data);
            }
        };

        if (!process.Start())
        {
            return new CommandResult
            {
                ExitCode = -1,
                StandardError = $"Failed to start process {fileName}."
            };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new CommandResult
            {
                ExitCode = -2,
                StandardError = $"Process timed out after {timeoutMs} ms."
            };
        }

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdOut.ToString(),
            StandardError = stdErr.ToString()
        };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
