using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Smart_X.frontend;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Smart-X", "crash.log");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteLog(e.ExceptionObject as Exception);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog(e.Exception);

        MessageBox.Show(
            $"Smart-X ran into a problem:\n\n{Describe(e.Exception)}\n\nFull details were saved to:\n{LogPath}",
            "Smart-X error", MessageBoxButton.OK, MessageBoxImage.Error);

        e.Handled = true;

        if (Windows.Count == 0) Shutdown(1);
    }

    private static string Describe(Exception ex)
    {
        var text = new StringBuilder();
        for (var current = ex; current is not null; current = current.InnerException)
            text.AppendLine(current.Message);
        return text.ToString().TrimEnd();
    }

    private static void WriteLog(Exception? ex)
    {
        if (ex is null) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n");
        }
        catch (Exception logEx) when (logEx is IOException or UnauthorizedAccessException)
        {
            // If the log can't be written there is nothing more to do; the message box still appears.
        }
    }
}
