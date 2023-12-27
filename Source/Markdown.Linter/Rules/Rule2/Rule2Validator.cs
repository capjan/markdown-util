using System.Text.RegularExpressions;
using FluentValidation;
using Markdown.Linter.Constants;

namespace Markdown.Linter.Rules.Rule2;

/// <summary>
/// The name of an directory should match the title of the containing README.md file 
/// </summary>
/// <remarks>
/// e.g. the markdown util has a title like # notes than the directory name should 
/// </remarks>
public class Rule2Validator : AbstractValidator<Rule2Input>
{
    public Rule2Validator()
    {
        RuleFor(x => x.DirectoryName).Must(BeAValidDirectoryName).WithMessage(CustomInvalidDirectoryNameMessage);
    }

    private bool BeAValidDirectoryName(Rule2Input input, string directoryName)
    {
        var shortTitle = ShortenTitle(input.Title);
        var shortDirectoryName = ShortenDirectoryName(directoryName);

        return shortDirectoryName.Equals(shortTitle, StringComparison.OrdinalIgnoreCase);
    }

    private string CustomInvalidDirectoryNameMessage(Rule2Input input)
    {
        var shortTitle = SuggestDirectoryName(input.Title);
        return $"The folder name of a index page must match its shortTitle. expected: {shortTitle}, found: {input.DirectoryName}";
    }
    
    private static string ShortenTitle(string input)
    {
        // cut titles like "Rule 2: Directory Name must match Title" to "Rule 2"
        var cutTitle = CutStringAtFirst(input, ':');
        return RemoveInvalidAndIgnoredCharacters(cutTitle).ToLower();
    }

    private static string ShortenDirectoryName(string directoryName) => RemoveInvalidAndIgnoredCharacters(directoryName).ToLower();
    private static string RemoveIgnoredCharacters(string input) => Regex.Replace(input, "[_-]", "");
    
    private static string RemoveInvalidCharacters(String input) => Regex.Replace(input, "[^A-Za-z0-9_-]", "");
    
    private static string RemoveInvalidAndIgnoredCharacters(string input)
    {
        var validInput = RemoveInvalidCharacters(input);
        return RemoveIgnoredCharacters(validInput);
    }
    private static string CutStringAtFirst(string input, char cutCharacter)
    {
        var indexOfCutCharacter = input.IndexOf(cutCharacter);

        return indexOfCutCharacter != -1
            ? input[..indexOfCutCharacter]
            : input;
    }

    private static string SuggestDirectoryName(string title)
    {
        var cutTitle = CutStringAtFirst(title, KnownChars.Colon);
        var trimmedCutTitle = cutTitle.Trim();
        var trimmedCutAndReplacedTitle = trimmedCutTitle.Replace(' ', '-');
        return RemoveInvalidCharacters(trimmedCutAndReplacedTitle);
    }
}