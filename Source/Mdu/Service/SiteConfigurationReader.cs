using Markdown.Renderer.Res.Model;
using YamlDotNet.RepresentationModel;

namespace Mdu.Service;

public static class SiteConfigurationReader
{
    public static SiteConfiguration Read(string rootPath)
    {
        var path = Path.Combine(rootPath, "mdu.yml");
        if (!File.Exists(path)) return new SiteConfiguration();

        var stream = new YamlStream();
        stream.Load(new StringReader(File.ReadAllText(path)));
        if (stream.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode root) return new SiteConfiguration();

        var configuration = new SiteConfiguration
        {
            SiteName = ReadScalar(root, "site_name") ?? "Documentation",
            LogoLight = ReadAsset(root, "logo", "light"),
            LogoDark = ReadAsset(root, "logo", "dark"),
            FaviconLight = ReadAsset(root, "favicon", "light"),
            FaviconDark = ReadAsset(root, "favicon", "dark"),
            Light = ReadPalette(root, "light"),
            Dark = ReadPalette(root, "dark")
        };
        configuration.LogoDark ??= configuration.LogoLight;
        if (configuration.FaviconLight is null && configuration.FaviconDark is null)
        {
            configuration.FaviconLight = configuration.LogoLight;
            configuration.FaviconDark = configuration.LogoDark;
        }
        else
        {
            configuration.FaviconLight ??= configuration.LogoLight;
        }
        configuration.FaviconDark ??= configuration.FaviconLight;
        return configuration;
    }

    public static void CopyAssets(string rootPath, string outputPath, SiteConfiguration configuration)
    {
        foreach (var asset in new[] { configuration.LogoLight, configuration.LogoDark, configuration.FaviconLight, configuration.FaviconDark }
                     .Where(asset => !string.IsNullOrWhiteSpace(asset)).Distinct(StringComparer.Ordinal))
        {
            if (Uri.TryCreate(asset, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") continue;
            var source = Path.GetFullPath(asset!, rootPath);
            var relative = Path.GetRelativePath(rootPath, source);
            if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
            if (!File.Exists(source)) continue;
            var target = Path.Combine(outputPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, true);
        }
    }

    private static SitePalette? ReadPalette(YamlMappingNode root, string name)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("colors"), out var colorsNode) || colorsNode is not YamlMappingNode colors ||
            !colors.Children.TryGetValue(new YamlScalarNode(name), out var paletteNode) || paletteNode is not YamlMappingNode palette)
            return null;
        return new SitePalette
        {
            Background = ReadScalar(palette, "background"),
            Text = ReadScalar(palette, "text"),
            Primary = ReadScalar(palette, "primary")
        };
    }

    private static string? ReadAsset(YamlMappingNode root, string key, string theme)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(key), out var node)) return null;
        if (node is YamlScalarNode scalar) return scalar.Value;
        if (node is YamlMappingNode mapping) return ReadScalar(mapping, theme);
        return null;
    }

    private static string? ReadScalar(YamlMappingNode mapping, string key) =>
        mapping.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlScalarNode scalar
            ? scalar.Value
            : null;
}
