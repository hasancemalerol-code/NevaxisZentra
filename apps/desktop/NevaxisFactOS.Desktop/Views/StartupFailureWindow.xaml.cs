using System.Diagnostics;
using System.Windows;
using NevaxisFactOS.Desktop.Runtime;

namespace NevaxisFactOS.Desktop.Views;

public partial class StartupFailureWindow : Window
{
    private readonly StartupSession? _session;
    private readonly IReadOnlyList<string> _diagnostics;

    public StartupFailureWindow(
        string summary,
        IEnumerable<string> diagnostics,
        string? errorCode = null,
        StartupSession? session = null)
    {
        InitializeComponent();
        _session = session;
        _diagnostics = diagnostics.ToList();
        SummaryText.Text = summary;
        ErrorCodeText.Text = string.IsNullOrWhiteSpace(errorCode)
            ? string.Empty
            : $"Hata Kodu: {errorCode}";
        DiagnosticsText.Text = string.Join(Environment.NewLine, _diagnostics);
    }

    public event EventHandler? RetryRequested;

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        RetryRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var bundlePath = FactOsDiagnosticsBundleService.CreateSupportBundle(
                _session,
                _diagnostics,
                _session?.ErrorCode);
            DiagnosticsBundleText.Text = $"Tanılama paketi oluşturuldu:\n{bundlePath}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{bundlePath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            DiagnosticsBundleText.Text = $"Tanılama paketi oluşturulamadı: {ex.Message}";
        }
    }
}
