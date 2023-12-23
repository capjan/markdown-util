namespace StaticCode.Highlighter;

public class LanguageToken
{
    public LanguageToken(string text, TokenType type)
    {
        Text = text;
        Type = type;
    }

    public string Text { get; }
    public TokenType Type { get; }

    public override string ToString()
    {
        return $"\"{Text}\" ({Type})";
    }
}