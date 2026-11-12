using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Smart_X.frontend.Models;

namespace Smart_X.frontend.Views;

public partial class SensorLogsWindow : Window
{
    private const string AllLabel = "All classifications";

    private readonly SensorRecord _sensor;
    private readonly bool _ready;

    public SensorLogsWindow(SensorRecord sensor)
    {
        InitializeComponent();
        _sensor = sensor;

        HeaderText.Text = $"Hardware logs for {sensor.Id}";
        FilterBox.ItemsSource = new[] { AllLabel }.Concat(Enum.GetValues<LogClassification>()
            .Select(value => value == LogClassification.RawData ? "Raw Data" : value.ToString())).ToList();
        FilterBox.SelectedIndex = 0;
        HintText.Visibility = MainWindow.GuidanceEnabled ? Visibility.Visible : Visibility.Collapsed;

        _ready = true;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadLogsAsync();

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_ready) await LoadLogsAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadLogsAsync();

    private LogClassification? CurrentFilter =>
        FilterBox.SelectedIndex > 0 && FilterBox.SelectedItem is string name
            ? Enum.GetValues<LogClassification>().FirstOrDefault(value =>
                string.Equals(value == LogClassification.RawData ? "Raw Data" : value.ToString(), name, StringComparison.Ordinal))
            : null;

    private async Task LoadLogsAsync()
    {
        var filter = CurrentFilter;
        var logs = await MainWindow.ApiClient.GetLogsAsync(_sensor.Id, filter);

        if (logs is null)
        {
            ShowStatus("Could not load the logs. Is the API running?", isError: true);
            return;
        }

        LogGrid.ItemsSource = logs;
        EmptyText.Text = filter is null
            ? "No logs attached yet. Click Attach Log to add one."
            : $"No {filter} logs for this sensor.";
        EmptyText.Visibility = logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Attach_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AttachLogDialog(_sensor) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        var log = dialog.UploadedLog;
        ShowStatus(log is null
            ? "Log attached."
            : $"Attached {log.FileName} as {log.ClassificationText}.", isError: false);
        await LoadLogsAsync();
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (LogGrid.SelectedItem is not SensorLog log)
        {
            ShowStatus("Select a log to download first.", isError: true);
            return;
        }

        var ext = Path.GetExtension(log.FileName);
        var dialog = new SaveFileDialog
        {
            Title = "Save hardware log",
            FileName = log.FileName,
            OverwritePrompt = true,
            Filter = ext.Length > 0
                ? $"{ext} files (*{ext})|*{ext}|All files (*.*)|*.*"
                : "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true) return;

        ShowStatus("Downloading...", isError: false);
        var (success, error) = await MainWindow.ApiClient.DownloadLogAsync(_sensor.Id, log.LogId, dialog.FileName);

        ShowStatus(success
            ? $"Saved {log.FileName} to {dialog.FileName}"
            : error ?? "The download failed.", isError: !success);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (LogGrid.SelectedItem is not SensorLog log)
        {
            ShowStatus("Select a log to delete first.", isError: true);
            return;
        }

        var confirm = MessageBox.Show(this, $"Delete '{log.FileName}'? This cannot be undone.",
            "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var (success, error) = await MainWindow.ApiClient.DeleteLogAsync(_sensor.Id, log.LogId);
        ShowStatus(success ? $"Deleted {log.FileName}." : error ?? "The log could not be deleted.", isError: !success);
        if (success) await LoadLogsAsync();
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = (Brush)FindResource(isError ? "DangerBrush" : "OkBrush");
    }
}
