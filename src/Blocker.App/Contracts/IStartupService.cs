namespace Blocker.App.Contracts;

public interface IStartupService
{
    void EnableAutoStart(string executablePath, bool startInTray);
    void DisableAutoStart();
    bool IsAutoStartEnabled();
}
