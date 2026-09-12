using System.Windows.Input;

namespace NevaxisFactOS.Desktop;

public static class KeyboardCommands
{
    public static bool TryHandle(KeyEventArgs e, MainWindow window) =>
        TryHandle(e.Key, Keyboard.Modifiers, window);

    public static bool TryHandle(Key key, ModifierKeys modifiers, MainWindow window)
    {
        if (key == Key.F11)
        {
            window.ToggleFullscreen();
            return true;
        }

        if (key == Key.Escape && window.IsFullscreen)
        {
            window.ToggleFullscreen();
            return true;
        }

        if (key == Key.F5
            || (key == Key.R && modifiers == ModifierKeys.Control))
        {
            window.ExecuteRefresh();
            return true;
        }

        if (key == Key.Left && modifiers == ModifierKeys.Alt)
        {
            window.ExecuteBack();
            return true;
        }

        if (key == Key.Right && modifiers == ModifierKeys.Alt)
        {
            window.ExecuteForward();
            return true;
        }

        return false;
    }
}
