using Core.Resources;

namespace Markdown.Renderer;

public static class Resources
{
    private static readonly ResourceService R = ResourceService.FromType(typeof(Resources));
    public static string CssContent() => R.GetStringByName("Markdown.Renderer.Res.Css.main.css");
}