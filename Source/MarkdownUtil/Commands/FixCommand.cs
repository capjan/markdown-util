using Core.Collections.NodeGraph.Extensions;
using Markdown.Linter;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class FixCommand
{
    private static readonly Regex FrontMatterBlock = new(
        @"\A---[ \t]*\r?\n(?<yaml>[\s\S]*?)\r?\n---[ \t]*(?:\r?\n|$)",
        RegexOptions.Compiled);
    private readonly GraphBuilder _graphBuilder;
    private readonly LinterVisitor _linterVisitor;

    public FixCommand(GraphBuilder graphBuilder, LinterVisitor linterVisitor)
    {
        _graphBuilder = graphBuilder;
        _linterVisitor = linterVisitor;
    }

    public int Execute(VisitorSettings settings)
    {
        string rootPath;
        try
        {
            rootPath = CommandPathDefaults.ResolveRootPath(settings.RootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        var pathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var migrationChanges = MigrateMkDocsContent(rootPath, settings.IncludeHidden, pathComparer);
        if (migrationChanges < 0) return 1;

        var graph = _graphBuilder.CreateGraph(new VisitorSettings
        {
            RootPath = rootPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden
        });
        _linterVisitor.Prepare(graph);
        graph.Visit(_linterVisitor);

        var moves = _linterVisitor.ErrorReceiver.Diagnostics
            .Where(diagnostic => diagnostic.IsWarning &&
                                 diagnostic.Message.StartsWith($"{RuleCodes.ReadmeWithoutSubpages}:", StringComparison.Ordinal))
            .Select(diagnostic => CreateMove(Path.GetFullPath(diagnostic.FilePath)))
            .ToArray();

        if (moves.Length == 0)
        {
            Console.WriteLine(migrationChanges == 0
                ? "No fixable issues found."
                : $"Applied {migrationChanges} MkDocs compatibility and structure fix{(migrationChanges == 1 ? "" : "es")}.");
            return 0;
        }

        var movedPaths = moves.ToDictionary(move => move.SourcePath, move => move.TargetPath, pathComparer);
        var duplicateTargets = moves
            .GroupBy(move => move.TargetPath, pathComparer)
            .FirstOrDefault(group => group.Count() > 1);
        var existingTarget = moves.FirstOrDefault(move =>
            File.Exists(move.TargetPath) && !pathComparer.Equals(move.SourcePath, move.TargetPath));
        if (duplicateTargets is not null || existingTarget is not null)
        {
            var target = duplicateTargets?.Key ?? existingTarget!.TargetPath;
            Console.Error.WriteLine($"Cannot fix MDU006 because the target already exists: {target}");
            return 1;
        }

        var enumerationOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.System
                : FileAttributes.Hidden | FileAttributes.System,
            RecurseSubdirectories = true
        };
        var markdownFiles = Directory.EnumerateFiles(rootPath, "*", enumerationOptions)
            .Where(IsMarkdownFile)
            .ToArray();
        var changedDocuments = new Dictionary<string, string>(pathComparer);
        var updatedLinkCount = 0;

        foreach (var markdownFile in markdownFiles)
        {
            var sourcePath = Path.GetFullPath(markdownFile);
            var newSourcePath = movedPaths.GetValueOrDefault(sourcePath, sourcePath);
            var content = File.ReadAllText(sourcePath);
            var updatedContent = RewriteLinks(content, sourcePath, newSourcePath, movedPaths, pathComparer, out var changedLinkCount);
            if (updatedContent != content)
            {
                changedDocuments[sourcePath] = updatedContent;
                updatedLinkCount += changedLinkCount;
            }
        }

        foreach (var move in moves)
            File.Move(move.SourcePath, move.TargetPath);

        foreach (var (sourcePath, content) in changedDocuments)
        {
            var targetPath = movedPaths.GetValueOrDefault(sourcePath, sourcePath);
            File.WriteAllText(targetPath, content);
        }

        foreach (var move in moves)
        {
            var sectionDirectory = Path.GetDirectoryName(move.SourcePath)!;
            if (Directory.Exists(sectionDirectory) && !Directory.EnumerateFileSystemEntries(sectionDirectory).Any())
                Directory.Delete(sectionDirectory);

            Console.WriteLine($"Fixed MDU006: {Path.GetRelativePath(rootPath, move.SourcePath)} -> {Path.GetRelativePath(rootPath, move.TargetPath)}");
        }

        Console.WriteLine($"Updated {updatedLinkCount} Markdown link{(updatedLinkCount == 1 ? "" : "s")}.");
        Console.WriteLine($"Applied {migrationChanges} MkDocs compatibility and structure fix{(migrationChanges == 1 ? "" : "es")}.");
        return 0;
    }

    private static int MigrateMkDocsContent(string rootPath, bool includeHidden, StringComparer pathComparer)
    {
        var enumerationOptions = new EnumerationOptions
        {
            AttributesToSkip = includeHidden ? FileAttributes.System : FileAttributes.Hidden | FileAttributes.System,
            RecurseSubdirectories = true
        };
        var markdownFiles = Directory.EnumerateFiles(rootPath, "*", enumerationOptions)
            .Where(IsMarkdownFile).Select(Path.GetFullPath).ToArray();
        var indexMoves = markdownFiles
            .Where(path => Path.GetFileName(path).Equals("index.md", StringComparison.OrdinalIgnoreCase))
            .Select(path => new PageMove(path, Path.Combine(Path.GetDirectoryName(path)!, "README.md")))
            .ToArray();
        var duplicateTarget = indexMoves.FirstOrDefault(move => File.Exists(move.TargetPath));
        if (duplicateTarget is not null)
        {
            Console.Error.WriteLine($"Cannot rename '{duplicateTarget.SourcePath}' because '{duplicateTarget.TargetPath}' already exists.");
            return -1;
        }
        var duplicateMoveTarget = indexMoves.GroupBy(move => move.TargetPath, pathComparer).FirstOrDefault(group => group.Count() > 1);
        if (duplicateMoveTarget is not null)
        {
            Console.Error.WriteLine($"Cannot rename multiple index pages to '{duplicateMoveTarget.Key}'.");
            return -1;
        }

        var movedPaths = indexMoves.ToDictionary(move => move.SourcePath, move => move.TargetPath, pathComparer);
        var updated = new Dictionary<string, string>(pathComparer);
        var changes = 0;
        foreach (var path in markdownFiles)
        {
            var content = File.ReadAllText(path);
            var fixedContent = MarkdownCompatibility.FixMarkdown(content);
            var targetPath = movedPaths.GetValueOrDefault(path, path);
            if (fixedContent != content || !pathComparer.Equals(path, targetPath))
            {
                updated[targetPath] = fixedContent;
                if (fixedContent != content) changes++;
            }
        }

        var pagesFiles = Directory.EnumerateFiles(rootPath, ".pages", new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.System,
            RecurseSubdirectories = true
        }).ToArray();
        Dictionary<string, Dictionary<string, string>> pageSettings;
        try
        {
            pageSettings = ReadPagesSettings(pagesFiles, rootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return -1;
        }

        try
        {
            CreateMissingSectionReadmes(rootPath, enumerationOptions, movedPaths, updated, pathComparer, ref changes);
            foreach (var (path, settings) in pageSettings)
            {
                var sourcePath = movedPaths.FirstOrDefault(move => pathComparer.Equals(move.Value, path)).Key ?? path;
                if (!updated.TryGetValue(path, out var content))
                {
                    if (!File.Exists(sourcePath)) continue;
                    content = File.ReadAllText(sourcePath);
                }

                foreach (var (key, value) in settings)
                    content = SetFrontMatterValue(content, key, value, path);
                updated[path] = content;
                changes++;
            }

            WriteStaticBlogAndTags(rootPath, updated, movedPaths, pathComparer, ref changes);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return -1;
        }

        var changedLinkCount = 0;
        foreach (var path in markdownFiles)
        {
            var sourcePath = Path.GetFullPath(path);
            var newSourcePath = movedPaths.GetValueOrDefault(sourcePath, sourcePath);
            var content = updated.GetValueOrDefault(newSourcePath) ?? File.ReadAllText(sourcePath);
            var rewritten = RewriteLinks(content, sourcePath, newSourcePath, movedPaths, pathComparer, out var linksChanged);
            if (rewritten != content)
            {
                updated[newSourcePath] = rewritten;
                changedLinkCount += linksChanged;
            }
        }

        foreach (var move in indexMoves) changes++;

        // All Markdown and YAML transformations are prepared before the first filesystem mutation.
        foreach (var move in indexMoves)
            File.Move(move.SourcePath, move.TargetPath);

        foreach (var (targetPath, content) in updated)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllText(targetPath, content);
        }

        foreach (var pagesFile in pagesFiles)
        {
            File.Delete(pagesFile);
            changes++;
        }

        if (changedLinkCount > 0)
        {
            changes++;
            Console.WriteLine($"Updated {changedLinkCount} Markdown link{(changedLinkCount == 1 ? "" : "s")} during migration.");
        }
        return changes;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadPagesSettings(
        IEnumerable<string> pagesFiles,
        string rootPath)
    {
        var settings = new Dictionary<string, Dictionary<string, string>>(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);
        foreach (var pagesFile in pagesFiles)
        {
            var yaml = new YamlStream();
            try
            {
                yaml.Load(new StringReader(File.ReadAllText(pagesFile)));
            }
            catch (YamlDotNet.Core.YamlException exception)
            {
                throw new InvalidOperationException($"Cannot migrate {pagesFile}: {exception.Message}");
            }

            var rootNode = yaml.Documents.FirstOrDefault()?.RootNode;
            if (rootNode is not YamlMappingNode mapping)
            {
                if (rootNode is YamlScalarNode empty && string.IsNullOrWhiteSpace(empty.Value)) continue;
                throw new InvalidOperationException($"Cannot migrate {pagesFile}: expected a YAML mapping.");
            }
            var directory = Path.GetDirectoryName(pagesFile)!;
            if (mapping.Children.TryGetValue(new YamlScalarNode("title"), out var titleNode))
            {
                if (titleNode is not YamlScalarNode title)
                    throw new InvalidOperationException($"Cannot migrate {pagesFile}: 'title' must be a string.");
                Add(Path.Combine(directory, "README.md"), "sidebar.title", title.Value ?? string.Empty);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("arrange"), out var arrangeNode))
            {
                if (arrangeNode is not YamlSequenceNode arrange)
                    throw new InvalidOperationException($"Cannot migrate {pagesFile}: 'arrange' must be a list.");
                for (var index = 0; index < arrange.Children.Count; index++)
                {
                    if (arrange.Children[index] is not YamlScalarNode entry || string.IsNullOrWhiteSpace(entry.Value))
                        throw new InvalidOperationException($"Cannot migrate {pagesFile}: 'arrange' entries must be non-empty strings.");
                    var value = entry.Value.Trim();
                    string target;
                    if (value.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        target = Path.GetFullPath(Path.Combine(directory, value));
                    }
                    else
                    {
                        var section = Path.GetFullPath(Path.Combine(directory, value));
                        target = Path.Combine(section, "README.md");
                    }

                    if (Path.GetFileName(target).Equals("index.md", StringComparison.OrdinalIgnoreCase))
                        target = Path.Combine(Path.GetDirectoryName(target)!, "README.md");
                    Add(target, "sidebar.order", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }

        return settings;

        void Add(string path, string key, string value)
        {
            var fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) &&
                !fullPath.Equals(Path.GetFullPath(rootPath), OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return;
            if (!settings.TryGetValue(fullPath, out var values)) settings[fullPath] = values = new Dictionary<string, string>(StringComparer.Ordinal);
            values[key] = value;
        }
    }

    private static void CreateMissingSectionReadmes(
        string rootPath,
        EnumerationOptions options,
        IReadOnlyDictionary<string, string> movedPaths,
        Dictionary<string, string> updated,
        StringComparer pathComparer,
        ref int changes)
    {
        var directOptions = new EnumerationOptions
        {
            AttributesToSkip = options.AttributesToSkip,
            RecurseSubdirectories = false
        };
        var directories = Directory.EnumerateDirectories(rootPath, "*", options).Append(rootPath)
            .OrderBy(path => path.Length).ToArray();
        foreach (var directory in directories)
        {
            var directMarkdown = Directory.EnumerateFiles(directory, "*", directOptions).Where(IsMarkdownFile).ToArray();
            var hasMarkdownChildDirectory = Directory.EnumerateDirectories(directory, "*", directOptions).Any(ContainsMarkdown);
            if (!hasMarkdownChildDirectory && directMarkdown.Length < 2) continue;
            if (directMarkdown.Any(path => Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                                           movedPaths.Values.Any(target => pathComparer.Equals(target, Path.Combine(directory, "README.md"))))) continue;

            var readmePath = Path.Combine(directory, "README.md");
            updated[readmePath] = $"# {Path.GetFileName(directory)}{Environment.NewLine}";
            changes++;
        }

        bool ContainsMarkdown(string directory)
        {
            return Directory.EnumerateFiles(directory, "*", directOptions).Any(IsMarkdownFile) ||
                   Directory.EnumerateDirectories(directory, "*", directOptions).Any(ContainsMarkdown);
        }
    }

    private static string SetFrontMatterValue(string content, string key, string value, string path)
    {
        YamlMappingNode mapping;
        string body;
        var match = FrontMatterBlock.Match(content);
        if (match.Success)
        {
            var yaml = new YamlStream();
            try
            {
                yaml.Load(new StringReader(match.Groups["yaml"].Value));
            }
            catch (YamlDotNet.Core.YamlException exception)
            {
                throw new InvalidOperationException($"Cannot migrate malformed Markdown front matter in '{path}': {exception.Message}");
            }

            mapping = yaml.Documents.FirstOrDefault()?.RootNode as YamlMappingNode
                ?? throw new InvalidOperationException($"Cannot migrate Markdown front matter in '{path}' because it must be a YAML mapping.");
            body = content[match.Length..];
        }
        else
        {
            if (StartsWithFrontMatter(content))
                throw new InvalidOperationException($"Cannot migrate Markdown front matter in '{path}' because its closing '---' delimiter is missing.");
            mapping = new YamlMappingNode();
            body = content;
        }

        var keyParts = key.Split('.');
        var destination = mapping;
        for (var index = 0; index < keyParts.Length - 1; index++)
        {
            var part = new YamlScalarNode(keyParts[index]);
            if (!destination.Children.TryGetValue(part, out var nested) || nested is not YamlMappingNode nestedMapping)
            {
                nestedMapping = new YamlMappingNode();
                destination.Children[part] = nestedMapping;
            }
            destination = nestedMapping;
        }

        destination.Children[new YamlScalarNode(keyParts[^1])] = new YamlScalarNode(value);
        var serializer = new SerializerBuilder().Build();
        using var writer = new StringWriter();
        serializer.Serialize(writer, mapping);
        var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var serialized = writer.ToString().TrimEnd().Replace("\n", newline, StringComparison.Ordinal);
        return $"---{newline}{serialized}{newline}---{newline}{newline}{body}";
    }

    private static string ReplaceMarkdownBody(string content, string body, string path)
    {
        var match = FrontMatterBlock.Match(content);
        if (!match.Success)
        {
            if (StartsWithFrontMatter(content))
                throw new InvalidOperationException($"Cannot migrate malformed Markdown front matter in '{path}' because its closing '---' delimiter is missing.");
            return body;
        }
        var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return content[..match.Length].TrimEnd('\r', '\n') + newline + newline + body;
    }

    private static void WriteStaticBlogAndTags(
        string rootPath,
        Dictionary<string, string> updated,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer,
        ref int changes)
    {
        var blogDirectory = Path.Combine(rootPath, "blog");
        var blogIndex = Path.Combine(blogDirectory, "README.md");
        if (File.Exists(blogIndex) || updated.ContainsKey(blogIndex))
        {
            var existing = updated.GetValueOrDefault(blogIndex) ??
                           (movedPaths.FirstOrDefault(move => pathComparer.Equals(move.Value, blogIndex)).Key is { } oldBlogIndex
                               ? File.ReadAllText(oldBlogIndex)
                               : File.ReadAllText(blogIndex));
            var body = RemoveSimpleFrontMatter(existing).Trim();
            if (Regex.IsMatch(body, @"^# Blog\s*$", RegexOptions.IgnoreCase))
            {
                var posts = Directory.Exists(Path.Combine(blogDirectory, "posts"))
                    ? Directory.EnumerateFiles(Path.Combine(blogDirectory, "posts"), "*.md")
                        .Where(path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
                        .Select(path =>
                        {
                            var fullPath = Path.GetFullPath(path);
                            return (Path: path, TargetPath: movedPaths.GetValueOrDefault(fullPath, fullPath), Metadata: ReadMetadata(path));
                        })
                        .OrderByDescending(item => item.Metadata.GetValueOrDefault("date"), StringComparer.Ordinal)
                        .ToArray()
                    : Array.Empty<(string Path, string TargetPath, Dictionary<string, string> Metadata)>();
                var lines = posts.Select(post => $"- [{post.Metadata.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(post.Path))}]({EncodeRelativePath(Path.GetRelativePath(blogDirectory, post.TargetPath))})" +
                                                  (post.Metadata.TryGetValue("date", out var date) ? $" — {date}" : string.Empty));
                var generatedBody = "# Blog" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, lines) + Environment.NewLine;
                updated[blogIndex] = ReplaceMarkdownBody(existing, generatedBody, blogIndex);
                changes++;
            }
        }

        var tagsPath = Path.Combine(rootPath, "Tags.md");
        if (File.Exists(tagsPath))
        {
            var existing = updated.GetValueOrDefault(tagsPath) ?? File.ReadAllText(tagsPath);
            if (existing.Contains("material/tags", StringComparison.OrdinalIgnoreCase))
            {
                var tagGroups = new SortedDictionary<string, List<(string RelativePath, string Title)>>(StringComparer.OrdinalIgnoreCase);
                foreach (var page in Directory.EnumerateFiles(rootPath, "*.md", SearchOption.AllDirectories))
                {
                    var metadata = ReadMetadata(page);
                    if (metadata.TryGetValue("tags", out var tags))
                    {
                        var fullPath = Path.GetFullPath(page);
                        var targetPath = movedPaths.GetValueOrDefault(fullPath, fullPath);
                        foreach (var tag in tags.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (!tagGroups.TryGetValue(tag, out var entries)) tagGroups[tag] = entries = new List<(string, string)>();
                            entries.Add((Path.GetRelativePath(rootPath, targetPath).Replace(Path.DirectorySeparatorChar, '/'),
                                metadata.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(page))));
                        }
                    }
                }

                var lines = new List<string> { "# Tags", string.Empty };
                foreach (var (tag, entries) in tagGroups)
                {
                    lines.Add($"## {tag}");
                    lines.AddRange(entries.OrderBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
                        .Select(entry => $"- [{entry.Title}]({EncodeRelativePath(entry.RelativePath)})"));
                    lines.Add(string.Empty);
                }
                updated[tagsPath] = ReplaceMarkdownBody(existing, string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine, tagsPath);
                changes++;
            }
        }
    }

    private static Dictionary<string, string> ReadMetadata(string path)
    {
        var content = File.ReadAllText(path);
        var frontMatter = FrontMatterBlock.Match(content);
        if (!frontMatter.Success) return new Dictionary<string, string>();
        var yaml = new YamlStream();
        try { yaml.Load(new StringReader(frontMatter.Groups["yaml"].Value)); }
        catch (YamlDotNet.Core.YamlException) { return new Dictionary<string, string>(); }
        if (yaml.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode mapping) return new Dictionary<string, string>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in mapping.Children)
        {
            if (key is not YamlScalarNode name) continue;
            if (value is YamlScalarNode scalar) values[name.Value ?? string.Empty] = scalar.Value ?? string.Empty;
            else if (name.Value == "tags" && value is YamlSequenceNode sequence)
                values["tags"] = string.Join('|', sequence.Children.OfType<YamlScalarNode>().Select(item => item.Value));
        }
        return values;
    }

    private static string RemoveSimpleFrontMatter(string content)
    {
        var match = FrontMatterBlock.Match(content);
        return match.Success ? content[match.Length..] : content;
    }

    private static bool StartsWithFrontMatter(string content)
    {
        var newline = content.IndexOfAny(['\r', '\n']);
        var firstLine = newline < 0 ? content : content[..newline];
        return firstLine.Trim() == "---";
    }

    private static string EncodeRelativePath(string path) => string.Join('/',
        path.Replace(Path.DirectorySeparatorChar, '/').Split('/').Select(Uri.EscapeDataString));

    private static PageMove CreateMove(string sourcePath)
    {
        var sectionDirectory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        var targetPath = Path.Combine(sectionDirectory.Parent!.FullName, $"{sectionDirectory.Name}.md");
        return new PageMove(sourcePath, targetPath);
    }

    private static IEnumerable<MarkdownFile> Flatten(IEnumerable<MarkdownFile> pages)
    {
        foreach (var page in pages)
        {
            yield return page;
            foreach (var child in Flatten(page.Children)) yield return child;
        }
    }

    private static bool IsMarkdownFile(string path) =>
        Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(path).Equals(".markdown", StringComparison.OrdinalIgnoreCase);

    private static string RewriteLinks(
        string content,
        string sourcePath,
        string newSourcePath,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer,
        out int changedLinkCount)
    {
        var pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();
        var document = global::Markdig.Markdown.Parse(content, pipeline);
        var edits = new Dictionary<(int Start, int Length), string>();

        foreach (var link in document.Descendants<LinkInline>())
            AddEdit(link.UrlSpan, link.Url);

        foreach (var definition in document.GetLinkReferenceDefinitions(true).OfType<LinkReferenceDefinition>())
            AddEdit(definition.UrlSpan, definition.Url);

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

            var newDestination = GetUpdatedDestination(
                sourcePath,
                newSourcePath,
                destination,
                movedPaths,
                pathComparer);
            if (newDestination is null) return;

            var original = content.Substring(span.Start, span.Length);
            var replacement = original.StartsWith('<') && original.EndsWith('>')
                ? $"<{newDestination}>"
                : newDestination;
            edits[(span.Start, span.Length)] = replacement;
        }
    }

    private static string? GetUpdatedDestination(
        string sourcePath,
        string newSourcePath,
        string destination,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer)
    {
        if (string.IsNullOrWhiteSpace(destination) ||
            destination.StartsWith("//", StringComparison.Ordinal) ||
            System.Text.RegularExpressions.Regex.IsMatch(destination, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
            return null;

        var suffixStart = destination.IndexOfAny(['?', '#']);
        var relativePath = suffixStart < 0 ? destination : destination[..suffixStart];
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        string targetPath;
        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath)) return null;

            targetPath = Path.GetFullPath(relativePath, Path.GetDirectoryName(sourcePath)!);
            if (Directory.Exists(targetPath))
            {
                var readmePath = Path.Combine(targetPath, "README.md");
                if (!File.Exists(readmePath)) return null;
                targetPath = readmePath;
            }
            else if (!File.Exists(targetPath) && !movedPaths.ContainsKey(targetPath))
            {
                return null;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return null;
        }

        var targetMoved = movedPaths.TryGetValue(targetPath, out var movedTargetPath);
        var finalTargetPath = targetMoved ? movedTargetPath! : targetPath;
        if (!targetMoved && pathComparer.Equals(sourcePath, newSourcePath)) return null;

        var relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(newSourcePath)!, finalTargetPath);
        var encodedTarget = string.Join('/', relativeTarget
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Select(Uri.EscapeDataString));
        var suffix = suffixStart < 0 ? string.Empty : destination[suffixStart..];
        return encodedTarget + suffix;
    }

    private sealed record PageMove(string SourcePath, string TargetPath);
}
