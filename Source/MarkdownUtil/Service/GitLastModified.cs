using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace MarkdownUtil.Service;

/// <summary>
/// Reads, from git, the date of the last commit that changed each file below a folder.
/// </summary>
public static class GitLastModified
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns the commit dates keyed by the file path relative to <paramref name="rootPath"/> with '/' separators.
    /// Returns no dates outside a git work tree and in a shallow clone, whose cut-off history would date
    /// every older file to the oldest fetched commit.
    /// </summary>
    public static IReadOnlyDictionary<string, DateTimeOffset> Read(string rootPath)
    {
        var dates = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        if (RunGit(rootPath, "rev-parse", "--is-shallow-repository")?.Trim() != "false") return dates;

        var log = RunGit(rootPath, "-c", "core.quotePath=false", "log", "--format=@%cI", "--name-only", "--relative", "--", ".");
        if (log is null) return dates;

        DateTimeOffset? commitDate = null;
        foreach (var line in log.Split('\n'))
        {
            if (line.StartsWith('@'))
            {
                commitDate = DateTimeOffset.TryParse(line[1..], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date
                    : null;
            }
            else if (line.Length > 0 && commitDate is not null)
            {
                // The log lists the newest commit first, so the first date seen for a file is its last change.
                dates.TryAdd(line, commitDate.Value);
            }
        }

        return dates;
    }

    private static string? RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(Timeout))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            Task.WaitAll(output, error);
            return process.ExitCode == 0 ? output.Result : null;
        }
        catch (Win32Exception)
        {
            // git is not installed.
            return null;
        }
    }
}
