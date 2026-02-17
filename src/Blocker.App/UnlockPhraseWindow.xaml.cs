namespace Blocker.App;

public partial class UnlockPhraseWindow : System.Windows.Window
{
    public UnlockPhraseWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PhraseTextBox.Focus();
    }

    public string? EnteredPhrase { get; private set; }

    private void HandleConfirmClick(object sender, System.Windows.RoutedEventArgs e)
    {
        EnteredPhrase = PhraseTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void HandleCancelClick(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
