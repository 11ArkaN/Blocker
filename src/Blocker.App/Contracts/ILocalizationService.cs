using System.ComponentModel;

namespace Blocker.App.Contracts;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguageCode { get; }
    string this[string key] { get; }
    event EventHandler? LanguageChanged;
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken = default);
}
