using System.Text;
using Core.Extensions.TextRelated;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using StaticCode.Highlighter;

namespace Markdown.Renderer;

public class HighlightedCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
  private readonly HtmlFormatter _codeFormatter = new();

  protected override void Write(Markdig.Renderers.HtmlRenderer renderer, CodeBlock obj)
    {
      var fencedCodeBlock = obj as FencedCodeBlock ?? throw new InvalidOperationException("Expected FencedCodeBlock");
      var parser = obj.Parser as FencedCodeBlockParser ?? throw new InvalidOperationException("expected FencedCodeBlockParser");
      
        var languageName = fencedCodeBlock.Info?.Replace(parser.InfoPrefix ?? string.Empty, string.Empty);
        if (string.IsNullOrEmpty(languageName)) languageName = "NoHighlight";

   
        var code = GetCode(obj, out _);
        
        var lexer = Lexer.ForLanguage(languageName);
        var tokens = lexer.Analyse(code);
        var renderedHtml = _codeFormatter.WriteToString(tokens);
        renderer.WriteLine(renderedHtml);
          
          // attributes.AddClass(string.Format("lang-{0}", (object) languageMoniker));
          // attributes.Classes.Remove(string.Format("language-{0}", (object) languageMoniker));
          // attributes.AddClass("editor-colors");
          // string firstLine;
          //
          // var htmlFormattedCode = 
          // renderer.Write("<div").WriteAttributes(attributes, (Func<string, string>) null).Write(">");
          // var content = code;
          //
          // renderer.WriteLine("</div>");
        
      
    }

    // private string ApplySyntaxHighlighting(string languageMoniker, string firstLine, string code)
    // {
    //   var language = new LanguageTypeAdapter().Parse(languageMoniker, firstLine);
    //   if (language == null)
    //     return code;
    //   var sb = new StringBuilder();
    //   var stringWriter = new StringWriter(sb);
    //   new CodeColorizer().Colorize(code, language, Formatters.Default, styleSheet, (TextWriter) stringWriter);
    //   return sb.ToString();
    // }

    private static string GetCode(LeafBlock obj, out string? firstLine)
    {
      var stringBuilder = new StringBuilder();
      firstLine = null;
      foreach (var line in obj.Lines.Lines)
      {
        var slice = line.Slice;
        if (slice.Text != null)
        {
          var str = slice.Text.Substring(slice.Start, slice.Length);
          if (firstLine == null)
            firstLine = str;
          else
            stringBuilder.AppendLine();
          stringBuilder.Append(str);
        }
      }
      return stringBuilder.ToString();
    }
    
  }