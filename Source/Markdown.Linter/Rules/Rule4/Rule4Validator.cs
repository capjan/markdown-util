using System.Text.RegularExpressions;
using FluentValidation;
using Markdown.Linter;

namespace Markdown.Linter.Rules.Rule4;

/// <summary>
/// Checks that relative local links point to a file or an indexed directory.
/// </summary>
public class Rule4Validator : AbstractValidator<Rule4Input>
{
    public Rule4Validator()
    {
        RuleFor(x => x.LinkDestination)
            .Must(LinkTargetExists)
            .WithMessage(input =>
                $"{RuleCodes.LocalLinkExists} {RuleCodes.LocalLinkExistsName}: Local link target '{input.LinkDestination}' does not exist.");
    }

    private static bool LinkTargetExists(Rule4Input input, string destination)
    {
        if (string.IsNullOrWhiteSpace(destination) || destination.StartsWith('#') ||
            destination.StartsWith("//", StringComparison.Ordinal) ||
            Regex.IsMatch(destination, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
            return true;

        var pathEnd = destination.IndexOfAny(['?', '#']);
        var relativePath = pathEnd < 0 ? destination : destination[..pathEnd];
        if (string.IsNullOrWhiteSpace(relativePath)) return true;

        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath)) return true;

            var sourceDirectory = Path.GetDirectoryName(input.DocumentFilePath);
            if (string.IsNullOrEmpty(sourceDirectory)) return true;

            var targetPath = Path.GetFullPath(relativePath, sourceDirectory);
            if (File.Exists(targetPath)) return true;

            return Directory.Exists(targetPath) &&
                   File.Exists(Path.Combine(targetPath, "README.md"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return false;
        }
    }
}
