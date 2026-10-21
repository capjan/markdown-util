using System.Text.RegularExpressions;
using Markdown.Renderer;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using YamlDotNet.RepresentationModel;

namespace MarkdownUtil.Service;

/// <summary>
/// Finds the local links of Markdown pages and rewrites them when pages or other files move, so relative links keep
/// pointing at the same files. Links to web addresses, absolute paths, and missing files are left alone.
/// </summary>
public static class MarkdownLinks
{
    public static readonly StringComparer PathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static readonly Regex Scheme = new(@"^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.Compiled);
    private static readonly Regex FrontMatterBlock = new(
        @"\A---[ \t]*\r?\n(?<yaml>[\s\S]*?)\r?\n---[ \t]*(?:\r?\n|$)",
        RegexOptions.Compiled);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();

    public static bool IsMarkdownFile(string path) =>
        Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(path).Equals(".markdown", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Lists the Markdown pages of a folder. Symbolic links are skipped, so pages outside the folder are never read
    /// or rewritten through them.
    /// </summary>
    public static IEnumerable<string> EnumerateMarkdownFiles(string rootPath, bool includeHidden) =>
        Directory.EnumerateFiles(rootPath, "*", new EnumerationOptions
            {
                AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint | (includeHidden ? 0 : FileAttributes.Hidden),
                RecurseSubdirectories = true
            })
            .Where(IsMarkdownFile)
            .Select(Path.GetFullPath);

    /// <summary>
    /// Rewrites the local links of the page at <paramref name="sourcePath"/>, which moves to
    /// <paramref name="newSourcePath"/>, for the files in <paramref name="movedPaths"/> that move as well.
    /// </summary>
    public static string Rewrite(
        string content,
        string sourcePath,
        string newSourcePath,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer,
        out int changedLinkCount)
    {
        var document = Markdig.Markdown.Parse(content, Pipeline);
        var edits = new Dictionary<(int Start, int Length), string>();

        foreach (var link in document.Descendants<LinkInline>())
            AddEdit(link.UrlSpan, link.Url);

        foreach (var definition in document.GetLinkReferenceDefinitions(true).OfType<LinkReferenceDefinition>())
            AddEdit(definition.UrlSpan, definition.Url);

        if (FindOpenApiSpec(content) is { } openApiSpec)
        {
            var specPath = OpenApiMarkdownRenderer.ResolveSpecPath(sourcePath, openApiSpec.Value);
            var movedSpecPath = movedPaths.GetValueOrDefault(specPath, specPath);
            if (!pathComparer.Equals(sourcePath, newSourcePath) || !pathComparer.Equals(specPath, movedSpecPath))
            {
                var updatedSpecPath = Path.IsPathRooted(openApiSpec.Value)
                    ? movedSpecPath
                    : Path.GetRelativePath(Path.GetDirectoryName(newSourcePath)!, movedSpecPath).Replace('\\', '/');
                if (updatedSpecPath != openApiSpec.Value)
                {
                    var escapedSpecPath = updatedSpecPath.Replace("\\", "\\\\", StringComparison.Ordinal)
                        .Replace("\"", "\\\"", StringComparison.Ordinal);
                    edits[(openApiSpec.Start, openApiSpec.Length)] = $"\"{escapedSpecPath}\"";
                }
            }
        }

        var rewritten = content;
        foreach (var edit in edits.OrderByDescending(edit => edit.Key.Start))
        {
            rewritten = rewritten.Remove(edit.Key.Start, edit.Key.Length)
                .Insert(edit.Key.Start, edit.Value);
        }

        changedLinkCount = edits.Count;
        return rewritten;

        void AddEdit(SourceSpan span, string? destination)
        {
            if (destination is null || span.Start < 0 || span.Length <= 0 || span.Start + span.Length > content.Length)
                return;

            var newDestination = GetUpdatedDestination(sourcePath, newSourcePath, destination, movedPaths, pathComparer);
            if (newDestination is null) return;

            var original = content.Substring(span.Start, span.Length);
            var replacement = original.StartsWith('<') && original.EndsWith('>')
                ? $"<{newDestination}>"
                : newDestination;
            if (replacement != original) edits[(span.Start, span.Length)] = replacement;
        }

        (string Value, int Start, int Length)? FindOpenApiSpec(string markdown)
        {
            var frontMatter = FrontMatterBlock.Match(markdown);
            if (!frontMatter.Success) return null;

            var yamlText = frontMatter.Groups["yaml"].Value;
            var yaml = new YamlStream();
            try
            {
                yaml.Load(new StringReader(yamlText));
            }
            catch (YamlDotNet.Core.YamlException)
            {
                return null;
            }

            if (yaml.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode mapping ||
                !mapping.Children.TryGetValue(new YamlScalarNode("openapi"), out var node) ||
                node is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value)) return null;

            var span = frontMatter.Groups["yaml"].Index + checked((int)scalar.Start.Index);
            var length = checked((int)(scalar.End.Index - scalar.Start.Index));
            return (scalar.Value, span, length);
        }
    }

    /// <summary>Returns the local links of a page together with the files they point to.</summary>
    public static IReadOnlyList<LocalLink> Find(string content, string sourcePath)
    {
        var document = Markdig.Markdown.Parse(content, Pipeline);
        var links = new List<LocalLink>();
        foreach (var link in document.Descendants<LinkInline>())
            Add(link.Line, link.Span, link.Url);
        foreach (var definition in document.GetLinkReferenceDefinitions(true).OfType<LinkReferenceDefinition>())
            Add(definition.Line, definition.Span, definition.Url);
        return links.OrderBy(link => link.Line).ToList();

        void Add(int line, SourceSpan span, string? destination)
        {
            if (destination is null || ResolveTarget(sourcePath, destination, null, out _, out _) is not { } targetPath) return;
            var markdown = span.Start >= 0 && span.End < content.Length ? content[span.Start..(span.End + 1)] : destination;
            links.Add(new LocalLink(line + 1, markdown, targetPath));
        }
    }

    private static string? GetUpdatedDestination(
        string sourcePath,
        string newSourcePath,
        string destination,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer)
    {
        if (ResolveTarget(sourcePath, destination, movedPaths, out var suffixStart, out var viaFolder) is not { } targetPath) return null;

        var targetMoved = movedPaths.TryGetValue(targetPath, out var movedTargetPath);
        var finalTargetPath = targetMoved ? movedTargetPath! : targetPath;
        if (!targetMoved && pathComparer.Equals(sourcePath, newSourcePath)) return null;

        // A link to a folder stays a link to the folder while its README.md is still the folder page.
        var keepFolder = viaFolder && Path.GetFileName(finalTargetPath).Equals("README.md", StringComparison.OrdinalIgnoreCase);
        if (StillPointsTo(destination, suffixStart, newSourcePath, keepFolder ? Path.GetDirectoryName(finalTargetPath)! : finalTargetPath))
            return null;
        var relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(newSourcePath)!,
            keepFolder ? Path.GetDirectoryName(finalTargetPath)! : finalTargetPath);
        var encodedTarget = string.Join('/', relativeTarget
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Select(Uri.EscapeDataString));
        var path = suffixStart < 0 ? destination : destination[..suffixStart];
        if (keepFolder && path.EndsWith('/')) encodedTarget += "/";
        var suffix = suffixStart < 0 ? string.Empty : destination[suffixStart..];
        return encodedTarget + suffix;
    }

    // Links between files that move together, such as pages of a moved folder, often still work unchanged. The
    // comparison respects case even where the file system does not, because web servers usually do.
    private static bool StillPointsTo(string destination, int suffixStart, string newSourcePath, string target)
    {
        var relativePath = Uri.UnescapeDataString(suffixStart < 0 ? destination : destination[..suffixStart]);
        var resolved = Path.GetFullPath(relativePath, Path.GetDirectoryName(newSourcePath)!);
        return string.Equals(Path.TrimEndingDirectorySeparator(resolved), Path.TrimEndingDirectorySeparator(target), StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves a link destination relative to its page. A folder resolves to its <c>README.md</c>. Returns null for
    /// web addresses, absolute paths, and files that neither exist nor move.
    /// </summary>
    private static string? ResolveTarget(
        string sourcePath,
        string destination,
        IReadOnlyDictionary<string, string>? movedPaths,
        out int suffixStart,
        out bool viaFolder)
    {
        suffixStart = -1;
        viaFolder = false;
        if (string.IsNullOrWhiteSpace(destination) ||
            destination.StartsWith("//", StringComparison.Ordinal) ||
            Scheme.IsMatch(destination))
            return null;

        suffixStart = destination.IndexOfAny(['?', '#']);
        var relativePath = suffixStart < 0 ? destination : destination[..suffixStart];
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath)) return null;

            var targetPath = Path.GetFullPath(relativePath, Path.GetDirectoryName(sourcePath)!);
            if (Directory.Exists(targetPath))
            {
                var readmePath = Path.Combine(targetPath, "README.md");
                viaFolder = File.Exists(readmePath);
                return viaFolder ? readmePath : null;
            }

            return File.Exists(targetPath) || movedPaths?.ContainsKey(targetPath) == true ? targetPath : null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return null;
        }
    }
}

/// <param name="Line">The 1-based line of the link in its page.</param>
/// <param name="Markdown">The Markdown of the link, such as <c>[Setup](setup.md)</c>.</param>
/// <param name="TargetPath">The full path of the file the link points to.</param>
public sealed record LocalLink(int Line, string Markdown, string TargetPath);
