using Blocker.App.Constants;
using Blocker.App.Contracts;
using Microsoft.Win32;

namespace Blocker.App.Services;

public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly ILogService _logger;

    public StartupService(ILogService logger)
    {
        _logger = logger;
    }

    public void EnableAutoStart(string executablePath, bool startInTray)
    {
        try
        {
            var startupArgs = startInTray ? " --tray" : string.Empty;
            var value = $"\"{executablePath}\"{startupArgs}";

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            key.SetValue(BlockerConstants.StartupRegistryValue, value, RegistryValueKind.String);
            _logger.Info("Autostart enabled.");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to enable autostart.", ex);
        }
    }

    public void DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(BlockerConstants.StartupRegistryValue, throwOnMissingValue: false);
            _logger.Info("Autostart disabled.");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to disable autostart.", ex);
        }
    }

    public bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(BlockerConstants.StartupRegistryValue) as string;
        return !string.IsNullOrWhiteSpace(value);
    }
}
