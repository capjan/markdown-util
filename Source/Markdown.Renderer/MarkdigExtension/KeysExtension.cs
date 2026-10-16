using System.Net;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Markdown.Renderer.MarkdigExtension;

/// <summary>
/// Renders keyboard shortcuts such as <c>++ctrl+alt+del++</c> as keys, with the syntax of the pymdownx Keys extension
/// used by Material for MkDocs. Keys are named (see <see cref="KeyboardKeys"/>) or quoted, such as <c>++cmd+"Ü"++</c>.
/// Text between <c>++</c> that is not a key combination stays inserted text.
/// </summary>
public class KeysExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // Runs before the emphasis parser, which reads ++text++ as inserted text.
        if (!pipeline.InlineParsers.Contains<KeysParser>())
            pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new KeysParser());
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not Markdig.Renderers.HtmlRenderer htmlRenderer) return;
        htmlRenderer.ObjectRenderers.AddIfNotAlready<KeysInlineRenderer>();
    }
}

/// <summary>A key in a shortcut: a named key, or a quoted key with only a label.</summary>
public sealed record ShortcutKey(string Label, KeyboardKey? Key);

public class KeysInline : LeafInline
{
    public KeysInline(IReadOnlyList<ShortcutKey> keys) => Keys = keys;
    public IReadOnlyList<ShortcutKey> Keys { get; }
}

public class KeysParser : InlineParser
{
    public KeysParser() => OpeningCharacters = new[] { '+' };

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var text = slice.Text;
        var end = slice.End;
        var start = slice.Start;
        if (start + 1 > end || text[start + 1] != '+') return false;

        var keys = new List<ShortcutKey>();
        var position = start + 2;
        while (true)
        {
            if (!TryReadKey(text, ref position, end, out var key)) return false;
            keys.Add(key);

            // "++" closes the shortcut; a single "+" separates two keys.
            if (position + 1 <= end && text[position] == '+' && text[position + 1] == '+') break;
            if (position > end || text[position] != '+') return false;
            position++;
        }

        var sourceStart = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new KeysInline(keys)
        {
            Span = new SourceSpan(sourceStart, sourceStart + position + 1 - start),
            Line = line,
            Column = column
        };
        slice.Start = position + 2;
        return true;
    }

    private static bool TryReadKey(string text, ref int position, int end, out ShortcutKey key)
    {
        key = null!;
        if (position > end) return false;

        if (text[position] == '"')
        {
            var closingQuote = text.IndexOf('"', position + 1, end - position);
            if (closingQuote <= position + 1) return false;
            key = new ShortcutKey(WebUtility.HtmlDecode(text[(position + 1)..closingQuote]), null);
            position = closingQuote + 1;
            return true;
        }

        var nameStart = position;
        while (position <= end && (char.IsAsciiLetterOrDigit(text[position]) || text[position] == '-')) position++;
        if (position == nameStart || !KeyboardKeys.TryGetKey(text[nameStart..position], out var keyboardKey)) return false;

        key = new ShortcutKey(keyboardKey.Label, keyboardKey);
        return true;
    }
}

public class KeysInlineRenderer : HtmlObjectRenderer<KeysInline>
{
    protected override void Write(Markdig.Renderers.HtmlRenderer renderer, KeysInline obj)
    {
        if (!renderer.EnableHtmlForInline)
        {
            renderer.Write(string.Join("+", obj.Keys.Select(key => key.Label)));
            return;
        }

        var attributes = new HtmlAttributes();
        attributes.AddClass("keys");
        obj.TryGetAttributes()?.CopyTo(attributes, mergeIdAndProperties: true);
        renderer.Write("<span").WriteAttributes(attributes).Write('>');
        for (var index = 0; index < obj.Keys.Count; index++)
        {
            if (index > 0) renderer.Write("<span>+</span>");
            WriteKey(renderer, obj.Keys[index]);
        }
        renderer.Write("</span>");
    }

    private static void WriteKey(Markdig.Renderers.HtmlRenderer renderer, ShortcutKey shortcutKey)
    {
        var key = shortcutKey.Key;
        if (key is null)
        {
            renderer.Write("<kbd>").WriteEscape(shortcutKey.Label).Write("</kbd>");
            return;
        }

        renderer.Write("<kbd class=\"key-").Write(key.Name).Write("\">");
        if (key.Symbol is not null)
            renderer.Write("<span class=\"key-symbol\" aria-hidden=\"true\">").Write(key.Symbol).Write("</span>");
        if (key.SymbolOnly)
            renderer.Write("<span class=\"visually-hidden\">").WriteEscape(key.Label).Write("</span>");
        else
            renderer.Write("<span class=\"key-label\">").WriteEscape(key.Label).Write("</span>");
        renderer.Write("</kbd>");
    }
}
