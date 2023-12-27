using System.Reflection;

namespace Markdown.Document.Test.EmbeddedResources;

public static class Strings
{
    public static readonly string SimpleDocument = StringResource("Markdown.Document.Test.EmbeddedResources.Markdown.SimpleDocument.md");

    private static string StringResource(string name)
    {
        return Assembly
            .GetExecutingAssembly()
            .GetEmbeddedStringResource(name);
    }

}

