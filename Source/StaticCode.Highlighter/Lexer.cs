using System.Text;
using System.Text.RegularExpressions;
using Core.Extensions.ParserRelated;
using Core.Parser;
using Core.Parser.Special;

namespace StaticCode.Highlighter;

class DisabledPredicate : IParserInputPredicate
{
    public static readonly IParserInputPredicate Instance = new DisabledPredicate();
    public bool IsMatch(IParserInput input, TextWriter writer)
    {
        return false;
    }

    private DisabledPredicate()
    {
        
    }
}
public class Lexer
{
    private static readonly Regex YamlMappingKeyRegex = new(
        "^(?<indent>[ \\t]*)(?:-[ \\t]+)?(?<key>\"(?:\\\\.|[^\"])*\"|'(?:''|[^'])*'|[^:#\\r\\n][^:#\\r\\n]*?):(?=[ \\t]|$)",
        RegexOptions.Compiled);

    public static Lexer ForLanguage(string name)
    {
        var setting = GrammarStore.GetByName(name);
        return new Lexer(setting);
    }
    private readonly IParserInputPredicate _keywordPredicate;
    private readonly IParserInputPredicate _numberPredicate;
    private readonly IParserInputPredicate _whitespacePredicate;
    private readonly IParserInputPredicate _newLinePredicate;
    private readonly IParserInputPredicate[] _stringPredicates;
    private readonly IParserInputPredicate _operatorPredicate;
    private readonly IParserInputPredicate _lineComment;
    private readonly bool _highlightKeys;
    private readonly bool _highlightShellTokens;

    private Lexer(Grammar settings)
    {
        _highlightKeys = settings.HighlightKeys;
        _highlightShellTokens = settings.HighlightShellTokens;
        _keywordPredicate = ParserInput.Predicate()
            .EqualsWordBoundary()
            .EqualsAny(settings.Keywords)
            .EqualsWordBoundary()
            .Predicate;
        _whitespacePredicate = ParserInput.Predicate()
            .Equals(new [] {' ', '\t'}, Repetition.OneOrMore)
            .Predicate;
        _newLinePredicate = ParserInput.Predicate()
            .Equals('\r', Repetition.ZeroOrOne)
            .Equals('\n')
            .Predicate;
        _stringPredicates = settings.Strings.Select(rule =>
        {
            return ParserInput.Predicate()
                .Equals(rule.StartEnd)
                .EqualsAny((option1, option2) =>
                {
                    option1.EqualsAny(rule.EscapeSequences);
                    option2.EqualsNot(rule.StartEnd);
                }, Repetition.ZeroOrMore)
                .Equals(rule.StartEnd)
                .Predicate;
        }).ToArray();
        _operatorPredicate = ParserInput.Predicate()
            .EqualsAny(settings.Operators)
            .Predicate;
        
        
        _numberPredicate = ParserInput.Predicate()
            .EqualsWordBoundary()
            .EqualsCharacterRange('0', '9', Repetition.OneOrMore)
            .Equals(block => block
                    .Equals('.')
                    .EqualsCharacterRange('0', '9', Repetition.OneOrMore)
            , Repetition.ZeroOrOne)
            .EqualsWordBoundary()
            .Predicate;
        
        if (string.IsNullOrEmpty(settings.LineCommentStart))
            _lineComment = DisabledPredicate.Instance;
        else
            _lineComment = ParserInput.Predicate()
            .Equals(settings.LineCommentStart)
            .EqualsNot(new [] {'\r', '\n'}, Repetition.ZeroOrMore)
            .Predicate;
    }

    public IEnumerable<LanguageToken> Analyse(TextReader reader)
    {
        if (!_highlightKeys && !_highlightShellTokens)
            return AnalyseCore(reader);

        var source = reader.ReadToEnd();
        var ranges = new List<(int Start, int End, TokenType Type)>();
        if (_highlightKeys)
            ranges.AddRange(FindYamlKeyRanges(source).Select(range => (range.Start, range.End, TokenType.Key)));
        if (_highlightShellTokens)
            ranges.AddRange(FindShellTokenRanges(source));

        return ApplyTokenRanges(AnalyseCore(new StringReader(source)), source.Length, ranges);
    }

    private IEnumerable<LanguageToken> AnalyseCore(TextReader reader)
    {
        var input = new ParserInput(reader);
        var otherTextBuilder = new StringBuilder();

        var done = false;
        while (!done)
        {
            // Line Comment
            if (input.TryReadMatch(_lineComment, out var lineComment))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(lineComment, TokenType.Comment);
                continue;
            }
            
            // Keyword
            if (input.TryReadMatch(_keywordPredicate, out var keyword))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(keyword, TokenType.Keyword);
                continue;
            }

            // Whitespace
            if (input.TryReadMatch(_whitespacePredicate, out var whitespace))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(whitespace, TokenType.Whitespace);
                continue;
            }
            
            // NewLine
            if (input.TryReadMatch(_newLinePredicate, out var newLine))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(newLine, TokenType.NewLine);
                continue;
            }
            
            // String
            var hasFoundString = false;
            var completeString = "";
            foreach (var stringPredicate in _stringPredicates)
            {
                if (input.TryReadMatch(stringPredicate, out completeString))
                {
                    hasFoundString = true;
                    break;
                }
            }

            if (hasFoundString)
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(completeString, TokenType.String);
                continue;
            }
            
            // Operator
            if (input.TryReadMatch(_operatorPredicate, out var operatorString))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(operatorString, TokenType.Operator);
                continue;
            }
            
            // Number
            if (input.TryReadMatch(_numberPredicate, out var numberString))
            {
                if (otherTextBuilder.Length != 0)
                {
                    var otherText = otherTextBuilder.ToString();
                    otherTextBuilder.Clear();
                    yield return new LanguageToken(otherText, TokenType.Text);
                }
                yield return new LanguageToken(numberString, TokenType.Number);
                continue;
            }

            // Put all other to the OtherStringBuilder
            if (input.TryReadChar(out var otherCharacter))
            {
                otherTextBuilder.Append(otherCharacter);
                continue;
            }

            if (otherTextBuilder.Length != 0)
            {
                var otherText = otherTextBuilder.ToString();
                otherTextBuilder.Clear();
                yield return new LanguageToken(otherText, TokenType.Text);
            }
            
            done = true;
        }
    }

    private static IEnumerable<LanguageToken> ApplyTokenRanges(
        IEnumerable<LanguageToken> tokens,
        int sourceLength,
        IReadOnlyCollection<(int Start, int End, TokenType Type)> ranges)
    {
        var tokenTypes = new TokenType?[sourceLength];
        foreach (var (start, end, type) in ranges)
        {
            for (var index = start; index < end; index++)
                tokenTypes[index] = type;
        }

        var sourcePosition = 0;
        foreach (var token in tokens)
        {
            var tokenOffset = 0;
            while (tokenOffset < token.Text.Length)
            {
                var type = tokenTypes[sourcePosition + tokenOffset] ?? token.Type;
                var runStart = tokenOffset;
                while (tokenOffset < token.Text.Length &&
                       (tokenTypes[sourcePosition + tokenOffset] ?? token.Type) == type)
                    tokenOffset++;

                yield return new LanguageToken(
                    token.Text[runStart..tokenOffset],
                    type);
            }

            sourcePosition += token.Text.Length;
        }
    }

    private static List<(int Start, int End)> FindYamlKeyRanges(string source)
    {
        var ranges = new List<(int Start, int End)>();
        int? blockScalarIndent = null;
        var lineStart = 0;

        while (lineStart < source.Length)
        {
            var lineEnd = lineStart;
            while (lineEnd < source.Length && source[lineEnd] is not ('\r' or '\n'))
                lineEnd++;

            var line = source[lineStart..lineEnd];
            var match = YamlMappingKeyRegex.Match(line);
            var indentation = 0;
            while (indentation < line.Length && line[indentation] is ' ' or '\t')
                indentation++;
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (blockScalarIndent.HasValue)
            {
                if (isBlank || indentation > blockScalarIndent.Value)
                {
                    lineStart = NextLineStart(source, lineEnd);
                    continue;
                }

                blockScalarIndent = null;
            }

            if (match.Success)
            {
                var key = match.Groups["key"];
                var keyEnd = key.Index + key.Length;
                while (keyEnd > key.Index && line[keyEnd - 1] is ' ' or '\t')
                    keyEnd--;

                ranges.Add((lineStart + key.Index, lineStart + keyEnd));

                var valueStart = key.Index + key.Length + 1;
                while (valueStart < line.Length && line[valueStart] is ' ' or '\t')
                    valueStart++;
                if (valueStart < line.Length && line[valueStart] is '|' or '>')
                    blockScalarIndent = indentation;
            }

            lineStart = NextLineStart(source, lineEnd);
        }

        return ranges;
    }

    private static List<(int Start, int End, TokenType Type)> FindShellTokenRanges(string source)
    {
        var ranges = new List<(int Start, int End, TokenType Type)>();
        var commandExpected = true;
        var commandSeen = false;
        var subcommandSeen = false;
        var redirectionTarget = false;
        var index = 0;

        while (index < source.Length)
        {
            var character = source[index];
            if (character is '\r' or '\n')
            {
                commandExpected = true;
                commandSeen = false;
                subcommandSeen = false;
                index++;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                index++;
                continue;
            }

            if (character == '#')
            {
                while (index < source.Length && source[index] is not ('\r' or '\n'))
                    index++;
                continue;
            }

            if (character is ';' or '|')
            {
                while (index < source.Length && source[index] == character)
                    index++;
                if (character == ';' && index < source.Length && source[index] == '&')
                    index++;
                commandExpected = true;
                commandSeen = false;
                subcommandSeen = false;
                continue;
            }

            if (character == '&')
            {
                if (index + 1 < source.Length && source[index + 1] is '<' or '>')
                {
                    index++;
                    redirectionTarget = true;
                    continue;
                }

                while (index < source.Length && source[index] == '&')
                    index++;
                commandExpected = true;
                commandSeen = false;
                subcommandSeen = false;
                continue;
            }

            if (character is '(' or '{')
            {
                index++;
                commandExpected = true;
                commandSeen = false;
                subcommandSeen = false;
                continue;
            }

            if (character is ')' or '}')
            {
                index++;
                commandExpected = true;
                commandSeen = false;
                subcommandSeen = false;
                continue;
            }

            if (character is '<' or '>')
            {
                while (index < source.Length && source[index] is '<' or '>' or '&')
                    index++;
                redirectionTarget = true;
                continue;
            }

            var wordStart = index;
            var containsQuote = false;
            var quote = '\0';
            while (index < source.Length)
            {
                character = source[index];
                if (quote == '\0' && (char.IsWhiteSpace(character) || character is '|' or '&' or ';' or '(' or ')' or '{' or '}' or '<' or '>'))
                    break;

                if (character == '\\' && index + 1 < source.Length && quote != '\'')
                {
                    index += 2;
                    continue;
                }

                if (quote == '\0' && character is '\'' or '"')
                {
                    quote = character;
                    containsQuote = true;
                }
                else if (quote == character)
                {
                    quote = '\0';
                }

                index++;
            }

            var word = source[wordStart..index];
            if (word.Length == 0)
            {
                index++;
                continue;
            }

            if (redirectionTarget)
            {
                redirectionTarget = false;
                continue;
            }

            if (word.All(char.IsDigit) && index < source.Length && source[index] is '<' or '>')
                continue;

            if (commandExpected)
            {
                if (IsShellPrefixKeyword(word))
                    continue;

                if (IsShellKeyword(word))
                {
                    commandExpected = false;
                    continue;
                }

                if (!containsQuote && IsShellAssignment(word))
                    continue;

                if (!containsQuote)
                    ranges.Add((wordStart, index, TokenType.Command));
                commandExpected = false;
                commandSeen = true;
                subcommandSeen = false;
                continue;
            }

            if (!containsQuote && IsShellOption(word))
            {
                ranges.Add((wordStart, index, TokenType.Option));
                continue;
            }

            if (!commandSeen)
                continue;

            if (!subcommandSeen)
            {
                if (!containsQuote)
                    ranges.Add((wordStart, index, TokenType.Subcommand));
                subcommandSeen = true;
            }
            else if (!containsQuote)
            {
                ranges.Add((wordStart, index, TokenType.Argument));
            }
        }

        return ranges;
    }

    private static bool IsShellPrefixKeyword(string word) => word is
        "if" or "then" or "else" or "elif" or "while" or "until" or "do" or "time" or "!";

    private static bool IsShellKeyword(string word) => word is
        "fi" or "for" or "done" or "case" or "esac" or "in" or "function" or "select" or "coproc";

    private static bool IsShellOption(string word) =>
        word.StartsWith("--", StringComparison.Ordinal) ||
        (word.Length > 1 && word[0] == '-' && !char.IsDigit(word[1]));

    private static bool IsShellAssignment(string word)
    {
        var equalsIndex = word.IndexOf('=');
        if (equalsIndex < 1 || !(char.IsLetter(word[0]) || word[0] == '_'))
            return false;

        for (var index = 1; index < equalsIndex; index++)
        {
            if (!(char.IsLetterOrDigit(word[index]) || word[index] == '_'))
                return false;
        }

        return true;
    }

    private static int NextLineStart(string source, int lineEnd)
    {
        if (lineEnd < source.Length && source[lineEnd] == '\r')
            lineEnd++;
        if (lineEnd < source.Length && source[lineEnd] == '\n')
            lineEnd++;
        return lineEnd;
    }
}
