namespace Markdown.Linter.Rules.Rule3;

/// <summary>
/// Input for the Rule 3: Casing of internal links must match the casing on the current machine
/// </summary>
/// <param name="DocumentFilePath">FilePath of the markdown file that contains the link</param>
/// <param name="LinkDestination">Link that casing should be checked</param>
public record Rule3Input(string DocumentFilePath, string LinkDestination);