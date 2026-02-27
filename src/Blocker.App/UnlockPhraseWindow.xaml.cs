using System.Windows;
using Blocker.App.Contracts;

namespace Blocker.App;

public enum UnlockPhraseWindowMode
{
    Setup,
    Verify
}

public partial class UnlockPhraseWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly ILocalizationService _localizationService;
    private readonly UnlockPhraseWindowMode _mode;
    private readonly string? _referencePhrase;

    public UnlockPhraseWindow(ILocalizationService localizationService, UnlockPhraseWindowMode mode, string? referencePhrase = null)
    {
        InitializeComponent();
        _localizationService = localizationService;
        _mode = mode;
        _referencePhrase = referencePhrase;

        _localizationService.LanguageChanged += HandleLanguageChanged;
        Configure(_mode, _referencePhrase);
        Loaded += (_, _) => PhraseTextBox.Focus();
        Closed += (_, _) => _localizationService.LanguageChanged -= HandleLanguageChanged;
    }

    public string? EnteredPhrase { get; private set; }

    private void HandleConfirmClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var phrase = PhraseTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(phrase))
        {
            System.Windows.MessageBox.Show(
                _localizationService["Unlock.EmptyPhraseWarning"],
                "Blocker",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            PhraseTextBox.Focus();
            return;
        }

        EnteredPhrase = phrase;
        DialogResult = true;
        Close();
    }

    private void HandleCancelClick(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Configure(UnlockPhraseWindowMode mode, string? referencePhrase)
    {
        CancelButton.Content = _localizationService["Common.Cancel"];

        if (mode == UnlockPhraseWindowMode.Setup)
        {
            Title = _localizationService["Unlock.SetupWindowTitle"];
            DialogTitleBar.Title = _localizationService["Unlock.SetupTitleBar"];
            DialogSectionTitleTextBlock.Text = _localizationService["Unlock.SetupSectionTitle"];
            DialogMessageTitleTextBlock.Text = _localizationService["Unlock.SetupMessageTitle"];
            DialogMessageTextBlock.Text = _localizationService["Unlock.SetupMessageBody"];
            ReferencePhraseContainer.Visibility = Visibility.Collapsed;
            PhraseTextBox.PlaceholderText = _localizationService["Unlock.SetupPlaceholder"];
            ConfirmButton.Content = _localizationService["Unlock.SetupConfirm"];
            return;
        }

        Title = _localizationService["Unlock.VerifyWindowTitle"];
        DialogTitleBar.Title = _localizationService["Unlock.VerifyTitleBar"];
        DialogSectionTitleTextBlock.Text = _localizationService["Unlock.VerifySectionTitle"];
        DialogMessageTitleTextBlock.Text = _localizationService["Unlock.VerifyMessageTitle"];
        DialogMessageTextBlock.Text = _localizationService["Unlock.VerifyMessageBody"];
        ReferencePhraseContainer.Visibility = Visibility.Visible;
        ReferencePhraseTextBlock.Text = string.IsNullOrWhiteSpace(referencePhrase) ? "-" : referencePhrase;
        PhraseTextBox.PlaceholderText = _localizationService["Unlock.VerifyPlaceholder"];
        ConfirmButton.Content = _localizationService["Unlock.VerifyConfirm"];
    }

    private void HandleLanguageChanged(object? sender, EventArgs e)
    {
        Configure(_mode, _referencePhrase);
    }
}
