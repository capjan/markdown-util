namespace StaticCode.Diagrams.Layout;

internal readonly record struct Point(double X, double Y)
{
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point point, double factor) => new(point.X * factor, point.Y * factor);

    public double Dot(Point other) => X * other.X + Y * other.Y;
    public double Length => Math.Sqrt(X * X + Y * Y);
    public Point Normalized => Length == 0 ? this : this * (1 / Length);
    public Point Perpendicular => new(-Y, X);
}
