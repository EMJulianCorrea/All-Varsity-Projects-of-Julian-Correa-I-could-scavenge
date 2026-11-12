using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Smart_X.frontend.Models;

namespace Smart_X.frontend.Views;

public partial class NetworkTopology : Page
{
    private List<SensorRecord> _sensors = new();
    private List<NodeRow> _topologyNodes = new();
    private List<EdgeRow> _topologyEdges = new();
    private HashSet<string> _mstEdges = new(StringComparer.OrdinalIgnoreCase);
    public NetworkTopology() { InitializeComponent(); Loaded += async (_, _) => await RefreshAsync(); }

    private async Task RefreshAsync()
    {
        _sensors = await MainWindow.ApiClient.GetSensorsAsync();
        SensorCombo.ItemsSource = _sensors;
        GatewayCombo.ItemsSource = _sensors;
        if (SensorCombo.SelectedIndex < 0 && _sensors.Count > 0) SensorCombo.SelectedIndex = 0;
        if (GatewayCombo.SelectedIndex < 0 && _sensors.Count > 0) GatewayCombo.SelectedIndex = 0;
        var topology = await MainWindow.ApiClient.GetNetworkTopologyAsync();
        if (topology is null) { NetworkStatusText.Text = "API unavailable. Start the Smart-X backend and refresh."; return; }
        var nodes = topology.Value.GetProperty("nodes").EnumerateArray().Select(n => new NodeRow(
            n.GetProperty("id").GetString() ?? "", n.GetProperty("kind").GetString() ?? "",
            n.GetProperty("isOnline").GetBoolean() ? "Online" : "Down")).ToList();
        var edges = topology.Value.GetProperty("edges").EnumerateArray().Select(e => new EdgeRow(
            e.GetProperty("from").GetString() ?? "", e.GetProperty("to").GetString() ?? "",
            e.GetProperty("energyCost").GetDouble(), e.GetProperty("isActive").GetBoolean() ? "Active" : "Inactive")).ToList();
        _topologyNodes = nodes; _topologyEdges = edges;
        TopologyGrid.ItemsSource = nodes; EdgesGrid.ItemsSource = edges;
        DrawTopology(nodes, edges);
        NodeCountText.Text = nodes.Count.ToString();
        OnlineCountText.Text = nodes.Count(n => n.Status == "Online" || n.Kind == "Server").ToString();
        DownCountText.Text = nodes.Count(n => n.Status == "Down").ToString();
        NetworkStatusText.Text = "Topology loaded. Default topology is a demonstration scaffold; configure real ESP32 links and measured energy costs through PUT /api/network/topology.";
        await AnalyzeAsync();
    }
    private void DrawTopology(List<NodeRow> nodes, List<EdgeRow> edges)
    {
        NetworkCanvas.Children.Clear();
        if (nodes.Count == 0) return;
        var width = Math.Max(NetworkCanvas.ActualWidth, 640); const double height = 280, radius = 105, nodeSize = 54;
        var positions = new Dictionary<string, Point>();
        var ordered = nodes.OrderBy(n => n.Kind == "Server" ? 0 : n.Kind == "Gateway" ? 1 : 2).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            double angle = -Math.PI / 2 + 2 * Math.PI * i / Math.Max(1, ordered.Count);
            positions[ordered[i].Id] = new Point(width / 2 + radius * Math.Cos(angle), height / 2 + radius * Math.Sin(angle));
        }
        foreach (var edge in edges)
        {
            if (!positions.TryGetValue(edge.From, out var a) || !positions.TryGetValue(edge.To, out var b)) continue;
            var isMst = _mstEdges.Contains(EdgeKey(edge.From, edge.To));
            NetworkCanvas.Children.Add(new Line { X1=a.X, Y1=a.Y, X2=b.X, Y2=b.Y,
                Stroke=edge.Status != "Active" ? Brushes.IndianRed : isMst ? Brushes.DarkOrange : Brushes.SlateGray,
                StrokeThickness=isMst ? 4 : 2 });
            var mid = new TextBlock { Text=edge.EnergyCost.ToString("0.##"), FontSize=10, Foreground=Brushes.DimGray,
                Background=Brushes.White, Padding=new Thickness(2) };
            Canvas.SetLeft(mid, (a.X+b.X)/2); Canvas.SetTop(mid, (a.Y+b.Y)/2); NetworkCanvas.Children.Add(mid);
        }
        foreach (var node in ordered)
        {
            var point = positions[node.Id];
            var fill = node.Kind == "Server" ? Brushes.SteelBlue :
                node.Status == "Down" ? Brushes.IndianRed : node.Kind == "Gateway" ? Brushes.SeaGreen : Brushes.CornflowerBlue;
            var circle = new Ellipse { Width=nodeSize, Height=nodeSize, Fill=fill, Stroke=Brushes.White, StrokeThickness=2 };
            Canvas.SetLeft(circle, point.X-nodeSize/2); Canvas.SetTop(circle, point.Y-nodeSize/2); NetworkCanvas.Children.Add(circle);
            var label = new TextBlock { Text=node.Id, FontSize=10, FontWeight=FontWeights.SemiBold,
                Foreground=Brushes.White, Width=nodeSize+45, TextAlignment=TextAlignment.Center };
            Canvas.SetLeft(label, point.X-(nodeSize+45)/2); Canvas.SetTop(label, point.Y-7); NetworkCanvas.Children.Add(label);
        }
    }

    private async Task AnalyzeAsync()
    {
        var gateway = GatewayCombo.SelectedItem as SensorRecord;
        if (gateway is null) { DfsResultText.Text = "Register a sensor to select a gateway."; return; }
        var result = await MainWindow.ApiClient.GetNetworkAnalysisAsync(gateway.Id);
        if (result is null) { DfsResultText.Text = "Could not retrieve network analysis."; return; }
        var connected = Strings(result.Value, "connectedDevices");
        var down = Strings(result.Value, "downDevices");
        var unreachable = Strings(result.Value, "unreachableDevices");
        var order = Strings(result.Value, "dfsOrder");
        DfsResultText.Text = $"DFS start: {gateway.Id}\nTraversal order: {string.Join(" → ", order)}\nReachable devices: {string.Join(", ", connected)}\nDown devices in this component: {(down.Count == 0 ? "None detected" : string.Join(", ", down))}\nDevices with no path to central server: {(unreachable.Count == 0 ? "None" : string.Join(", ", unreachable))}";
        var mstEdges = result.Value.GetProperty("minimumSpanningTree").EnumerateArray().ToList();
        _mstEdges = mstEdges.Select(e => EdgeKey(e.GetProperty("from").GetString() ?? "", e.GetProperty("to").GetString() ?? ""))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        DrawTopology(_topologyNodes, _topologyEdges);
        var mst = mstEdges.Select(e =>
            $"{e.GetProperty("from").GetString()} ↔ {e.GetProperty("to").GetString()} (cost {e.GetProperty("energyCost").GetDouble():0.###})").ToList();
        var cost = result.Value.GetProperty("totalEnergyCost").GetDouble();
        var connectedMst = result.Value.GetProperty("mstConnected").GetBoolean();
        EnergyText.Text = cost.ToString("0.##");
        var routes = result.Value.GetProperty("routesToServer").EnumerateObject()
            .Select(pair => $"{pair.Name}: {(pair.Value.GetArrayLength() == 0 ? "No route" : string.Join(" → ", pair.Value.EnumerateArray().Select(x => x.GetString())))}")
            .ToList();
        MstResultText.Text = $"Selected minimum-cost links:\n{(mst.Count == 0 ? "No active links available." : string.Join("\n", mst))}\n\nRoutes along the MST to central server:\n{(routes.Count == 0 ? "No sensor routes." : string.Join("\n", routes))}\n\nTotal relative energy cost: {cost:0.###}\nBackbone connected: {(connectedMst ? "Yes" : "No — graph contains disconnected components")}";
    }
    private static string EdgeKey(string from, string to) =>
        string.Compare(from, to, StringComparison.OrdinalIgnoreCase) <= 0 ? $"{from}|{to}" : $"{to}|{from}";

    private static List<string> Strings(JsonElement root, string property) =>
        root.GetProperty(property).EnumerateArray().Select(x => x.GetString() ?? "").ToList();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void Analyze_Click(object sender, RoutedEventArgs e) => await AnalyzeAsync();
    private async void QueryHistory_Click(object sender, RoutedEventArgs e)
    {
        if (SensorCombo.SelectedItem is not SensorRecord sensor) { HistoryStatusText.Text = "Register/select a sensor first."; return; }
        var from = (FromDate.SelectedDate ?? DateTime.Today.AddDays(-7)).Date;
        var to = (ToDate.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
        if (from > to) { HistoryStatusText.Text = "The start date must not be after the end date."; return; }
        var rows = await MainWindow.ApiClient.GetTelemetryRangeAsync(sensor.Id, from, to);
        var ordered = rows.OrderBy(r => r.Timestamp).ToList();
        HistoryGrid.ItemsSource = ordered;
        DrawTelemetryChart(ordered);
        HistoryStatusText.Text = $"{rows.Count:N0} records returned for {sensor.Id} from {from:u} to {to:u}. Maximum query size: 10,000. The server uses an AVL-balanced timestamp tree; range work is proportional to tree height plus returned records.";
    }

    private void DrawTelemetryChart(List<TelemetryReading> readings)
    {
        TelemetryChart.Children.Clear();
        var width = Math.Max(TelemetryChart.ActualWidth, 640);
        const double height = 150, left = 42, right = 14, top = 12, bottom = 24;
        var axis = new System.Windows.Media.BrushConverter().ConvertFromString("#91A0B2") as Brush ?? Brushes.Gray;
        TelemetryChart.Children.Add(new Line { X1 = left, Y1 = top, X2 = left, Y2 = height - bottom, Stroke = axis });
        TelemetryChart.Children.Add(new Line { X1 = left, Y1 = height - bottom, X2 = width - right, Y2 = height - bottom, Stroke = axis });
        if (readings.Count == 0)
        {
            var empty = new TextBlock { Text = "No telemetry readings in this range", Foreground = Brushes.Gray };
            Canvas.SetLeft(empty, left + 12); Canvas.SetTop(empty, 58); TelemetryChart.Children.Add(empty);
            return;
        }
        var min = readings.Min(r => r.Value);
        var max = readings.Max(r => r.Value);
        var span = Math.Max(max - min, 0.000001);
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        var points = new PointCollection();
        for (var i = 0; i < readings.Count; i++)
        {
            var x = left + (readings.Count == 1 ? plotWidth / 2 : i * plotWidth / (readings.Count - 1));
            var y = top + (max - readings[i].Value) / span * plotHeight;
            points.Add(new Point(x, y));
        }
        if (points.Count > 1)
            TelemetryChart.Children.Add(new Polyline { Points = points, Stroke = Brushes.SteelBlue, StrokeThickness = 2.5, StrokeLineJoin = PenLineJoin.Round });
        foreach (var pair in readings.Select((reading, index) => (reading, index)).Where(x => x.reading.IsAnomalous || readings.Count == 1))
        {
            var point = points[pair.index];
            var marker = new Ellipse { Width = 8, Height = 8, Fill = pair.reading.IsAnomalous ? Brushes.IndianRed : Brushes.SteelBlue };
            Canvas.SetLeft(marker, point.X - 4); Canvas.SetTop(marker, point.Y - 4); TelemetryChart.Children.Add(marker);
        }
        var rangeLabel = new TextBlock { Text = $"{min:0.##} – {max:0.##} {readings[0].Units}", FontSize = 10, Foreground = Brushes.DimGray };
        Canvas.SetLeft(rangeLabel, 4); Canvas.SetTop(rangeLabel, 4); TelemetryChart.Children.Add(rangeLabel);
    }

    private sealed record NodeRow(string Id, string Kind, string Status);
    private sealed record EdgeRow(string From, string To, double EnergyCost, string Status);
}
