namespace Mdu.Commands;

public static class NewCommand
{
    private const string StarterPageContent = "# Documentation\n\nStart writing your documentation here.\n";

    public static int Execute(string? configuredDirectory)
    {
        string targetDirectory;
        var createdDirectories = new List<string>();
        string? createdPagePath = null;

        try
        {
            targetDirectory = Path.GetFullPath(configuredDirectory ?? ".");

            if (File.Exists(targetDirectory))
            {
                Console.Error.WriteLine($"The target path is a file: {targetDirectory}");
                return 1;
            }

            if (Directory.Exists(targetDirectory) && Directory.EnumerateDirectories(targetDirectory)
                .Any(path => Path.GetFileName(path).Equals("docs", StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine("A documentation folder already exists in the target directory.");
                return 1;
            }

            CreateDirectories(targetDirectory, createdDirectories);
            var docsDirectory = Path.Combine(targetDirectory, "docs");
            CreateDirectories(docsDirectory, createdDirectories);

            var pagePath = Path.Combine(docsDirectory, "README.md");
            using var stream = new FileStream(pagePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            createdPagePath = pagePath;
            using var writer = new StreamWriter(stream);
            writer.Write(StarterPageContent);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            RemovePartialProject(createdPagePath, createdDirectories);
            Console.Error.WriteLine($"Could not create the MDU project: {exception.Message}");
            return 1;
        }

        var relativePagePath = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(targetDirectory, "docs", "README.md"));
        Console.WriteLine($"Created {relativePagePath}.");
        var relativeTargetDirectory = Path.GetRelativePath(Environment.CurrentDirectory, targetDirectory);
        if (relativeTargetDirectory == ".")
        {
            Console.WriteLine("Next: mdu serve");
        }
        else
        {
            Console.WriteLine("Next:");
            Console.WriteLine($"  cd \"{relativeTargetDirectory}\"");
            Console.WriteLine("  mdu serve");
        }

        return 0;
    }

    private static void CreateDirectories(string path, ICollection<string> createdDirectories)
    {
        var missingDirectories = new Stack<string>();
        var currentPath = path;
        while (!Directory.Exists(currentPath))
        {
            missingDirectories.Push(currentPath);
            currentPath = Directory.GetParent(currentPath)?.FullName ?? string.Empty;
            if (currentPath.Length == 0) break;
        }

        while (missingDirectories.TryPop(out var directory))
        {
            Directory.CreateDirectory(directory);
            createdDirectories.Add(directory);
        }
    }

    private static void RemovePartialProject(string? createdPagePath, IEnumerable<string> createdDirectories)
    {
        if (createdPagePath is not null)
        {
            try
            {
                File.Delete(createdPagePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        foreach (var directory in createdDirectories.Reverse())
        {
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
