using System.Text;
using Core.Extensions.TextRelated;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdown.Renderer.MarkdigExtension;
using StaticCode.Diagrams;
using StaticCode.Highlighter;

namespace Markdown.Renderer;

public class HighlightedCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
  private readonly HtmlFormatter _codeFormatter = new();

  protected override void Write(Markdig.Renderers.HtmlRenderer renderer, CodeBlock obj)
    {
        var code = GetCode(obj);
        var languageName = "NoHighlight";

        if (obj is FencedCodeBlock fencedCodeBlock)
        {
            var parser = fencedCodeBlock.Parser as FencedCodeBlockParser
                ?? throw new InvalidOperationException("expected FencedCodeBlockParser");
            languageName = fencedCodeBlock.Info?.Replace(parser.InfoPrefix ?? string.Empty, string.Empty);
            if (string.IsNullOrEmpty(languageName)) languageName = "NoHighlight";

            if (languageName == IconSearch.CodeBlockLanguage)
            {
                renderer.WriteLine(IconSearch.Html);
                return;
            }

            if (languageName.Equals("math", StringComparison.OrdinalIgnoreCase))
            {
                renderer.WriteLine(MathFormulaRenderer.Render(code, display: true));
                return;
            }

            // Supported diagrams become static SVG; everything else stays a readable code block.
            if (DiagramRenderer.TryRenderSvg(languageName, code, out var svg))
            {
                renderer.Write("<figure class=\"diagram\">").Write(svg).WriteLine("</figure>");
                return;
            }
        }

        var lexer = Lexer.ForLanguage(languageName);
        var tokens = lexer.Analyse(code);
        var renderedHtml = _codeFormatter.WriteToString(tokens);
        renderer.WriteLine(renderedHtml);
    }

    private static string GetCode(LeafBlock obj)
    {
      var stringBuilder = new StringBuilder();
      // An empty code block has no line array.
      if (obj.Lines.Lines == null) return string.Empty;
      var isFirstLine = true;
      foreach (var line in obj.Lines.Lines)
      {
        var slice = line.Slice;
        if (slice.Text != null)
        {
          if (!isFirstLine)
            stringBuilder.AppendLine();
          stringBuilder.Append(slice.Text.Substring(slice.Start, slice.Length));
          isFirstLine = false;
        }
      }
      return stringBuilder.ToString();
    }
    
  }
