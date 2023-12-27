namespace Markdown.Renderer.Util;

public struct MarkdownLinkInfo
{
    /// <summary>
    /// Full Match of the Markdown Link
    /// </summary>
    public string FullMatch { get; init; }
    
    /// <summary>
    /// Name of the Link
    /// </summary>
    public string LinkName { get; init; }
    
    /// <summary>
    /// Path of the Link. e.g. ./folder/ or ../
    /// </summary>
    public string Path { get; init; }
    
    /// <summary>
    /// Filename of the Link target with extension
    /// </summary>
    public string FileName { get; init; }
    
    /// <summary>
    /// Filename without extension
    /// </summary>
    public string FileNameWithoutExtension { get; init; }
    
    /// <summary>
    /// Extension of the Target
    /// </summary>
    public string Extension { get; init; }
}