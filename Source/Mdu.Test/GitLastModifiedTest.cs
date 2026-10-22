using System.Diagnostics;
using Mdu.Service;

namespace Mdu.Test;

public class GitLastModifiedTest
{
    [Fact]
    public void ReadsTheLastCommitDateOfEachFileBelowTheFolder()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"mdu-git-test-{Guid.NewGuid():N}");
        var docsPath = Path.Combine(rootPath, "Docs");
        Directory.CreateDirectory(Path.Combine(docsPath, "Guide"));
        try
        {
            Git(rootPath, null, "init", "--quiet");
            File.WriteAllText(Path.Combine(docsPath, "README.md"), "# Home\n");
            File.WriteAllText(Path.Combine(docsPath, "Guide", "Setup.md"), "# Setup\n");
            File.WriteAllText(Path.Combine(rootPath, "outside.md"), "# Outside\n");
            Git(rootPath, "2026-01-02T10:00:00+01:00", "add", ".");
            Git(rootPath, "2026-01-02T10:00:00+01:00", "commit", "--quiet", "-m", "first");
            File.AppendAllText(Path.Combine(docsPath, "Guide", "Setup.md"), "More\n");
            Git(rootPath, "2026-03-04T08:30:00+01:00", "commit", "--quiet", "-am", "second");

            var dates = GitLastModified.Read(docsPath);

            Assert.Equal(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.FromHours(1)), dates["README.md"]);
            Assert.Equal(new DateTimeOffset(2026, 3, 4, 8, 30, 0, TimeSpan.FromHours(1)), dates["Guide/Setup.md"]);
            Assert.DoesNotContain(dates.Keys, key => key.Contains("outside", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ReturnsNoDatesOutsideAGitWorkTree()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"mdu-git-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        try
        {
            File.WriteAllText(Path.Combine(rootPath, "README.md"), "# Home\n");

            Assert.Empty(GitLastModified.Read(rootPath));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static void Git(string workingDirectory, string? date, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("user.name=MDU Test");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("user.email=mdu@example.com");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("commit.gpgsign=false");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("core.hooksPath=/dev/null");
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        if (date is not null)
        {
            startInfo.Environment["GIT_AUTHOR_DATE"] = date;
            startInfo.Environment["GIT_COMMITTER_DATE"] = date;
        }

        using var process = Process.Start(startInfo)!;
        var error = process.StandardError.ReadToEnd();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {error}");
    }
}
