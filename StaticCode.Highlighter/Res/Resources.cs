using Core.Resources;

namespace StaticCode.Highlighter.Res;

public static class Resources
{
    private static readonly ResourceService R = ResourceService.FromType(typeof(Resources));
    
    public static string CssContent() => R.GetStringByName("StaticCode.Highlighter.Res.Css.Highlight.css");
}