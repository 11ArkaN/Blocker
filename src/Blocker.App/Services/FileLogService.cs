using System.Text;
using Blocker.App.Contracts;

namespace Blocker.App.Services;

public sealed class FileLogService : ILogService
{
    private readonly object _gate = new();
    private readonly string _logPath;
    private const long MaxSizeBytes = 5 * 1024 * 1024;
    private const int MaxArchives = 3;

    public FileLogService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Blocker",
            "logs");

        Directory.CreateDirectory(root);
        _logPath = Path.Combine(root, "app.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    public void Error(string message, Exception exception) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            RotateIfNeeded();

            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
            File.AppendAllText(_logPath, line + Environment.NewLine, new UTF8Encoding(false));
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logPath))
        {
            return;
        }

        var fileInfo = new FileInfo(_logPath);
        if (fileInfo.Length < MaxSizeBytes)
        {
            return;
        }

        for (var i = MaxArchives - 1; i >= 1; i--)
        {
            var source = $"{_logPath}.{i}";
            var destination = $"{_logPath}.{i + 1}";

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            if (File.Exists(source))
            {
                File.Move(source, destination);
            }
        }

        var firstArchive = $"{_logPath}.1";
        if (File.Exists(firstArchive))
        {
            File.Delete(firstArchive);
        }

        File.Move(_logPath, firstArchive);
    }
}
