using StaticCode.Diagrams.Layout;

namespace StaticCode.Diagrams.Test;

public class LayeredLayoutTest
{
    private static readonly LayoutNode Box = new(60, 30);

    [Fact]
    public void PlacesTargetsBelowSourcesFromTopToBottom()
    {
        var result = Compute(4, new[] { (0, 1), (1, 2), (0, 3), (3, 2) }, LayoutDirection.TopToBottom);

        Assert.True(result.NodeCenters[1].Y > result.NodeCenters[0].Y);
        Assert.True(result.NodeCenters[2].Y > result.NodeCenters[1].Y);
        Assert.Equal(result.NodeCenters[1].Y, result.NodeCenters[3].Y);
    }

    [Fact]
    public void PlacesTargetsRightOfSourcesFromLeftToRight()
    {
        var result = Compute(3, new[] { (0, 1), (1, 2) }, LayoutDirection.LeftToRight);

        Assert.True(result.NodeCenters[1].X > result.NodeCenters[0].X);
        Assert.True(result.NodeCenters[2].X > result.NodeCenters[1].X);
        Assert.Equal(result.NodeCenters[0].Y, result.NodeCenters[2].Y, 1);
    }

    [Fact]
    public void ReversesDirectionsForBottomToTopAndRightToLeft()
    {
        var bottomToTop = Compute(2, new[] { (0, 1) }, LayoutDirection.BottomToTop);
        var rightToLeft = Compute(2, new[] { (0, 1) }, LayoutDirection.RightToLeft);

        Assert.True(bottomToTop.NodeCenters[1].Y < bottomToTop.NodeCenters[0].Y);
        Assert.True(rightToLeft.NodeCenters[1].X < rightToLeft.NodeCenters[0].X);
    }

    [Fact]
    public void KeepsNodesOfARankApart()
    {
        var result = Compute(5, new[] { (0, 1), (0, 2), (0, 3), (0, 4) }, LayoutDirection.TopToBottom);

        var children = result.NodeCenters.Skip(1).OrderBy(point => point.X).ToList();
        for (var index = 1; index < children.Count; index++)
            Assert.True(children[index].X - children[index - 1].X >= Box.Width);
    }

    [Fact]
    public void CentersAParentAboveItsChildren()
    {
        var result = Compute(3, new[] { (0, 1), (0, 2) }, LayoutDirection.TopToBottom);

        Assert.Equal((result.NodeCenters[1].X + result.NodeCenters[2].X) / 2, result.NodeCenters[0].X, 1);
    }

    [Fact]
    public void RoutesCyclesFromSourceToTarget()
    {
        var result = Compute(3, new[] { (0, 1), (1, 2), (2, 0) }, LayoutDirection.TopToBottom);

        var back = result.Edges[2].Points;
        Assert.True(back.Count >= 3);
        Assert.Equal(result.NodeCenters[2].Y - Box.Height / 2, back[0].Y, 3);
        Assert.Equal(result.NodeCenters[0].Y + Box.Height / 2, back[^1].Y, 3);
    }

    [Fact]
    public void AvoidsCrossingsWhenPossible()
    {
        // Declaring the children in the "wrong" order would cross the edges without reordering.
        var result = Compute(4, new[] { (0, 3), (1, 2), (0, 2) }, LayoutDirection.TopToBottom);

        var sourcesInOrder = result.NodeCenters[0].X < result.NodeCenters[1].X;
        var targetsInOrder = result.NodeCenters[3].X < result.NodeCenters[2].X;
        Assert.Equal(sourcesInOrder, targetsInOrder);
    }

    [Fact]
    public void PlacesEdgeLabelsBetweenTheirNodes()
    {
        var nodes = new[] { Box, Box };
        var edges = new[] { new LayoutEdge(0, 1, LabelWidth: 40, LabelHeight: 20) };

        var result = new LayeredLayout().Compute(nodes, edges, LayoutDirection.TopToBottom);

        Assert.NotNull(result.Edges[0].LabelCenter);
        var label = result.Edges[0].LabelCenter!.Value;
        Assert.True(label.Y - 10 > result.NodeCenters[0].Y + Box.Height / 2);
        Assert.True(label.Y + 10 < result.NodeCenters[1].Y - Box.Height / 2);
    }

    [Fact]
    public void ReturnsEmptyRoutesForSelfLoops()
    {
        var result = Compute(1, new[] { (0, 0) }, LayoutDirection.TopToBottom);

        Assert.Empty(result.Edges[0].Points);
    }

    [Fact]
    public void LongerLinksSpanMoreRanks()
    {
        var nodes = new[] { Box, Box, Box };
        var edges = new[] { new LayoutEdge(0, 1), new LayoutEdge(0, 2, MinLength: 2) };

        var result = new LayeredLayout().Compute(nodes, edges, LayoutDirection.TopToBottom);

        Assert.True(result.NodeCenters[2].Y > result.NodeCenters[1].Y);
    }

    private static LayoutResult Compute(int nodeCount, (int Source, int Target)[] edges, LayoutDirection direction) =>
        new LayeredLayout().Compute(
            Enumerable.Repeat(Box, nodeCount).ToArray(),
            edges.Select(edge => new LayoutEdge(edge.Source, edge.Target)).ToArray(),
            direction);
}
