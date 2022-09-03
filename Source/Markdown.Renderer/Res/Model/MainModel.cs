namespace Markdown.Renderer.Res.Model;

public class MainModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RenderedMarkdown { get; set; } = string.Empty;
    
    /// <summary>
    /// Hyperlink to the website that allows editing the current site
    /// </summary>
    public string EditPageLink { get; set; } = string.Empty;
    public string CssLink { get; set; } = string.Empty;
}