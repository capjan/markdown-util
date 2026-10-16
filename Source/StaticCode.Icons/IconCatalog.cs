namespace StaticCode.Icons;

/// <summary>
/// Resolves icon shortcodes such as <c>material-account-circle</c> to the bundled icon sets.
/// Each set is loaded on first use.
/// </summary>
public static class IconCatalog
{
    public static IReadOnlyList<IconSet> Sets { get; } = new[]
    {
        new IconSet("material-", "mdi", "https://pictogrammers.com/library/mdi/"),
        new IconSet("fontawesome-brands-", "fa7-brands", "https://fontawesome.com/search?ic=brands"),
        new IconSet("fontawesome-regular-", "fa7-regular", "https://fontawesome.com/search?ic=free&s=regular"),
        new IconSet("fontawesome-solid-", "fa7-solid", "https://fontawesome.com/search?ic=free&s=solid"),
        new IconSet("octicons-", "octicon", "https://primer.style/octicons/"),
        new IconSet("simple-", "simple-icons", "https://simpleicons.org/"),
        new IconSet("dashboard-", "dashboard-icons", "https://dashboardicons.com/")
    };

    /// <summary>
    /// Finds the icon for a shortcode name without colons, for example <c>octicons-mark-github-16</c>.
    /// </summary>
    public static bool TryGetIcon(string shortcode, out Icon icon)
    {
        foreach (var set in Sets)
        {
            if (shortcode.Length > set.ShortcodePrefix.Length
                && shortcode.StartsWith(set.ShortcodePrefix, StringComparison.Ordinal)
                && set.TryGetIcon(shortcode[set.ShortcodePrefix.Length..], out icon))
                return true;
        }

        icon = null!;
        return false;
    }
}
