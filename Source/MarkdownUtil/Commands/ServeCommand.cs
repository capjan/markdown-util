using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Core.Collections.NodeGraph.Extensions;
using Markdown.Renderer;
using Markdown.Search;
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
    private readonly SearchIndexBuilder _searchIndexBuilder;
    private readonly Dictionary<string, MarkdownFile> _pages = new(PathComparer);
    private readonly Dictionary<string, HashSet<string>> _assetsByPage = new(PathComparer);
    private readonly Dictionary<string, HashSet<string>> _pagesByAsset = new(PathComparer);
    private readonly HashSet<string> _generatedFiles = new(PathComparer);

    private MarkdownFile[] _navigationRoots = Array.Empty<MarkdownFile>();
    private IReadOnlyCollection<string> _siteMediaProviders = Array.Empty<string>();
    private RenderCommandSettings _settings = new();
    private string _rootPath = string.Empty;
    private string _outputPath = string.Empty;

    public ServeCommand(
        GraphBuilder graphBuilder,
        DataPreparationVisitor dataPreparationVisitor,
        HtmlRenderer renderer,
        SearchIndexBuilder searchIndexBuilder)
    {
        _graphBuilder = graphBuilder;
        _dataPreparationVisitor = dataPreparationVisitor;
        _renderer = renderer;
        _searchIndexBuilder = searchIndexBuilder;
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
            _rootPath = CommandPathDefaults.ResolveRootPath(settings.RootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        _outputPath = CommandPathDefaults.ResolveOutputPath(settings.OutputPath);
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
            var serverTask = server.RunAsync(cancellation.Token);
            var changesTask = ProcessChangesAsync(changes.Reader, server, cancellation.Token);
            await Task.WhenAny(serverTask, changesTask);
            var stoppedByUser = cancellation.IsCancellationRequested;
            if (!stoppedByUser) cancellation.Cancel();

            await Task.WhenAll(serverTask, changesTask);
            if (!stoppedByUser)
            {
                Console.Error.WriteLine("The preview server stopped unexpectedly.");
                return 1;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is HttpListenerException or SocketException or IOException or ObjectDisposedException or InvalidOperationException)
        {
            Console.Error.WriteLine($"The preview server stopped: {exception.Message}");
            return 1;
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
        SearchClientAsset.WriteTo(_outputPath);
        _searchIndexBuilder.Clear();
        graph.Visit(new LocalAssetsCopy(_rootPath, _outputPath));
        var rendererVisitor = new MarkdigVisitor(
            _rootPath,
            _outputPath,
            _renderer,
            _settings.EditPageRoot,
            _searchIndexBuilder,
            _navigationRoots);
        _siteMediaProviders = rendererVisitor.SiteMediaProviders;
        graph.Visit(rendererVisitor);
        _searchIndexBuilder.WriteIndex(_outputPath);

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
            await Task.Delay(TimeSpan.FromMilliseconds(75), cancellationToken);
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

        // Editors that save through a temporary file report creations, renames, and deletions for a page whose
        // content only changed. Only the state after the batch decides whether pages were added or removed.
        var requiresRebuild = relevantChanges.Any(change =>
                change.Kind == FileChangeKind.Rescan ||
                change.Kind is (FileChangeKind.Created or FileChangeKind.Deleted or FileChangeKind.Renamed) &&
                IsDirectoryChange(change)) ||
            relevantChanges
                .Where(change => change.Kind is FileChangeKind.Created or FileChangeKind.Deleted or FileChangeKind.Renamed)
                .SelectMany(change => change.OldPath is null ? new[] { change.Path } : new[] { change.Path, change.OldPath })
                .Distinct(PathComparer)
                .Any(ChangesPageSet);

        if (requiresRebuild)
        {
            RebuildSite();
            server.Publish(reloadAll: true);
            return;
        }

        var changedPages = relevantChanges
            .Where(change => change.Kind is FileChangeKind.Changed or FileChangeKind.Created or FileChangeKind.Renamed &&
                             _pages.ContainsKey(change.Path) && File.Exists(change.Path))
            .Select(change => change.Path)
            .Distinct(PathComparer)
            .ToArray();

        var affectedPageUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pagePath in changedPages)
        {
            var page = _pages[pagePath];
            var depth = page.CountDepth();
            var previousTitle = page.Title;
            var previousSidebarOrder = page.SidebarOrder;
            var previousSidebarVisible = page.SidebarVisible;
            var previousSidebarExpanded = page.SidebarExpanded;
            _dataPreparationVisitor.Process(page, depth);
            if (!previousTitle.Equals(page.Title, StringComparison.Ordinal) ||
                previousSidebarOrder != page.SidebarOrder ||
                previousSidebarVisible != page.SidebarVisible ||
                previousSidebarExpanded != page.SidebarExpanded)
            {
                RebuildSite();
                server.Publish(reloadAll: true);
                return;
            }

            UpdatePageAssets(pagePath, page);
            new LocalAssetsCopy(_rootPath, _outputPath).Process(page, depth);
            var rendererVisitor = new MarkdigVisitor(
                _rootPath,
                _outputPath,
                _renderer,
                _settings.EditPageRoot,
                _searchIndexBuilder,
                _navigationRoots);
            if (!new HashSet<string>(_siteMediaProviders, StringComparer.Ordinal).SetEquals(rendererVisitor.SiteMediaProviders))
            {
                RebuildSite();
                server.Publish(reloadAll: true);
                return;
            }

            rendererVisitor.Process(page, depth);
            affectedPageUrls.Add(PreviewServer.ToUrlPath(_outputPath, GetPageOutputPath(page)));
        }

        if (changedPages.Length > 0)
            _searchIndexBuilder.WriteIndex(_outputPath);

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

    private bool ChangesPageSet(string path) =>
        _pages.ContainsKey(path) ? !File.Exists(path) : File.Exists(path) && IsMarkdownCandidate(path);

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
            Path.Combine(_outputPath, "css", "main.css"),
            Path.Combine(_outputPath, "js", "search.js"),
            Path.Combine(_outputPath, "search-index.json")
        };
        currentFiles.UnionWith(_renderer.IconSearchAssets);

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
