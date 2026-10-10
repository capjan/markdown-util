namespace MarkdownUtil.Commands;

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
                "No ./docs or ./Docs folder found. Pass the Markdown root folder as the first argument."),
            _ => throw new InvalidOperationException(
                "Both ./docs and ./Docs folders exist. Pass the desired Markdown root folder explicitly.")
        };
    }

    public static string ResolveOutputPath(string? configuredPath) =>
        Path.GetFullPath(configuredPath ?? Path.Combine(Environment.CurrentDirectory, "site"));
}
