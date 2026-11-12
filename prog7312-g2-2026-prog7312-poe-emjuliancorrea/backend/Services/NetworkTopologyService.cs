using Smart_X.backend.Models;

namespace Smart_X.backend.Services;

// AVL tree algorithm attribution: Adelson-Velsky & Landis (1962), “An algorithm for the
// organization of information”; see also Sedgewick & Wayne, Algorithms (4e), §3.2.
public sealed class TelemetryTimestampBst
{
    private sealed class Node(DateTime key, TelemetryReading reading)
    {
        public DateTime Key = key;
        public List<TelemetryReading> Values = new() { reading };
        public Node? Left, Right;
        public int Height = 1;
    }

    private Node? _root;
    public int Count { get; private set; }
    public int Height => HeightOf(_root);

    public void Insert(TelemetryReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        _root = InsertRecursive(_root, reading.Timestamp.ToUniversalTime(), reading);
        Count++;
    }

    private static Node InsertRecursive(Node? node, DateTime key, TelemetryReading reading)
    {
        if (node is null) return new Node(key, reading);
        if (key < node.Key) node.Left = InsertRecursive(node.Left, key, reading);
        else if (key > node.Key) node.Right = InsertRecursive(node.Right, key, reading);
        else { node.Values.Add(reading); return node; }

        UpdateHeight(node);
        var balance = Balance(node);
        if (balance > 1)
        {
            if (key > node.Left!.Key) node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }
        if (balance < -1)
        {
            if (key < node.Right!.Key) node.Right = RotateRight(node.Right);
            return RotateLeft(node);
        }
        return node;
    }

    public List<TelemetryReading> Range(DateTime from, DateTime to, int limit = 5000)
    {
        if (from > to) throw new ArgumentException("Range start must not be after range end.");
        var result = new List<TelemetryReading>(Math.Min(Math.Max(limit, 0), 1024));
        RangeRecursive(_root, from.ToUniversalTime(), to.ToUniversalTime(), result, Math.Max(0, limit));
        return result;
    }

    private static void RangeRecursive(Node? node, DateTime from, DateTime to,
        List<TelemetryReading> output, int limit)
    {
        if (node is null || output.Count >= limit) return;
        if (from < node.Key) RangeRecursive(node.Left, from, to, output, limit);
        if (node.Key >= from && node.Key <= to)
            foreach (var reading in node.Values)
            {
                if (output.Count >= limit) break;
                output.Add(reading);
            }
        if (to > node.Key) RangeRecursive(node.Right, from, to, output, limit);
    }

    private static int HeightOf(Node? node) => node?.Height ?? 0;
    private static int Balance(Node node) => HeightOf(node.Left) - HeightOf(node.Right);
    private static void UpdateHeight(Node node) => node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    private static Node RotateRight(Node root)
    {
        var pivot = root.Left!;
        root.Left = pivot.Right;
        pivot.Right = root;
        UpdateHeight(root);
        UpdateHeight(pivot);
        return pivot;
    }
    private static Node RotateLeft(Node root)
    {
        var pivot = root.Right!;
        root.Right = pivot.Left;
        pivot.Left = root;
        UpdateHeight(root);
        UpdateHeight(pivot);
        return pivot;
    }
}

public sealed class MeshNode
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "Sensor";
    public bool IsOnline { get; set; } = true;
}
public sealed class MeshEdge
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public double EnergyCost { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
public sealed class MeshTopology
{
    public List<MeshNode> Nodes { get; set; } = new();
    public List<MeshEdge> Edges { get; set; } = new();
}

public sealed class NetworkTopologyService
{
    private readonly Dictionary<string, TelemetryTimestampBst> _history = new(StringComparer.OrdinalIgnoreCase);
    private MeshTopology _topology = new();
    private bool _configured;
    private readonly object _gate = new();

    public void Record(TelemetryReading reading)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(reading.SensorId, out var tree))
                _history[reading.SensorId] = tree = new TelemetryTimestampBst();
            tree.Insert(reading);
        }
    }

    public List<TelemetryReading> Query(string sensorId, DateTime from, DateTime to, int limit)
    {
        lock (_gate)
            return _history.TryGetValue(sensorId, out var tree)
                ? tree.Range(from, to, Math.Clamp(limit, 1, 10000))
                : new List<TelemetryReading>();
    }

    public void Configure(MeshTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (topology.Nodes.Any(n => string.IsNullOrWhiteSpace(n.Id)))
            throw new ArgumentException("Every network node needs a non-empty ID.");
        if (topology.Nodes.Select(n => n.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != topology.Nodes.Count)
            throw new ArgumentException("Node IDs must be unique.");
        var ids = topology.Nodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (topology.Edges.Any(e => !ids.Contains(e.From) || !ids.Contains(e.To) ||
            string.Equals(e.From, e.To, StringComparison.OrdinalIgnoreCase) ||
            !double.IsFinite(e.EnergyCost) || e.EnergyCost < 0))
            throw new ArgumentException("Every edge must connect two distinct known nodes and have a finite, non-negative energy cost.");
        lock (_gate) { _topology = topology; _configured = true; }
    }

    public MeshTopology GetTopology(IEnumerable<SensorRecord> sensors)
    {
        lock (_gate)
        {
            var sensorList = sensors.ToList();
            if (!_configured)
            {
                _topology = new MeshTopology();
                if (sensorList.Count == 0) return new MeshTopology();
                var ordered = sensorList.OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase).ToList();
                var gateway = ordered[0].Id;
                _topology.Nodes.Add(new MeshNode { Id = "CENTRAL-SERVER", Kind = "Server" });
                _topology.Nodes.Add(new MeshNode { Id = gateway, Kind = "Gateway" });
                foreach (var sensor in ordered.Skip(1)) _topology.Nodes.Add(new MeshNode { Id = sensor.Id, Kind = "Sensor" });
                _topology.Edges.Add(new MeshEdge { From = gateway, To = "CENTRAL-SERVER", EnergyCost = 1 });
                for (var i = 1; i < ordered.Count; i++)
                    _topology.Edges.Add(new MeshEdge { From = ordered[i].Id, To = gateway, EnergyCost = 1 + i / 10.0 });
            }

            var known = sensorList.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var node in _topology.Nodes.Where(n => n.Kind != "Server"))
                if (known.TryGetValue(node.Id, out var sensor))
                    node.IsOnline = sensor.LastStatus is not null &&
                        !sensor.LastStatus.Contains("offline", StringComparison.OrdinalIgnoreCase) &&
                        !sensor.LastStatus.Contains("disabled", StringComparison.OrdinalIgnoreCase);

            return new MeshTopology
            {
                Nodes = _topology.Nodes.Select(n => new MeshNode { Id = n.Id, Kind = n.Kind, IsOnline = n.IsOnline }).ToList(),
                Edges = _topology.Edges.Select(e => new MeshEdge { From = e.From, To = e.To, EnergyCost = e.EnergyCost, IsActive = e.IsActive }).ToList()
            };
        }
    }

    public object Analyze(IEnumerable<SensorRecord> sensors, string gatewayId)
    {
        var graph = GetTopology(sensors);
        var nodeMap = graph.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        if (!nodeMap.ContainsKey(gatewayId)) throw new ArgumentException($"Gateway '{gatewayId}' is not present in the topology.");

        var adjacency = graph.Nodes.ToDictionary(n => n.Id, _ => new List<MeshEdge>(), StringComparer.OrdinalIgnoreCase);
        foreach (var edge in graph.Edges.Where(e => e.IsActive))
        {
            adjacency[edge.From].Add(edge);
            adjacency[edge.To].Add(edge);
        }

        // Iterative DFS algorithm reference: Cormen et al., Introduction to Algorithms (4e), graph traversal chapter;
        // also Sedgewick & Wayne, Algorithms (4e), §4.1. This is an independent C# implementation.
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dfsOrder = new List<string>();
        var dfsStack = new Stack<string>();
        dfsStack.Push(gatewayId);
        while (dfsStack.Count > 0)
        {
            var current = dfsStack.Pop();
            if (!visited.Add(current)) continue;
            dfsOrder.Add(current);
            foreach (var edge in adjacency[current].AsEnumerable().Reverse())
            {
                var next = Other(edge, current);
                if (!visited.Contains(next)) dfsStack.Push(next);
            }
        }
        var connected = dfsOrder.Where(id => nodeMap[id].Kind != "Server").ToList();
        var down = connected.Where(id => !nodeMap[id].IsOnline).ToList();

        // Prim's algorithm reference: Prim (1957), “Shortest Connection Networks and Some Generalizations”;
        // implementation approach also described by Sedgewick & Wayne, Algorithms (4e), §4.3.
        // Heap invariant follows the binary-heap model in Williams (1964), CACM 7(6), Algorithm 232.
        var onlineIds = graph.Nodes.Where(n => n.IsOnline || n.Kind == "Server")
            .Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mst = new List<MeshEdge>();
        var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalCost = 0d;
        foreach (var start in graph.Nodes.Where(n => onlineIds.Contains(n.Id)).Select(n => n.Id))
        {
            if (!inTree.Add(start)) continue;
            var heap = new EdgeMinHeap();
            AddFrontier(start);
            while (heap.Count > 0)
            {
                var candidate = heap.Pop();
                var fromInside = inTree.Contains(candidate.From);
                var toInside = inTree.Contains(candidate.To);
                if (fromInside == toInside) continue;
                var next = fromInside ? candidate.To : candidate.From;
                mst.Add(candidate);
                totalCost += candidate.EnergyCost;
                inTree.Add(next);
                AddFrontier(next);
            }

            void AddFrontier(string id)
            {
                foreach (var edge in adjacency[id])
                    if (onlineIds.Contains(edge.From) && onlineIds.Contains(edge.To) &&
                        inTree.Contains(edge.From) != inTree.Contains(edge.To))
                        heap.Push(edge);
            }
        }

        var serverReachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (nodeMap.ContainsKey("CENTRAL-SERVER"))
        {
            var pending = new Stack<string>();
            pending.Push("CENTRAL-SERVER");
            serverReachable.Add("CENTRAL-SERVER");
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var edge in adjacency[current])
                {
                    var next = Other(edge, current);
                    if (serverReachable.Add(next)) pending.Push(next);
                }
            }
        }
        var unreachable = graph.Nodes.Where(n => n.Kind != "Server" && !serverReachable.Contains(n.Id))
            .Select(n => n.Id).ToList();

        var treeAdjacency = graph.Nodes.ToDictionary(n => n.Id, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var edge in mst) { treeAdjacency[edge.From].Add(edge.To); treeAdjacency[edge.To].Add(edge.From); }
        var routes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sensor in graph.Nodes.Where(n => n.Kind != "Server"))
            routes[sensor.Id] = FindTreeRoute(sensor.Id, "CENTRAL-SERVER", treeAdjacency);

        var onlineCount = onlineIds.Count;
        return new
        {
            topology = graph, dfsOrder, connectedDevices = connected, downDevices = down,
            unreachableDevices = unreachable, minimumSpanningTree = mst, routesToServer = routes,
            totalEnergyCost = totalCost,
            mstConnected = mst.Count == Math.Max(0, onlineCount - 1),
            algorithm = "Prim with binary min-heap; O((V + E) log E) per connected component"
        };
    }

    private static string Other(MeshEdge edge, string id) =>
        string.Equals(edge.From, id, StringComparison.OrdinalIgnoreCase) ? edge.To : edge.From;

    private static List<string> FindTreeRoute(string start, string target, Dictionary<string, List<string>> tree)
    {
        if (!tree.ContainsKey(start) || !tree.ContainsKey(target)) return new();
        var queue = new Queue<string>();
        var previous = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [start] = null };
        queue.Enqueue(start);
        while (queue.Count > 0 && !previous.ContainsKey(target))
        {
            var current = queue.Dequeue();
            foreach (var next in tree[current])
                if (!previous.ContainsKey(next)) { previous[next] = current; queue.Enqueue(next); }
        }
        if (!previous.ContainsKey(target)) return new();
        var route = new List<string>();
        for (string? cursor = target; cursor is not null; cursor = previous[cursor]) route.Add(cursor);
        route.Reverse();
        return route;
    }

    private sealed class EdgeMinHeap
    {
        private readonly List<MeshEdge> _items = new();
        public int Count => _items.Count;
        public void Push(MeshEdge edge)
        {
            _items.Add(edge);
            var child = _items.Count - 1;
            while (child > 0)
            {
                var parent = (child - 1) / 2;
                if (Compare(_items[parent], _items[child]) <= 0) break;
                (_items[parent], _items[child]) = (_items[child], _items[parent]);
                child = parent;
            }
        }
        public MeshEdge Pop()
        {
            if (_items.Count == 0) throw new InvalidOperationException("Heap is empty.");
            var minimum = _items[0];
            var last = _items[^1];
            _items.RemoveAt(_items.Count - 1);
            if (_items.Count == 0) return minimum;
            _items[0] = last;
            var parent = 0;
            while (true)
            {
                var left = parent * 2 + 1;
                var right = left + 1;
                if (left >= _items.Count) break;
                var smallest = right < _items.Count && Compare(_items[right], _items[left]) < 0 ? right : left;
                if (Compare(_items[parent], _items[smallest]) <= 0) break;
                (_items[parent], _items[smallest]) = (_items[smallest], _items[parent]);
                parent = smallest;
            }
            return minimum;
        }
        private static int Compare(MeshEdge a, MeshEdge b)
        {
            var cost = a.EnergyCost.CompareTo(b.EnergyCost);
            if (cost != 0) return cost;
            var from = string.Compare(a.From, b.From, StringComparison.OrdinalIgnoreCase);
            return from != 0 ? from : string.Compare(a.To, b.To, StringComparison.OrdinalIgnoreCase);
        }
    }
}
