using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace MarkdownUtil.Service;

public interface IGitHubTreeSourceResolver
{
    ResolvedGitHubTreeSource Resolve(string sourceUrl);
}

public interface IGitCommandRunner
{
    string Run(string? workingDirectory, IReadOnlyList<string> arguments);
}

public sealed class GitHubTreeSourceResolver : IGitHubTreeSourceResolver
{
    private readonly IGitCommandRunner _git;
    private readonly string _cacheRoot;

    public GitHubTreeSourceResolver()
        : this(
            new GitCommandRunner(),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MarkdownUtil",
                "GitHubImports"))
    {
    }

    public GitHubTreeSourceResolver(IGitCommandRunner git, string cacheRoot)
    {
        _git = git;
        _cacheRoot = cacheRoot;
    }

    public ResolvedGitHubTreeSource Resolve(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Imported sources must be GitHub HTTPS tree URLs.");

        var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        if (path.Length < 5 || !path[2].Equals("tree", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("An imported source must use the form https://github.com/owner/repository/tree/ref/path.");

        var owner = path[0];
        var repository = path[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? path[1][..^4]
            : path[1];
        var repositoryUrl = $"https://github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}.git";
        var webRepositoryUrl = $"https://github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}";
        var requestedTreePath = string.Join('/', path.Skip(3));
        var remoteRefs = ReadRemoteRefs(repositoryUrl);
        var match = remoteRefs
            .Where(reference => requestedTreePath.StartsWith(reference.Name + "/", StringComparison.Ordinal))
            .OrderByDescending(reference => reference.Name.Length)
            .ThenBy(reference => reference.IsBranch ? 0 : 1)
            .FirstOrDefault();
        if (match is null)
            throw new InvalidOperationException($"Could not resolve a branch or tag in imported source '{sourceUrl}'.");

        var sourcePath = requestedTreePath[(match.Name.Length + 1)..];
        if (string.IsNullOrWhiteSpace(sourcePath) || sourcePath.Split('/').Any(segment =>
                segment is "." or ".." || segment.Contains('\\') || Path.IsPathRooted(segment)))
            throw new InvalidOperationException($"Imported source '{sourceUrl}' must point to a folder inside the repository.");

        var repositoryPath = GetCachedRepository(repositoryUrl, match.Name, match.Commit);
        var sourceFolder = Path.GetFullPath(Path.Combine(repositoryPath, sourcePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsInsideDirectory(repositoryPath, sourceFolder) || !Directory.Exists(sourceFolder))
            throw new InvalidOperationException($"The imported folder '{sourcePath}' does not exist in '{sourceUrl}'.");

        return new ResolvedGitHubTreeSource(
            repositoryUrl,
            webRepositoryUrl,
            match.Name,
            sourcePath,
            repositoryPath,
            sourceFolder);
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
                throw new InvalidOperationException("Timed out while retrieving an imported GitHub source.");
            }

            Task.WaitAll(output, error);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Git could not retrieve an imported source: {error.Result.Trim()}");

            return output.Result;
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Git must be installed to import GitHub documentation.", exception);
        }
    }

}
public sealed record ResolvedGitHubTreeSource(
    string RepositoryUrl,
    string WebRepositoryUrl,
    string ReferenceName,
    string SourcePath,
    string RepositoryPath,
    string SourceFolderPath);
