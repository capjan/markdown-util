using System.Text;
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

    private Lexer(Grammar settings)
    {
        
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
}