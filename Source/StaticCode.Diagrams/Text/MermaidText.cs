using System.Globalization;
using System.Text.RegularExpressions;

namespace StaticCode.Diagrams.Text;

/// <summary>Turns Mermaid label text into plain text: <c>&lt;br&gt;</c> becomes a line break and <c>#quot;</c>-style codes are decoded.</summary>
internal static class MermaidText
{
    private static readonly Regex LineBreakRegex = new(@"<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EntityRegex = new(@"#(?<code>\d+|[a-zA-Z]+);", RegexOptions.Compiled);

    public static string Clean(string text)
    {
        text = LineBreakRegex.Replace(text, "\n");
        text = EntityRegex.Replace(text, match => DecodeEntity(match.Groups["code"].Value) ?? match.Value);
        return string.Join('\n', text.Split('\n').Select(line => line.Trim())).Trim();
    }

    private static string? DecodeEntity(string code)
    {
        if (int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var codePoint))
            return codePoint is > 0 and <= 0x10FFFF and not (>= 0xD800 and <= 0xDFFF) ? char.ConvertFromUtf32(codePoint) : null;

        return code switch
        {
            "quot" => "\"",
            "amp" => "&",
            "lt" => "<",
            "gt" => ">",
            "nbsp" => " ",
            _ => null
        };
    }
}
