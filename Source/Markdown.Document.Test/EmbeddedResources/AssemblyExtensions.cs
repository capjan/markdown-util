using System;
using System.IO;
using System.Reflection;

namespace Markdown.Document.Test.EmbeddedResources;

internal static class AssemblyExtensions
{
    public static string GetEmbeddedStringResource(this Assembly assembly, string name)
    {
        if (assembly.GetManifestResourceStream(name) is not { } stream)
        {
            var validNames = string.Join("\n", assembly.GetManifestResourceNames());
            throw new InvalidOperationException($"failed to open a manifest stream for the resource name: {{name}}.\nValid Names are:\n{validNames}");
        }

        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }
}