using Markdown.Renderer.Util;

namespace Markdown.Renderer;

public class LinkReplacer
{
    private readonly InternalLinkRegex _internalLinkRegex = new InternalLinkRegex();
    
    public string ReplaceAll(string input)
    {
        return _internalLinkRegex.Replace(input, match =>
        {
            var fileNameWithoutExtension = match.FileNameWithoutExtension;
            if (match.FileNameWithoutExtension.Equals("readme", StringComparison.InvariantCultureIgnoreCase))
            {
                fileNameWithoutExtension = "index";
            }
            return $"[{match.LinkName}]({match.Path}{fileNameWithoutExtension}.html)";
        });
    }
}