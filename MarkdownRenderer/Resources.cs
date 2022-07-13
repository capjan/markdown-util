using System.Reflection;

namespace MarkdownRenderer;

public static class Resources
{
    public static Stream MainCssStream()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = "MarkdownRenderer.Res.Css.main.css";
        return asm.GetManifestResourceStream(resourceName) ?? throw new KeyNotFoundException($"Failed to get a resource stream for: {resourceName}");
    }

    public static string[] GetManifestResourceNames()
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceNames();
    }
}