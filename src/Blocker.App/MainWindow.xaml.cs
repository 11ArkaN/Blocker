using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Blocker.App.Contracts;
using Blocker.App.ViewModels;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace Blocker.App;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly ILocalizationService _localizationService;

    public MainWindow(MainWindowViewModel viewModel, ILocalizationService localizationService)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _localizationService = localizationService;
        DataContext = viewModel;
        viewModel.RequestSetupUnlockPhraseAsync = RequestSetupUnlockPhraseAsync;
        viewModel.RequestUnlockPhraseAsync = RequestUnlockPhraseAsync;
        StateChanged += HandleStateChanged;
    }

    public MainWindowViewModel ViewModel { get; }

    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            WindowState = WindowState.Normal;
            return;
        }

        base.OnClosing(e);
    }

    private Task<string?> RequestSetupUnlockPhraseAsync()
    {
        var dialog = new UnlockPhraseWindow(_localizationService, UnlockPhraseWindowMode.Setup)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.EnteredPhrase : null);
    }

    private Task<string?> RequestUnlockPhraseAsync(string? requiredPhrase)
    {
        var dialog = new UnlockPhraseWindow(_localizationService, UnlockPhraseWindowMode.Verify, requiredPhrase)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.EnteredPhrase : null);
    }

    private void HandleStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private void HandleLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not WpfComboBox comboBox || comboBox.SelectedItem is not WpfComboBoxItem item)
        {
            return;
        }

        if (item.Tag is string languageCode && !string.IsNullOrWhiteSpace(languageCode))
        {
            ViewModel.SelectedLanguageCode = languageCode;
        }
    }
}
