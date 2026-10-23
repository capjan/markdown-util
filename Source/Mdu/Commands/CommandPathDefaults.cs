namespace Mdu.Commands;

internal static class CommandPathDefaults
{
    public static string ResolveRootPath(string? configuredPath)
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
                "No ./docs folder found. Run 'mdu new .' to create a starter project, or specify a Markdown folder in the command."),
            _ => throw new InvalidOperationException(
                "Multiple folders named docs found. Specify the desired Markdown folder in the command.")
        };
    }

    public static string ResolveOutputPath(string? configuredPath) =>
        Path.GetFullPath(configuredPath ?? Path.Combine(Environment.CurrentDirectory, "site"));
}
