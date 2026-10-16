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
                case TokenType.Key:
                    writer.Write("<span class=\"key\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Command:
                    writer.Write("<span class=\"command\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Subcommand:
                    writer.Write("<span class=\"subcommand\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Option:
                    writer.Write("<span class=\"option\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.Argument:
                    writer.Write("<span class=\"argument\">");
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
                case TokenType.MarkdownHeading:
                    writer.Write("<span class=\"md-heading\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownSyntax:
                    writer.Write("<span class=\"md-syntax\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownLink:
                    writer.Write("<span class=\"md-link\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownUrl:
                    writer.Write("<span class=\"md-url\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownCode:
                    writer.Write("<span class=\"md-code\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownEmphasis:
                    writer.Write("<span class=\"md-emphasis\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownStrong:
                    writer.Write("<span class=\"md-strong\">");
                    writer.Write(WebUtility.HtmlEncode(token.Text));
                    writer.Write("</span>");
                    break;
                case TokenType.MarkdownAlertNote:
                    WriteMarkdownAlert(token, writer, "note");
                    break;
                case TokenType.MarkdownAlertTip:
                    WriteMarkdownAlert(token, writer, "tip");
                    break;
                case TokenType.MarkdownAlertImportant:
                    WriteMarkdownAlert(token, writer, "important");
                    break;
                case TokenType.MarkdownAlertWarning:
                    WriteMarkdownAlert(token, writer, "warning");
                    break;
                case TokenType.MarkdownAlertCaution:
                    WriteMarkdownAlert(token, writer, "caution");
                    break;
            }
        }
        writer.Write("</code>");
        writer.Write("</pre>");
    }

    private static void WriteMarkdownAlert(LanguageToken token, TextWriter writer, string kind)
    {
        writer.Write($"<span class=\"md-alert-{kind}\">");
        writer.Write(WebUtility.HtmlEncode(token.Text));
        writer.Write("</span>");
    }
}
