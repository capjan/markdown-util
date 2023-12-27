using System.Net;
using Core.Text.Formatter;

namespace StaticCode.Highlighter;

/// <summary>
/// Formatter that produces html markup from a token stream dedicated to be embedded in a html website
/// </summary>
public class HtmlFormatter : ITextFormatter<IEnumerable<LanguageToken>>
{
    public void Write(IEnumerable<LanguageToken> tokens, TextWriter writer)
    {
        writer.Write("<pre>");
        writer.Write("<code>");
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Comment:
                    writer.Write("<span class=\"comment\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Keyword:
                    writer.Write("<span class=\"keyword\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Operator:
                    writer.Write("<span class=\"operator\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.String:
                    writer.Write("<span class=\"string\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Text:
                case TokenType.Whitespace:
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    break;
                case TokenType.NewLine:
                    writer.WriteLine();
                    break;
                case TokenType.Number:
                    writer.Write("<span class=\"number\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
            }
        }
        writer.Write("</code>");
        writer.Write("</pre>");
    }
}