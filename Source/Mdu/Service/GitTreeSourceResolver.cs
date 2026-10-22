using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Mdu.Service;

public interface IGitTreeSourceResolver
{
    ResolvedGitTreeSource Resolve(string sourceUrl);
}

public interface IGitCommandRunner
{
    string Run(string? workingDirectory, IReadOnlyList<string> arguments);
}

public sealed class GitTreeSourceResolver : IGitTreeSourceResolver
{
    private readonly IGitCommandRunner _git;
    private readonly string _cacheRoot;

    public GitTreeSourceResolver()
        : this(
            new GitCommandRunner(),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "mdu",
                "GitHubImports"))
    {
    }

    public GitTreeSourceResolver(IGitCommandRunner git, string cacheRoot)
    {
        _git = git;
        _cacheRoot = cacheRoot;
    }

    public ResolvedGitTreeSource Resolve(string sourceUrl)
    {
        var parsedSource = ParseSourceUrl(sourceUrl);
        var remoteRefs = ReadRemoteRefs(parsedSource.RepositoryUrl);
        RemoteRef? match;
        string sourcePath;
        var usedDefaultMainFallback = false;
        var requestedRefs = parsedSource.RequestedRefType is null
            ? remoteRefs
            : remoteRefs.Where(reference => reference.IsBranch == (parsedSource.RequestedRefType == RefType.Branch));

        if (string.IsNullOrWhiteSpace(parsedSource.RequestedTreePath))
        {
            match = parsedSource.RequestedRefType == RefType.Tag
                ? null
                : remoteRefs.FirstOrDefault(reference => reference.IsBranch && reference.Name == "main");
            sourcePath = string.Empty;
        }
        else
        {
            match = requestedRefs
                .Where(reference => parsedSource.RequestedTreePath == reference.Name ||
                                    parsedSource.RequestedTreePath.StartsWith(reference.Name + "/", StringComparison.Ordinal))
                .OrderByDescending(reference => reference.Name.Length)
                .ThenBy(reference => reference.IsBranch ? 0 : 1)
                .FirstOrDefault();

            if (match is null)
            {
                sourcePath = parsedSource.RequestedTreePath;
                if (parsedSource.RequestedRefType is null)
                {
                    match = remoteRefs.FirstOrDefault(reference => reference.IsBranch && reference.Name == "main");
                    usedDefaultMainFallback = match is not null;
                }
            }
            else
            {
                sourcePath = parsedSource.RequestedTreePath.Length == match.Name.Length
                    ? string.Empty
                    : parsedSource.RequestedTreePath[(match.Name.Length + 1)..];
            }
        }

        if (match is null)
        {
            if (parsedSource.RequestedRefType == RefType.Tag && string.IsNullOrEmpty(parsedSource.RequestedTreePath))
                throw new InvalidOperationException($"Imported source '{sourceUrl}' must include a tag revision in the tree or source URL.");
            if (parsedSource.RequestedRefType is { } requestedRefType)
                throw new InvalidOperationException($"Could not find a matching {requestedRefType.ToString().ToLowerInvariant()} in imported source '{sourceUrl}'. Include a valid revision before the folder path.");
            throw new InvalidOperationException($"Could not resolve a branch or tag in imported source '{sourceUrl}', and the default 'main' branch is unavailable.");
        }

        if (!string.IsNullOrWhiteSpace(sourcePath) && sourcePath.Split('/').Any(segment =>
                segment is "." or ".." || segment.Contains('\\') || Path.IsPathRooted(segment)))
            throw new InvalidOperationException($"Imported source '{sourceUrl}' must point to a folder inside the repository.");

        var repositoryPath = GetCachedRepository(parsedSource.RepositoryUrl, match.Name, match.Commit);
        var sourceFolder = string.IsNullOrWhiteSpace(sourcePath)
            ? repositoryPath
            : Path.GetFullPath(Path.Combine(repositoryPath, sourcePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsInsideDirectory(repositoryPath, sourceFolder) || !Directory.Exists(sourceFolder))
        {
            var fallbackHint = usedDefaultMainFallback
                ? " The URL did not match a branch or tag, so MDU checked this folder on the default 'main' branch; include a valid revision if you intended a branch or tag."
                : string.Empty;
            throw new InvalidOperationException($"The imported folder '{sourcePath}' does not exist in '{sourceUrl}'.{fallbackHint}");
        }

        return new ResolvedGitTreeSource(
            parsedSource.RepositoryUrl,
            parsedSource.WebRepositoryUrl,
            match.Name,
            match.IsBranch,
            sourcePath,
            repositoryPath,
            sourceFolder,
            BuildEditPageLinkPrefix(parsedSource, match));
    }

    private static ParsedSource ParseSourceUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Imported sources must be HTTPS Git repository or tree URLs without embedded credentials.");

        var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        for (var index = 2; index < path.Length - 1; index++)
        {
            if (path[index] == "-" && path[index + 1].Equals("tree", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(uri.Fragment))
                    throw new InvalidOperationException("Imported tree URLs must not contain a fragment.");
                return CreateTreeSource(uri, path, index, index + 2, EditLinkStyle.GitLab, ParseGitLabRefType(uri.Query));
            }

            if (path[index] == "-" && IsUnsupportedBrowseRoute(path[index + 1]))
            {
                throw new InvalidOperationException($"Imported source URL '{sourceUrl}' points to a GitLab page, not a repository tree. Use a /-/tree/<revision>/<folder> URL.");
            }
        }

        for (var index = 2; index < path.Length; index++)
        {
            if (!IsGitLabHost(uri) && path[index].Equals("tree", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(uri.Fragment))
                    throw new InvalidOperationException("Imported tree URLs must not contain a fragment.");
                return CreateTreeSource(uri, path, index, index + 1, EditLinkStyle.Tree, null);
            }

            if (!IsGitLabHost(uri) && path[index].Equals("src", StringComparison.OrdinalIgnoreCase))
            {
                var hasRefType = index + 1 < path.Length &&
                                 (path[index + 1].Equals("branch", StringComparison.OrdinalIgnoreCase) ||
                                  path[index + 1].Equals("tag", StringComparison.OrdinalIgnoreCase));
                if (!hasRefType && !(IsBitbucketHost(uri) && index == 2)) continue;
                if (!string.IsNullOrEmpty(uri.Fragment))
                    throw new InvalidOperationException("Imported source URLs must not contain a fragment.");
                RefType? sourceRefType = hasRefType
                    ? path[index + 1].Equals("branch", StringComparison.OrdinalIgnoreCase) ? RefType.Branch : RefType.Tag
                    : null;
                return CreateTreeSource(
                    uri,
                    path,
                    index,
                    index + (hasRefType ? 2 : 1),
                    hasRefType ? EditLinkStyle.SourceWithRefType : EditLinkStyle.Source,
                    sourceRefType);
            }

            if (index == 2 && !IsGitLabHost(uri) && IsUnsupportedBrowseRoute(path[index]))
                throw new InvalidOperationException($"Imported source URL '{sourceUrl}' points to a repository page, not a repository tree. Use a repository root or supported tree URL.");
        }

        var (repositoryUrl, webRepositoryUrl) = BuildRepositoryUrls(uri, path);
        var requestedTreePath = string.IsNullOrEmpty(uri.Fragment)
            ? string.Empty
            : Uri.UnescapeDataString(uri.Fragment[1..]).Trim('/');
        return new ParsedSource(repositoryUrl, webRepositoryUrl, requestedTreePath, EditLinkStyle.Tree, null);
    }

    private static ParsedSource CreateTreeSource(
        Uri uri,
        string[] path,
        int repositorySegmentCount,
        int contentStart,
        EditLinkStyle editLinkStyle,
        RefType? requestedRefType)
    {
        if (repositorySegmentCount < 2 || contentStart > path.Length)
            throw new InvalidOperationException("An imported source must identify a repository and may include a revision and folder path.");

        var repositorySegments = path.Take(repositorySegmentCount).ToArray();
        var (repositoryUrl, webRepositoryUrl) = BuildRepositoryUrls(uri, repositorySegments);
        var requestedTreePath = string.Join('/', path.Skip(contentStart));
        return new ParsedSource(repositoryUrl, webRepositoryUrl, requestedTreePath, editLinkStyle, requestedRefType);
    }

    private static (string RepositoryUrl, string WebRepositoryUrl) BuildRepositoryUrls(Uri uri, string[] repositorySegments)
    {
        if (repositorySegments.Length < 2)
            throw new InvalidOperationException("An imported source must identify a Git repository.");

        var webSegments = repositorySegments.ToArray();
        var repositoryName = webSegments[^1];
        if (repositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            webSegments[^1] = repositoryName[..^4];

        var gitSegments = webSegments.ToArray();
        gitSegments[^1] += ".git";
        var authority = uri.GetLeftPart(UriPartial.Authority);
        return (
            $"{authority}/{string.Join('/', gitSegments.Select(Uri.EscapeDataString))}",
            $"{authority}/{string.Join('/', webSegments.Select(Uri.EscapeDataString))}");
    }

    private static RefType? ParseGitLabRefType(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        RefType? requestedRefType = null;
        foreach (var parameter in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            var key = pair.Length == 2 ? Uri.UnescapeDataString(pair[0]) : string.Empty;
            if (!key.Equals("ref_type", StringComparison.OrdinalIgnoreCase)) continue;

            var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            var refType = value.ToLowerInvariant() switch
            {
                "heads" or "branches" => RefType.Branch,
                "tags" => RefType.Tag,
                _ => (RefType?)null
            };
            if (refType is null || requestedRefType is not null && requestedRefType != refType)
                throw new InvalidOperationException("GitLab tree URLs support ref_type=heads, ref_type=branches, or ref_type=tags.");
            requestedRefType = refType;
        }

        return requestedRefType;
    }

    private static bool IsGitLabHost(Uri uri) =>
        uri.Host.Equals("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Contains("gitlab", StringComparison.OrdinalIgnoreCase);

    private static bool IsBitbucketHost(Uri uri) =>
        uri.Host.Equals("bitbucket.org", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.EndsWith(".bitbucket.org", StringComparison.OrdinalIgnoreCase);

    private static bool IsUnsupportedBrowseRoute(string segment) => segment.ToLowerInvariant() is
        "blob" or "raw" or "issues" or "pull" or "pulls" or "pull-requests" or "merge_requests" or "commit" or "commits" or "branches" or "tags" or "releases" or "settings" or "actions" or "pipelines" or "wiki";

    private static string BuildEditPageLinkPrefix(ParsedSource source, RemoteRef reference)
    {
        var encodedReference = string.Join('/', reference.Name.Split('/').Select(Uri.EscapeDataString));
        return source.EditLinkStyle switch
        {
            EditLinkStyle.GitLab => $"{source.WebRepositoryUrl}/-/edit/{encodedReference}",
            EditLinkStyle.Source => $"{source.WebRepositoryUrl}/src/{encodedReference}",
            EditLinkStyle.SourceWithRefType => $"{source.WebRepositoryUrl}/src/{(reference.IsBranch ? "branch" : "tag")}/{encodedReference}",
            _ => $"{source.WebRepositoryUrl}/edit/{encodedReference}"
        };
    }

    private IReadOnlyList<RemoteRef> ReadRemoteRefs(string repositoryUrl)
    {
        var output = _git.Run(null, ["ls-remote", "--heads", "--tags", repositoryUrl]);
        var references = new List<RemoteRef>();
        var peeledTags = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', 2);
            if (parts.Length != 2) continue;

            var referenceName = parts[1].TrimEnd('\r');
            if (referenceName.StartsWith("refs/tags/", StringComparison.Ordinal) && referenceName.EndsWith("^{}", StringComparison.Ordinal))
            {
                peeledTags[referenceName[..^3]] = parts[0];
                continue;
            }

            var isBranch = referenceName.StartsWith("refs/heads/", StringComparison.Ordinal);
            var isTag = referenceName.StartsWith("refs/tags/", StringComparison.Ordinal);
            if (!isBranch && !isTag) continue;

            var name = referenceName[(isBranch ? "refs/heads/" : "refs/tags/").Length..];
            references.Add(new RemoteRef(name, parts[0], isBranch));
        }

        return references
            .Select(reference => reference.IsBranch
                ? reference
                : reference with { Commit = peeledTags.GetValueOrDefault($"refs/tags/{reference.Name}", reference.Commit) })
            .ToArray();
    }

    private string GetCachedRepository(string repositoryUrl, string referenceName, string commit)
    {
        var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{repositoryUrl}\n{commit}\nregular-files-v2"))).ToLowerInvariant();
        var repositoryPath = Path.Combine(_cacheRoot, cacheKey);
        if (Directory.Exists(repositoryPath))
        {
            try
            {
                if (_git.Run(repositoryPath, ["rev-parse", "HEAD"]).Trim().Equals(commit, StringComparison.OrdinalIgnoreCase))
                    return repositoryPath;
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (Directory.Exists(repositoryPath)) Directory.Delete(repositoryPath, recursive: true);
        Directory.CreateDirectory(_cacheRoot);

        var temporaryPath = repositoryPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            _git.Run(null, ["clone", "--depth", "1", "--no-checkout", "--branch", referenceName, repositoryUrl, temporaryPath]);
            _git.Run(temporaryPath, ["-c", "core.symlinks=false", "checkout", "--detach", commit]);
            Directory.Move(temporaryPath, repositoryPath);
        }
        finally
        {
            if (Directory.Exists(temporaryPath)) Directory.Delete(temporaryPath, recursive: true);
        }

        return repositoryPath;
    }

    private static bool IsInsideDirectory(string directory, string path)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return relativePath == "." ||
               !Path.IsPathRooted(relativePath) &&
               relativePath != ".." &&
               !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private sealed record RemoteRef(string Name, string Commit, bool IsBranch);

    private sealed record ParsedSource(
        string RepositoryUrl,
        string WebRepositoryUrl,
        string RequestedTreePath,
        EditLinkStyle EditLinkStyle,
        RefType? RequestedRefType);

    private enum RefType
    {
        Branch,
        Tag
    }

    private enum EditLinkStyle
    {
        Tree,
        GitLab,
        Source,
        SourceWithRefType
    }
}

internal sealed class GitCommandRunner : IGitCommandRunner
{
    private static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes(5);

    public string Run(string? workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start git.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(GitTimeout))
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException("Timed out while retrieving an imported Git source.");
            }

            Task.WaitAll(output, error);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Git could not retrieve an imported source: {error.Result.Trim()}");

            return output.Result;
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Git must be installed to import documentation from Git repositories.", exception);
        }
    }

}
public sealed record ResolvedGitTreeSource(
    string RepositoryUrl,
    string WebRepositoryUrl,
    string ReferenceName,
    bool IsBranch,
    string SourcePath,
    string RepositoryPath,
    string SourceFolderPath,
    string EditPageLinkPrefix);
