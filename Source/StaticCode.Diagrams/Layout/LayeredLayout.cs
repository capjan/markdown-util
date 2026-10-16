namespace StaticCode.Diagrams.Layout;

internal enum LayoutDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}

/// <param name="Margin">Extra free space on both sides along the layer, for example for self-loops.</param>
internal sealed record LayoutNode(double Width, double Height, double Margin = 0);

/// <param name="MinLength">Minimum number of ranks between source and target.</param>
internal sealed record LayoutEdge(int Source, int Target, int MinLength = 1, double LabelWidth = 0, double LabelHeight = 0);

/// <param name="Points">
/// Route from source to target. The first and last points lie on the bounding boxes of the end nodes;
/// the points in between are where the edge passes between ranks. Empty for self-loops.
/// </param>
internal sealed record EdgeRoute(IReadOnlyList<Point> Points, Point? LabelCenter);

internal sealed record LayoutResult(IReadOnlyList<Point> NodeCenters, IReadOnlyList<EdgeRoute> Edges, double Width, double Height);

/// <summary>
/// Layered graph drawing after Sugiyama et al.: break cycles, assign ranks, insert dummy vertices for long edges
/// and labels, reduce crossings with barycenter sweeps and transpositions, then place vertices by minimizing the
/// weighted squared length of all segments subject to the separation constraints.
/// </summary>
internal sealed class LayeredLayout
{
    private const int OrderingIterations = 24;
    private const int TransposePasses = 8;
    private const int PositioningIterations = 400;

    public double NodeSeparation { get; init; } = 36;
    public double EdgeSeparation { get; init; } = 14;
    public double RankSeparation { get; init; } = 52;
    public double PortSpacing { get; init; } = 14;

    public LayoutResult Compute(IReadOnlyList<LayoutNode> nodes, IReadOnlyList<LayoutEdge> edges, LayoutDirection direction)
    {
        var horizontal = direction is LayoutDirection.LeftToRight or LayoutDirection.RightToLeft;
        var vertices = nodes.Select(node => new Vertex
        {
            Breadth = horizontal ? node.Height : node.Width,
            Depth = horizontal ? node.Width : node.Height,
            Margin = node.Margin
        }).ToList();
        if (vertices.Count == 0)
            return new LayoutResult(Array.Empty<Point>(), edges.Select(_ => new EdgeRoute(Array.Empty<Point>(), null)).ToArray(), 0, 0);

        // Every edge spans at least two ranks so that labels and bends get a rank of their own.
        var reversed = FindBackEdges(nodes.Count, edges);
        var ranked = edges
            .Select((edge, index) => (Edge: edge, Index: index))
            .Where(item => item.Edge.Source != item.Edge.Target)
            .Select(item => new RankedEdge(
                item.Index,
                reversed[item.Index] ? item.Edge.Target : item.Edge.Source,
                reversed[item.Index] ? item.Edge.Source : item.Edge.Target,
                2 * Math.Max(1, item.Edge.MinLength)))
            .ToList();

        AssignRanks(vertices, ranked);

        var chains = new List<Vertex>?[edges.Count];
        var labels = new Vertex?[edges.Count];
        var allVertices = new List<Vertex>(vertices);
        foreach (var edge in ranked)
        {
            var input = edges[edge.Index];
            var from = vertices[edge.Upper];
            var to = vertices[edge.Lower];
            var labelRank = from.Rank + (to.Rank - from.Rank) / 2;
            var chain = new List<Vertex> { from };
            var previous = from;
            for (var rank = from.Rank + 1; rank < to.Rank; rank++)
            {
                var dummy = new Vertex { IsDummy = true, Rank = rank };
                if (rank == labelRank && input.LabelWidth > 0)
                {
                    dummy.Breadth = horizontal ? input.LabelHeight : input.LabelWidth;
                    dummy.Depth = horizontal ? input.LabelWidth : input.LabelHeight;
                    labels[edge.Index] = dummy;
                }

                Connect(previous, dummy);
                chain.Add(dummy);
                allVertices.Add(dummy);
                previous = dummy;
            }

            Connect(previous, to);
            chain.Add(to);
            chains[edge.Index] = chain;
        }

        var layers = BuildLayers(vertices, allVertices.Max(vertex => vertex.Rank) + 1);
        MinimizeCrossings(layers);
        AssignCoordinates(layers);
        foreach (var vertex in vertices) AssignPorts(vertex);

        var layoutWidth = allVertices.Max(vertex => vertex.X + vertex.HalfBreadth);
        var layoutHeight = allVertices.Max(vertex => vertex.Y + vertex.Depth / 2);
        Point Map(double x, double y) => direction switch
        {
            LayoutDirection.BottomToTop => new Point(x, layoutHeight - y),
            LayoutDirection.LeftToRight => new Point(y, x),
            LayoutDirection.RightToLeft => new Point(layoutHeight - y, x),
            _ => new Point(x, y)
        };

        var routes = new EdgeRoute[edges.Count];
        for (var index = 0; index < edges.Count; index++)
        {
            var chain = chains[index];
            if (chain == null)
            {
                routes[index] = new EdgeRoute(Array.Empty<Point>(), null);
                continue;
            }

            var first = chain[0];
            var last = chain[^1];
            var points = new List<Point> { Map(first.X + chain[1].UpperPort, first.Y + first.Depth / 2) };
            points.AddRange(chain.Skip(1).Take(chain.Count - 2).Select(dummy => Map(dummy.X, dummy.Y)));
            points.Add(Map(last.X + chain[^2].LowerPort, last.Y - last.Depth / 2));
            if (reversed[index]) points.Reverse();

            var label = labels[index];
            routes[index] = new EdgeRoute(points, label == null ? null : Map(label.X, label.Y));
        }

        return new LayoutResult(
            vertices.Select(vertex => Map(vertex.X, vertex.Y)).ToArray(),
            routes,
            horizontal ? layoutHeight : layoutWidth,
            horizontal ? layoutWidth : layoutHeight);
    }

    private static bool[] FindBackEdges(int nodeCount, IReadOnlyList<LayoutEdge> edges)
    {
        var reversed = new bool[edges.Count];
        var state = new byte[nodeCount];
        var outgoing = Enumerable.Range(0, nodeCount).Select(_ => new List<int>()).ToArray();
        for (var index = 0; index < edges.Count; index++)
            if (edges[index].Source != edges[index].Target)
                outgoing[edges[index].Source].Add(index);

        void Visit(int node)
        {
            state[node] = 1;
            foreach (var index in outgoing[node])
            {
                var target = edges[index].Target;
                if (state[target] == 1) reversed[index] = true;
                else if (state[target] == 0) Visit(target);
            }

            state[node] = 2;
        }

        for (var node = 0; node < nodeCount; node++)
            if (state[node] == 0)
                Visit(node);
        return reversed;
    }

    private static void AssignRanks(List<Vertex> vertices, List<RankedEdge> edges)
    {
        var count = vertices.Count;
        var outgoing = Enumerable.Range(0, count).Select(_ => new List<RankedEdge>()).ToArray();
        var incoming = new int[count];
        foreach (var edge in edges)
        {
            outgoing[edge.Upper].Add(edge);
            incoming[edge.Lower]++;
        }

        var remaining = (int[])incoming.Clone();
        var queue = new Queue<int>(Enumerable.Range(0, count).Where(node => remaining[node] == 0));
        var topological = new List<int>(count);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            topological.Add(node);
            foreach (var edge in outgoing[node])
                if (--remaining[edge.Lower] == 0)
                    queue.Enqueue(edge.Lower);
        }

        var rank = new int[count];
        foreach (var node in topological)
        foreach (var edge in outgoing[node])
            rank[edge.Lower] = Math.Max(rank[edge.Lower], rank[node] + edge.MinLength);

        // Longest-path ranking pushes everything up; move nodes down when that shortens more edges than it stretches.
        for (var index = topological.Count - 1; index >= 0; index--)
        {
            var node = topological[index];
            if (outgoing[node].Count > 0 && outgoing[node].Count > incoming[node])
                rank[node] = outgoing[node].Min(edge => rank[edge.Lower] - edge.MinLength);
        }

        for (var node = 0; node < count; node++) vertices[node].Rank = rank[node];
    }

    private static void Connect(Vertex upper, Vertex lower)
    {
        upper.Lower.Add(lower);
        lower.Upper.Add(upper);
    }

    private static List<List<Vertex>> BuildLayers(List<Vertex> vertices, int layerCount)
    {
        var layers = Enumerable.Range(0, layerCount).Select(_ => new List<Vertex>()).ToList();
        var visited = new HashSet<Vertex>();

        void Visit(Vertex vertex)
        {
            if (!visited.Add(vertex)) return;
            layers[vertex.Rank].Add(vertex);
            foreach (var lower in vertex.Lower) Visit(lower);
        }

        foreach (var vertex in vertices.OrderBy(vertex => vertex.Rank)) Visit(vertex);
        foreach (var layer in layers) UpdateOrder(layer);
        return layers;
    }

    private static void UpdateOrder(List<Vertex> layer)
    {
        for (var index = 0; index < layer.Count; index++) layer[index].Order = index;
    }

    private static void MinimizeCrossings(List<List<Vertex>> layers)
    {
        var best = layers.Select(layer => layer.ToArray()).ToList();
        var bestCrossings = CountCrossings(layers);
        for (var iteration = 0; iteration < OrderingIterations && bestCrossings > 0; iteration++)
        {
            if (iteration % 2 == 0)
                for (var rank = 1; rank < layers.Count; rank++)
                    SortByBarycenter(layers[rank], vertex => vertex.Upper);
            else
                for (var rank = layers.Count - 2; rank >= 0; rank--)
                    SortByBarycenter(layers[rank], vertex => vertex.Lower);

            Transpose(layers);
            var crossings = CountCrossings(layers);
            if (crossings >= bestCrossings) continue;
            bestCrossings = crossings;
            best = layers.Select(layer => layer.ToArray()).ToList();
        }

        for (var rank = 0; rank < layers.Count; rank++)
        {
            layers[rank].Clear();
            layers[rank].AddRange(best[rank]);
            UpdateOrder(layers[rank]);
        }
    }

    /// <summary>Sorts vertices by the mean position of their neighbors; vertices without neighbors keep their slot.</summary>
    private static void SortByBarycenter(List<Vertex> layer, Func<Vertex, List<Vertex>> neighbors)
    {
        var movable = layer
            .Where(vertex => neighbors(vertex).Count > 0)
            .OrderBy(vertex => neighbors(vertex).Average(neighbor => neighbor.Order))
            .ThenBy(vertex => vertex.Order)
            .ToList();
        var next = 0;
        for (var index = 0; index < layer.Count; index++)
            if (neighbors(layer[index]).Count > 0)
                layer[index] = movable[next++];
        UpdateOrder(layer);
    }

    private static void Transpose(List<List<Vertex>> layers)
    {
        for (var pass = 0; pass < TransposePasses; pass++)
        {
            var improved = false;
            foreach (var layer in layers)
            {
                for (var index = 0; index + 1 < layer.Count; index++)
                {
                    var left = layer[index];
                    var right = layer[index + 1];
                    if (PairCrossings(right, left) >= PairCrossings(left, right)) continue;
                    layer[index] = right;
                    layer[index + 1] = left;
                    right.Order = index;
                    left.Order = index + 1;
                    improved = true;
                }
            }

            if (!improved) return;
        }
    }

    private static int PairCrossings(Vertex left, Vertex right)
    {
        var crossings = 0;
        foreach (var a in left.Upper)
        foreach (var b in right.Upper)
            if (a.Order > b.Order) crossings++;
        foreach (var a in left.Lower)
        foreach (var b in right.Lower)
            if (a.Order > b.Order) crossings++;
        return crossings;
    }

    private static int CountCrossings(List<List<Vertex>> layers)
    {
        var crossings = 0;
        foreach (var layer in layers)
        {
            var segments = layer.SelectMany(upper => upper.Lower.Select(lower => (Upper: upper.Order, Lower: lower.Order))).ToList();
            for (var i = 0; i < segments.Count; i++)
            for (var j = i + 1; j < segments.Count; j++)
                if ((segments[i].Upper - segments[j].Upper) * (segments[i].Lower - segments[j].Lower) < 0)
                    crossings++;
        }

        return crossings;
    }

    private void AssignCoordinates(List<List<Vertex>> layers)
    {
        foreach (var layer in layers)
            for (var index = 0; index < layer.Count; index++)
                layer[index].X = index == 0 ? layer[index].HalfBreadth : layer[index - 1].X + Gap(layer[index - 1], layer[index]);

        for (var iteration = 0; iteration < PositioningIterations; iteration++)
        {
            var moved = 0.0;
            if (iteration % 2 == 0)
                foreach (var layer in layers) moved = Math.Max(moved, PlaceLayer(layer));
            else
                for (var rank = layers.Count - 1; rank >= 0; rank--) moved = Math.Max(moved, PlaceLayer(layers[rank]));
            if (moved < 0.001) break;
        }

        var all = layers.SelectMany(layer => layer).ToList();
        var minX = all.Min(vertex => vertex.X - vertex.HalfBreadth);
        foreach (var vertex in all) vertex.X -= minX;

        double? previousCenter = null;
        var previousDepth = 0.0;
        foreach (var layer in layers.Where(layer => layer.Count > 0))
        {
            var depth = layer.Max(vertex => vertex.Depth);
            var center = previousCenter == null
                ? depth / 2
                : previousCenter.Value + previousDepth / 2 + RankSeparation / 2 + depth / 2;
            foreach (var vertex in layer) vertex.Y = center;
            previousCenter = center;
            previousDepth = depth;
        }
    }

    /// <summary>
    /// Moves every vertex of a layer as close as possible to the weighted mean of its neighbors while keeping the
    /// order and minimum gaps. This is a weighted isotonic regression, solved exactly with pool-adjacent-violators.
    /// </summary>
    private double PlaceLayer(List<Vertex> layer)
    {
        var count = layer.Count;
        if (count == 0) return 0;

        var values = new double[count];
        var weights = new double[count];
        var offsets = new double[count];
        for (var index = 0; index < count; index++)
        {
            var vertex = layer[index];
            double sum = 0, weight = 0;
            foreach (var neighbor in vertex.Upper.Concat(vertex.Lower))
            {
                var segmentWeight = vertex.IsDummy && neighbor.IsDummy ? 8 : vertex.IsDummy || neighbor.IsDummy ? 2 : 1;
                sum += segmentWeight * neighbor.X;
                weight += segmentWeight;
            }

            offsets[index] = index == 0 ? 0 : offsets[index - 1] + Gap(layer[index - 1], vertex);
            values[index] = (weight > 0 ? sum / weight : vertex.X) - offsets[index];
            weights[index] = weight > 0 ? weight : 1e-3;
        }

        var fitted = FitNonDecreasing(values, weights);
        var moved = 0.0;
        for (var index = 0; index < count; index++)
        {
            var x = fitted[index] + offsets[index];
            moved = Math.Max(moved, Math.Abs(x - layer[index].X));
            layer[index].X = x;
        }

        return moved;
    }

    private static double[] FitNonDecreasing(double[] values, double[] weights)
    {
        var blocks = new List<(double Value, double Weight, int Count)>();
        for (var index = 0; index < values.Length; index++)
        {
            var block = (Value: values[index], Weight: weights[index], Count: 1);
            while (blocks.Count > 0 && blocks[^1].Value > block.Value)
            {
                var last = blocks[^1];
                blocks.RemoveAt(blocks.Count - 1);
                var weight = last.Weight + block.Weight;
                block = ((last.Value * last.Weight + block.Value * block.Weight) / weight, weight, last.Count + block.Count);
            }

            blocks.Add(block);
        }

        var result = new double[values.Length];
        var position = 0;
        foreach (var block in blocks)
            for (var index = 0; index < block.Count; index++)
                result[position++] = block.Value;
        return result;
    }

    private double Gap(Vertex left, Vertex right)
    {
        var separation = left.IsDummy && right.IsDummy ? EdgeSeparation
            : left.IsDummy || right.IsDummy ? (NodeSeparation + EdgeSeparation) / 2
            : NodeSeparation;
        return left.HalfBreadth + right.HalfBreadth + separation;
    }

    /// <summary>Spreads the edge ends on each side of a node so that arrowheads do not overlap.</summary>
    private void AssignPorts(Vertex vertex)
    {
        var lower = vertex.Lower.OrderBy(neighbor => neighbor.X).ToList();
        var lowerOffsets = PortOffsets(vertex, lower.Count);
        for (var index = 0; index < lower.Count; index++) lower[index].UpperPort = lowerOffsets[index];

        var upper = vertex.Upper.OrderBy(neighbor => neighbor.X).ToList();
        var upperOffsets = PortOffsets(vertex, upper.Count);
        for (var index = 0; index < upper.Count; index++) upper[index].LowerPort = upperOffsets[index];
    }

    private double[] PortOffsets(Vertex vertex, int count)
    {
        var spacing = count > 1 ? Math.Min(PortSpacing, vertex.Breadth * 0.6 / (count - 1)) : 0;
        return Enumerable.Range(0, count).Select(index => (index - (count - 1) / 2.0) * spacing).ToArray();
    }

    private sealed record RankedEdge(int Index, int Upper, int Lower, int MinLength);

    private sealed class Vertex
    {
        /// <summary>Extent along the layer.</summary>
        public double Breadth;

        /// <summary>Extent across layers.</summary>
        public double Depth;

        public double Margin;
        public bool IsDummy;
        public int Rank;
        public int Order;
        public double X;
        public double Y;

        /// <summary>For dummies: offset of the edge end on the real vertex above.</summary>
        public double UpperPort;

        /// <summary>For dummies: offset of the edge end on the real vertex below.</summary>
        public double LowerPort;

        public readonly List<Vertex> Upper = new();
        public readonly List<Vertex> Lower = new();

        public double HalfBreadth => Breadth / 2 + Margin;
    }
}
