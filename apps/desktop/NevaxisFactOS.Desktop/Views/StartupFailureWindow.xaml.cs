using System.Windows;

namespace NevaxisFactOS.Desktop.Views;

public partial class StartupFailureWindow : Window
{
    public StartupFailureWindow(string summary, IEnumerable<string> diagnostics)
    {
        InitializeComponent();
        SummaryText.Text = summary;
        DiagnosticsText.Text = string.Join(Environment.NewLine, diagnostics);
    }

    public event EventHandler? RetryRequested;

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        RetryRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
