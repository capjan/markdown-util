using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using Core.Collections.NodeGraph.Extensions;
using Markdown.Renderer;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public sealed class ServeCommand
{
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private readonly GraphBuilder _graphBuilder;
    private readonly DataPreparationVisitor _dataPreparationVisitor;
    private readonly HtmlRenderer _renderer;
    private readonly Dictionary<string, MarkdownFile> _pages = new(PathComparer);
    private readonly Dictionary<string, HashSet<string>> _assetsByPage = new(PathComparer);
    private readonly Dictionary<string, HashSet<string>> _pagesByAsset = new(PathComparer);
    private readonly HashSet<string> _generatedFiles = new(PathComparer);

    private MarkdownFile[] _navigationRoots = Array.Empty<MarkdownFile>();
    private RenderCommandSettings _settings = new();
    private string _rootPath = string.Empty;
    private string _outputPath = string.Empty;

    public ServeCommand(
        GraphBuilder graphBuilder,
        DataPreparationVisitor dataPreparationVisitor,
        HtmlRenderer renderer)
    {
        _graphBuilder = graphBuilder;
        _dataPreparationVisitor = dataPreparationVisitor;
        _renderer = renderer;
    }

    public int Execute(RenderCommandSettings settings, int port, bool noBrowser) =>
        ExecuteAsync(settings, port, noBrowser).GetAwaiter().GetResult();

    private async Task<int> ExecuteAsync(RenderCommandSettings settings, int port, bool noBrowser)
    {
        if (port is < 1 or > 65535)
        {
            Console.Error.WriteLine("Port must be between 1 and 65535.");
            return 1;
        }

        try
        {
            _rootPath = ResolveRootPath(settings.RootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        _outputPath = Path.GetFullPath(settings.OutputPath ?? Path.Combine(Environment.CurrentDirectory, "site"));
        if (PathComparer.Equals(_rootPath, _outputPath))
        {
            Console.Error.WriteLine("The output folder must be different from the Markdown root folder.");
            return 1;
        }
        if (IsInsideDirectory(_outputPath, _rootPath))
        {
            Console.Error.WriteLine("The output folder cannot contain the Markdown root folder.");
            return 1;
        }

        _settings = new RenderCommandSettings
        {
            RootPath = _rootPath,
            OutputPath = _outputPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden,
            EditPageRoot = settings.EditPageRoot
        };

        try
        {
            RebuildSite();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Could not render the site: {exception.Message}");
            return 1;
        }

        using var server = new PreviewServer(_outputPath, port);
        try
        {
            server.Start();
        }
        catch (HttpListenerException exception)
        {
            Console.Error.WriteLine($"Could not start the preview server on port {port}: {exception.Message}");
            return 1;
        }

        var url = server.BaseUrl;
        Console.WriteLine($"Serving {_rootPath} at {url} (press Ctrl+C to stop)");
        var changes = Channel.CreateUnbounded<FileChange>();
        using var watcher = CreateWatcher(changes.Writer);
        if (!noBrowser) OpenBrowser(url);

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            await Task.WhenAll(
                server.RunAsync(cancellation.Token),
                ProcessChangesAsync(changes.Reader, server, cancellation.Token));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }

    private void RebuildSite()
    {
        var previousFiles = new HashSet<string>(_generatedFiles, PathComparer);
        var graph = _graphBuilder.CreateGraph(_settings);
        _navigationRoots = graph;

        _renderer.PrepareBasePath(_outputPath);
        graph.Visit(new LocalAssetsCopy(_rootPath, _outputPath));
        graph.Visit(new MarkdigVisitor(_rootPath, _outputPath, _renderer, _settings.EditPageRoot, _navigationRoots));

        RebuildDependencyIndex(graph);
        ReconcileGeneratedFiles(previousFiles);
        Console.WriteLine("Site updated.");
    }

    private void RebuildDependencyIndex(IEnumerable<MarkdownFile> graph)
    {
        _pages.Clear();
        _assetsByPage.Clear();
        _pagesByAsset.Clear();

        foreach (var page in Flatten(graph))
        {
            var pagePath = Path.GetFullPath(page.FileInfo.FullName);
            _pages[pagePath] = page;
            UpdatePageAssets(pagePath, page);
        }
    }

    private static IEnumerable<MarkdownFile> Flatten(IEnumerable<MarkdownFile> pages)
    {
        foreach (var page in pages)
        {
            yield return page;
            foreach (var child in Flatten(page.Children))
                yield return child;
        }
    }

    private void UpdatePageAssets(string pagePath, MarkdownFile page)
    {
        RemovePageAssets(pagePath);

        var pageAssets = new HashSet<string>(PathComparer);
        var markdown = File.ReadAllText(pagePath);
        foreach (var assetPath in LocalAssetsCopy.GetLinkedAssetPaths(
                     markdown,
                     page.FileInfo.DirectoryName ?? _rootPath))
        {
            pageAssets.Add(assetPath);
            if (!_pagesByAsset.TryGetValue(assetPath, out var dependentPages))
            {
                dependentPages = new HashSet<string>(PathComparer);
                _pagesByAsset.Add(assetPath, dependentPages);
            }

            dependentPages.Add(pagePath);
        }

        _assetsByPage[pagePath] = pageAssets;
    }

    private void RemovePageAssets(string pagePath)
    {
        if (!_assetsByPage.Remove(pagePath, out var previousAssets)) return;

        foreach (var assetPath in previousAssets)
        {
            if (!_pagesByAsset.TryGetValue(assetPath, out var dependentPages)) continue;
            dependentPages.Remove(pagePath);
            if (dependentPages.Count == 0) _pagesByAsset.Remove(assetPath);
        }
    }

    private async Task ProcessChangesAsync(
        ChannelReader<FileChange> changes,
        PreviewServer server,
        CancellationToken cancellationToken)
    {
        while (await changes.WaitToReadAsync(cancellationToken))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
            var batch = new List<FileChange>();
            while (changes.TryRead(out var change)) batch.Add(change);

            try
            {
                ProcessChangeBatch(batch, server);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
            {
                Console.Error.WriteLine($"Could not update the site: {exception.Message}");
            }
        }
    }

    private void ProcessChangeBatch(IReadOnlyCollection<FileChange> changes, PreviewServer server)
    {
        var relevantChanges = changes
            .Where(change => change.Kind == FileChangeKind.Rescan ||
                             !IsGeneratedOutputPath(change.Path) &&
                             (change.OldPath is null || !IsGeneratedOutputPath(change.OldPath)))
            .ToArray();
        if (relevantChanges.Length == 0) return;

        var requiresRebuild = relevantChanges.Any(change =>
            change.Kind == FileChangeKind.Rescan ||
            change.Kind is (FileChangeKind.Created or FileChangeKind.Deleted or FileChangeKind.Renamed) &&
            (IsDirectoryChange(change) || IsMarkdownCandidate(change.Path) ||
             change.OldPath is not null && IsMarkdownCandidate(change.OldPath)));

        if (requiresRebuild)
        {
            RebuildSite();
            server.Publish(reloadAll: true);
            return;
        }

        var changedPages = relevantChanges
            .Where(change => change.Kind == FileChangeKind.Changed && _pages.ContainsKey(change.Path))
            .Select(change => change.Path)
            .Distinct(PathComparer)
            .ToArray();

        var affectedPageUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pagePath in changedPages)
        {
            var page = _pages[pagePath];
            var depth = page.CountDepth();
            var previousTitle = page.Title;
            _dataPreparationVisitor.Process(page, depth);
            if (!previousTitle.Equals(page.Title, StringComparison.Ordinal))
            {
                RebuildSite();
                server.Publish(reloadAll: true);
                return;
            }

            UpdatePageAssets(pagePath, page);
            new LocalAssetsCopy(_rootPath, _outputPath).Process(page, depth);
            new MarkdigVisitor(_rootPath, _outputPath, _renderer, _settings.EditPageRoot, _navigationRoots).Process(page, depth);
            affectedPageUrls.Add(PreviewServer.ToUrlPath(_outputPath, GetPageOutputPath(page)));
        }

        var assetChanges = relevantChanges
            .Where(change => change.Kind is FileChangeKind.Changed or FileChangeKind.Created or FileChangeKind.Deleted or FileChangeKind.Renamed)
            .SelectMany(change => change.OldPath is null ? new[] { change.Path } : new[] { change.Path, change.OldPath })
            .Distinct(PathComparer)
            .Where(_pagesByAsset.ContainsKey)
            .ToArray();

        foreach (var assetPath in assetChanges)
        {
            var dependentPages = _pagesByAsset[assetPath];
            foreach (var pagePath in dependentPages)
            {
                if (_pages.TryGetValue(pagePath, out var page))
                    affectedPageUrls.Add(PreviewServer.ToUrlPath(_outputPath, GetPageOutputPath(page)));
            }

            CopyOrRemoveAsset(assetPath);
        }

        ReconcileGeneratedFiles(new HashSet<string>(_generatedFiles, PathComparer));
        if (affectedPageUrls.Count > 0)
        {
            Console.WriteLine($"Updated {affectedPageUrls.Count} page(s).");
            server.Publish(affectedPageUrls);
        }
    }

    private bool IsMarkdownCandidate(string path)
    {
        if (_pages.ContainsKey(path)) return true;
        if (Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase)) return true;

        if (!File.Exists(path)) return false;
        var pattern = _settings.SearchPattern ?? Default.SearchPattern;
        var directory = Path.GetDirectoryName(path);
        return directory is not null && Directory.GetFiles(directory, pattern)
            .Any(file => PathComparer.Equals(Path.GetFullPath(file), path));
    }

    private static bool IsDirectoryChange(FileChange change) =>
        Directory.Exists(change.Path) ||
        change.OldPath is not null && !Path.HasExtension(change.Path) && !Path.HasExtension(change.OldPath);

    private void CopyOrRemoveAsset(string assetPath)
    {
        var source = Path.GetFullPath(assetPath);
        if (!IsInsideDirectory(_rootPath, source)) return;

        var output = GetAssetOutputPath(source);
        if (!File.Exists(source))
        {
            if (File.Exists(output)) File.Delete(output);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var temporaryPath = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.Copy(source, temporaryPath);
            File.Move(temporaryPath, output, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private void ReconcileGeneratedFiles(HashSet<string> previousFiles)
    {
        var currentFiles = new HashSet<string>(PathComparer)
        {
            Path.Combine(_outputPath, "css", "main.css")
        };

        foreach (var page in _pages.Values)
            currentFiles.Add(GetPageOutputPath(page));

        foreach (var assetPath in _pagesByAsset.Keys)
        {
            if (File.Exists(assetPath) && IsInsideDirectory(_rootPath, assetPath))
                currentFiles.Add(GetAssetOutputPath(assetPath));
        }

        foreach (var staleFile in previousFiles.Except(currentFiles, PathComparer))
        {
            if (File.Exists(staleFile)) File.Delete(staleFile);
        }

        _generatedFiles.Clear();
        _generatedFiles.UnionWith(currentFiles);
    }

    private string GetPageOutputPath(MarkdownFile page)
    {
        var pathInfo = FilePathInfo.CreateRenderFileInfo(_rootPath, _outputPath, page.FileInfo, ".html");
        var outputPath = pathInfo.OutputFullFilePath;
        if (outputPath.EndsWith("readme.html", StringComparison.OrdinalIgnoreCase))
            outputPath = outputPath[..^"readme.html".Length] + "index.html";
        return Path.GetFullPath(outputPath);
    }

    private string GetAssetOutputPath(string assetPath)
    {
        var relativePath = Path.GetRelativePath(_rootPath, assetPath);
        return Path.GetFullPath(Path.Combine(_outputPath, relativePath));
    }

    private FileSystemWatcher CreateWatcher(ChannelWriter<FileChange> writer)
    {
        var watcher = new FileSystemWatcher(_rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                           NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = false
        };

        watcher.Changed += (_, eventArgs) => writer.TryWrite(new(eventArgs.FullPath, null, FileChangeKind.Changed));
        watcher.Created += (_, eventArgs) => writer.TryWrite(new(eventArgs.FullPath, null, FileChangeKind.Created));
        watcher.Deleted += (_, eventArgs) => writer.TryWrite(new(eventArgs.FullPath, null, FileChangeKind.Deleted));
        watcher.Renamed += (_, eventArgs) => writer.TryWrite(new(eventArgs.FullPath, eventArgs.OldFullPath, FileChangeKind.Renamed));
        watcher.Error += (_, _) => writer.TryWrite(new(_rootPath, null, FileChangeKind.Rescan));
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private static bool IsInsideDirectory(string directory, string path)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return relativePath == "." ||
               !Path.IsPathRooted(relativePath) &&
               relativePath != ".." &&
               !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private bool IsGeneratedOutputPath(string path) =>
        IsInsideDirectory(_rootPath, _outputPath) && IsInsideDirectory(_outputPath, path);

    private static string ResolveRootPath(string? configuredPath)
    {
        if (configuredPath is not null)
        {
            var path = Path.GetFullPath(configuredPath);
            if (!Directory.Exists(path))
                throw new InvalidOperationException($"Markdown folder not found: {path}");
            return Path.TrimEndingDirectorySeparator(path);
        }

        var candidates = Directory.EnumerateDirectories(Environment.CurrentDirectory)
            .Where(path => Path.GetFileName(path).Equals("docs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return candidates.Length switch
        {
            1 => Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidates[0])),
            0 => throw new InvalidOperationException(
                "No ./docs or ./Docs folder found. Pass the Markdown root folder as the first argument."),
            _ => throw new InvalidOperationException(
                "Both ./docs and ./Docs folders exist. Pass the desired Markdown root folder explicitly.")
        };
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            Console.Error.WriteLine($"Could not open a browser automatically. Open {url}");
        }
    }

    private sealed record FileChange(string Path, string? OldPath, FileChangeKind Kind);

    private enum FileChangeKind
    {
        Changed,
        Created,
        Deleted,
        Renamed,
        Rescan
    }
}
