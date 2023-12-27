using System.Resources;
using Core.Resources;

namespace Markdown.Renderer.Test.Res;

public class Resources
{
    private static ResourceService R = ResourceService.FromType(typeof(Resources));

    public static string MarkdownWithCode =>
        R.GetStringByName("Markdown.Renderer.Test.Res.Samples.MarkdownWithCode.md");
}