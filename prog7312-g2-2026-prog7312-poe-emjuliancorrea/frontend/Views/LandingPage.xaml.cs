using System.Windows;
using System.Windows.Controls;

namespace Smart_X.frontend.Views;

public partial class LandingPage : Page
{
    private readonly MainWindow _owner;

    public LandingPage(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;
    }

    private void SensorTile_Click(object sender, RoutedEventArgs e)
    {
        _owner.NavigateTo(new RegisterDevice());
    }

    private void CommandStreamTile_Click(object sender, RoutedEventArgs e)
    {
        _owner.NavigateTo(new CommandStream());
    }
    private void NetworkTopologyTile_Click(object sender, RoutedEventArgs e)
    {
        _owner.NavigateTo(new NetworkTopology());
    }

    private void UnderContructionTile_Click(object sender, RoutedEventArgs e)
    {
        var tileName = (sender as Button)?.Name switch
        {
            "NetworkTopologyTile" => "Network Topology and Mesh Routing",
            _ => "This section"
        };

        MessageBox.Show(
            $"{tileName} is on the roadmap and isn't wired up yet.\n\nFor now, try \"Sensor Data Ingestion and Telemetry\" to register sensors and simulate readings.",
            "Coming soon",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}