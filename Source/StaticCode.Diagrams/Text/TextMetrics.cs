using System.Text;

namespace StaticCode.Diagrams.Text;

/// <summary>
/// Estimates text extents without font files, using Helvetica-compatible advance widths.
/// Diagram text inherits the page's system font stack, so the estimate is padded slightly.
/// </summary>
internal static class TextMetrics
{
    public const double FontSize = 14;
    public const double LineHeight = 19;

    private const double SafetyFactor = 1.07;

    // Advance widths for ASCII 32..126 in 1/1000 em.
    private static readonly int[] AsciiWidths =
    {
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
        278, 278, 584, 584, 584, 556, 1015,
        667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833,
        722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611,
        278, 278, 278, 469, 556, 333,
        556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833,
        556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500,
        334, 260, 334, 584
    };

    public static double Width(string line)
    {
        double units = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var value = rune.Value;
            units += value switch
            {
                >= 32 and <= 126 => AsciiWidths[value - 32],
                < 0x0250 => 580,
                >= 0x2E80 => 1000,
                _ => 620
            };
        }

        return units / 1000 * FontSize * SafetyFactor;
    }

    /// <summary>Splits explicit line breaks and wraps words so that no line exceeds <paramref name="maxWidth"/>.</summary>
    public static IReadOnlyList<string> Wrap(string text, double maxWidth)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var current = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (current.Length > 0 && Width(candidate) > maxWidth)
                {
                    lines.Add(current.ToString());
                    current.Clear().Append(word);
                }
                else
                {
                    current.Clear().Append(candidate);
                }
            }

            lines.Add(current.ToString());
        }

        return lines;
    }

    public static (double Width, double Height) Measure(IReadOnlyList<string> lines) =>
        (lines.Count == 0 ? 0 : lines.Max(Width), lines.Count * LineHeight);
}
