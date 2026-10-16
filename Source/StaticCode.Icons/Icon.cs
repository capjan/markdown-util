namespace StaticCode.Icons;

/// <summary>
/// An icon with its shortcode name (without colons) and its SVG content.
/// </summary>
public sealed record Icon(string Shortcode, string Body, int Width, int Height)
{
    /// <summary>
    /// Returns the icon as inline SVG. The SVG has no size and uses <c>currentColor</c>,
    /// so CSS sizes it relative to the text and it takes the color of the surrounding text.
    /// </summary>
    public string ToSvg() =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" aria-hidden=\"true\" focusable=\"false\">{Body}</svg>";
}
