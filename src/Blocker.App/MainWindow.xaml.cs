using System.ComponentModel;
using System.Windows;
using Blocker.App.ViewModels;

namespace Blocker.App;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
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
        var dialog = new UnlockPhraseWindow(UnlockPhraseWindowMode.Setup)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.EnteredPhrase : null);
    }

    private Task<string?> RequestUnlockPhraseAsync(string? requiredPhrase)
    {
        var dialog = new UnlockPhraseWindow(UnlockPhraseWindowMode.Verify, requiredPhrase)
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
}
