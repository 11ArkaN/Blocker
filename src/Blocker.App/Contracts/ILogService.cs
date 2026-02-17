namespace Blocker.App.Contracts;

public interface ILogService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Error(string message, Exception exception);
}
