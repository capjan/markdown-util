using System.Globalization;

namespace StaticCode.Icons;

/// <summary>
/// An icon with its shortcode name (without colons). Icons with a <see cref="DarkGlyph"/> show it in the dark theme
/// and <see cref="Glyph"/> in the light theme.
/// </summary>
public sealed record Icon(string Shortcode, IconGlyph Glyph, IconGlyph? DarkGlyph = null)
{
    /// <summary>
    /// Returns the icon as inline SVG. The SVG has no size, so CSS sizes it relative to the text. A themed icon returns
    /// one SVG per theme with the classes <c>icon-light</c> and <c>icon-dark</c>.
    /// </summary>
    public string ToSvg() =>
        DarkGlyph is null
            ? Glyph.ToSvg()
            : Glyph.ToSvg("icon-light") + DarkGlyph.ToSvg("icon-dark");
}

/// <summary>The SVG content of an icon and its view box.</summary>
public sealed record IconGlyph(string Body, double Left, double Top, double Width, double Height)
{
    public string ViewBox => string.Join(' ', new[] { Left, Top, Width, Height }.Select(value => value.ToString(CultureInfo.InvariantCulture)));

    public string ToSvg(string? className = null) =>
        $"<svg{(className is null ? string.Empty : $" class=\"{className}\"")} xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{ViewBox}\" aria-hidden=\"true\" focusable=\"false\">{Body}</svg>";
}
