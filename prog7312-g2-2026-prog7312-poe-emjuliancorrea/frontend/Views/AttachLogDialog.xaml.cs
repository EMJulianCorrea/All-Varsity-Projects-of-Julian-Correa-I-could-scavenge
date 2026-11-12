using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Smart_X.frontend.Models;
using Smart_X.frontend.Services;

namespace Smart_X.frontend.Views;

public partial class AttachLogDialog : Window
{
    private readonly SensorRecord _sensor;
    private string? _filePath;

    public SensorLog? UploadedLog { get; private set; }

    public AttachLogDialog(SensorRecord sensor)
    {
        InitializeComponent();
        _sensor = sensor;

        TargetText.Text = $"Attaching to sensor {sensor.Id}  ({sensor.DeploymentLocation})";
        ClassificationBox.ItemsSource = Enum.GetValues<LogClassification>()
            .Select(value => new ClassificationChoice(value, value == LogClassification.RawData ? "Raw Data" : value.ToString()))
            .ToList();
        ClassificationBox.DisplayMemberPath = nameof(ClassificationChoice.Label);
        ClassificationBox.SelectedValuePath = nameof(ClassificationChoice.Value);
        ClassificationBox.SelectedValue = LogClassification.Other;
        TipText.Visibility = MainWindow.GuidanceEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"Choose a hardware log for {_sensor.Id}",
            Filter = LogFileRules.OpenFileFilter,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true) return;
        await LoadFileAsync(dialog.FileName);
    }

    private async Task LoadFileAsync(string path)
    {
        _filePath = null;
        UploadButton.IsEnabled = false;
        FilePathBox.Text = path;
        FileInfoText.Text = string.Empty;
        StatusText.Text = string.Empty;

        var info = new FileInfo(path);
        var problem = LogFileRules.Validate(info);
        if (problem is not null)
        {
            SuggestionText.Text = "Choose a file and a classification will be suggested.";
            ShowStatus(problem, isError: true);
            return;
        }

        FileInfoText.Text = $"{info.Name}   ·   {LogFileRules.FormatSize(info.Length)}";
        SuggestionText.Text = "Checking the file to suggest a classification...";

        string sample;
        try
        {
            await using var stream = File.OpenRead(path);
            var buffer = new byte[4096];
            var read = await stream.ReadAsync(buffer.AsMemory());
            sample = Encoding.UTF8.GetString(buffer, 0, read);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SuggestionText.Text = string.Empty;
            ShowStatus($"Could not read that file: {ex.Message}", isError: true);
            return;
        }

        var (success, _, result) = await MainWindow.ApiClient.ClassifyLogAsync(info.Name, sample);
        if (success && result is not null)
        {
            ClassificationBox.SelectedValue = result.Classification;
            SuggestionText.Text = $"Suggested: {result.Classification}, because {result.Reason} Change it above if that's wrong.";
        }
        else
        {
            ClassificationBox.SelectedValue = LogClassification.Other;
            SuggestionText.Text = "Couldn't get a suggestion from the API. Choose the classification yourself.";
        }

        _filePath = path;
        UploadButton.IsEnabled = true;
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (_filePath is null || ClassificationBox.SelectedValue is not LogClassification classification) return;
        var currentProblem = LogFileRules.Validate(new FileInfo(_filePath));
        if (currentProblem is not null)
        {
            ShowStatus(currentProblem, isError: true);
            UploadButton.IsEnabled = false;
            return;
        }

        SetBusy(true);
        ShowStatus("Uploading...", isError: false);

        var (success, error, log) = await MainWindow.ApiClient.UploadLogAsync(_sensor.Id, _filePath, classification);

        if (success)
        {
            UploadedLog = log;
            DialogResult = true;
            return;
        }

        SetBusy(false);
        ShowStatus(error ?? "The upload failed.", isError: true);
    }

    private void SetBusy(bool busy)
    {
        BrowseButton.IsEnabled = !busy;
        ClassificationBox.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        UploadButton.IsEnabled = !busy && _filePath is not null;
    }

    private sealed record ClassificationChoice(LogClassification Value, string Label);

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = (Brush)FindResource(isError ? "DangerBrush" : "OkBrush");
    }
}
