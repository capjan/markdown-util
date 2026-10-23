namespace Mdu.Commands;

internal static class NewCommand
{
    private const string StarterPageContent = "# Documentation\n\nStart writing your documentation here.\n";

    public static int Execute(string? configuredDirectory)
    {
        string targetDirectory;

        try
        {
            targetDirectory = Path.GetFullPath(configuredDirectory ?? ".");
            Directory.CreateDirectory(targetDirectory);

            if (Directory.EnumerateDirectories(targetDirectory)
                .Any(path => Path.GetFileName(path).Equals("docs", StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine("A documentation folder already exists in the target directory.");
                return 1;
            }

            var docsDirectory = Path.Combine(targetDirectory, "docs");
            Directory.CreateDirectory(docsDirectory);

            var pagePath = Path.Combine(docsDirectory, "README.md");
            using var stream = new FileStream(pagePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(StarterPageContent);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Could not create the MDU project: {exception.Message}");
            return 1;
        }

        var relativePagePath = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(targetDirectory, "docs", "README.md"));
        Console.WriteLine($"Created {relativePagePath}.");
        Console.WriteLine("Run 'mdu serve' from the project directory to preview your documentation.");
        return 0;
    }
}
