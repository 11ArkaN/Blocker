using System.Windows;

namespace Blocker.App;

public enum UnlockPhraseWindowMode
{
    Setup,
    Verify
}

public partial class UnlockPhraseWindow : Wpf.Ui.Controls.FluentWindow
{
    public UnlockPhraseWindow(UnlockPhraseWindowMode mode, string? referencePhrase = null)
    {
        InitializeComponent();
        Configure(mode, referencePhrase);
        Loaded += (_, _) => PhraseTextBox.Focus();
    }

    public string? EnteredPhrase { get; private set; }

    private void HandleConfirmClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var phrase = PhraseTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(phrase))
        {
            System.Windows.MessageBox.Show(
                "Fraza nie moze byc pusta.",
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
        if (mode == UnlockPhraseWindowMode.Setup)
        {
            Title = "Ustaw fraze";
            DialogTitleBar.Title = "Ustaw fraze Focus Lock";
            DialogSectionTitleTextBlock.Text = "Ustaw fraze do wczesnego odblokowania";
            DialogMessageTitleTextBlock.Text = "Nowa sesja";
            DialogMessageTextBlock.Text = "Przed wlaczeniem blokady ustaw fraze. Bedzie wymagana do wylaczenia przed uplywem 30 minut.";
            ReferencePhraseContainer.Visibility = Visibility.Collapsed;
            PhraseTextBox.PlaceholderText = "Ustaw fraze...";
            ConfirmButton.Content = "Ustaw i wlacz";
            return;
        }

        Title = "Potwierdzenie";
        DialogTitleBar.Title = "Potwierdzenie Focus Lock";
        DialogSectionTitleTextBlock.Text = "Wpisz fraze potwierdzajaca";
        DialogMessageTitleTextBlock.Text = "Wczesne zakonczenie sesji";
        DialogMessageTextBlock.Text = "Aby zakonczyc blokade przed uplywem 30 minut, wpisz dokladnie wymagana fraze.";
        ReferencePhraseContainer.Visibility = Visibility.Visible;
        ReferencePhraseTextBlock.Text = string.IsNullOrWhiteSpace(referencePhrase) ? "-" : referencePhrase;
        PhraseTextBox.PlaceholderText = "Wpisz fraze...";
        ConfirmButton.Content = "Potwierdz i wylacz";
    }
}
