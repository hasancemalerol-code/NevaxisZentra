using System.Windows;

namespace NevaxisZentra.Desktop.Views;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
    }

    public void SetStatus(string status)
    {
        StatusText.Text = status;
    }
}
