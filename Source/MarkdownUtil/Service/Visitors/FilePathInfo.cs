namespace MarkdownUtil.Service.Visitors;

public struct FilePathInfo
{
    /// <summary>
    /// Full path to the root directory that contains the files to be processed
    /// </summary>
    public string RootDirectoryPath { get; private init; }
    
    /// <summary>
    /// Full file path to the markdown file that is currently visited
    /// </summary>
    public string InputFullFilePath { get; private init; }
    
    /// <summary>
    /// Relative path to the file from the root directory
    /// </summary>
    public string InputRelativeFilePathToRootDirectory { get; private init; }
    
    /// <summary>
    /// Full path to the directory of the processed markdown file
    /// </summary>
    public string InputFullDirectoryPath { get; private init; }
    
    /// <summary>
    /// Full path to the root directory of the output
    /// </summary>
    public string OutputRootDirectoryPath { get; private init; }
    
    /// <summary>
    /// Full output path of the file (the copy destination)
    /// </summary>
    public string OutputFullFilePath { get; private init; }
    
    /// <summary>
    /// Relative path to the file from the output directory
    /// </summary>
    public string OutputRelativeFilePathToRootDirectory { get; private init; }
    
    /// <summary>
    /// Full output path to the directory (the directory of the copy destination) 
    /// </summary>
    public string OutputFullDirectoryPath { get; private init; }
    
    public static FilePathInfo CreateRenderFileInfo(
        string rootDirectoryPath,
        string outputDirectoryPath,
        FileInfo fileInfo,
        string? outputFileExtension = default,
        string? siteRelativePath = null)
    {
        var fullFilePath = fileInfo.FullName;
        var fullDirectoryPath = fileInfo.DirectoryName ?? throw new InvalidOperationException();

        var relativeFilePathToRoot = siteRelativePath ?? Path.GetRelativePath(rootDirectoryPath, fullFilePath);
        if (Path.IsPathRooted(relativeFilePathToRoot) || relativeFilePathToRoot == ".." ||
            relativeFilePathToRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativeFilePathToRoot.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException($"File path '{relativeFilePathToRoot}' must stay inside the documentation root.");

        var relativeFolderPathToRoot = Path.GetDirectoryName(relativeFilePathToRoot) ?? string.Empty;

        var outputFullFilePath = Path.Combine(outputDirectoryPath, relativeFilePathToRoot);
        
        if (outputFileExtension is not null)
        {
            outputFullFilePath = Path.ChangeExtension(outputFullFilePath, outputFileExtension);
        }
        var outputFullDirectoryPath = Path.Combine(outputDirectoryPath, relativeFolderPathToRoot);
        var outputRelativeFilePathToRootDirectory = Path.GetRelativePath(outputDirectoryPath, outputFullFilePath);
        
        return new FilePathInfo
        {
            RootDirectoryPath = rootDirectoryPath,
            InputFullFilePath = fullFilePath,
            InputFullDirectoryPath = fullDirectoryPath,
            InputRelativeFilePathToRootDirectory = relativeFilePathToRoot,
            OutputRootDirectoryPath = outputDirectoryPath,
            OutputFullFilePath = outputFullFilePath,
            OutputFullDirectoryPath = outputFullDirectoryPath,
            OutputRelativeFilePathToRootDirectory = outputRelativeFilePathToRootDirectory
        };
    }
    
}

public static class PathInfoExtensions
{
    /// <summary>
    /// returns the content of the input file as string
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static string ReadAllText(this FilePathInfo info)
    {
        return File.ReadAllText(info.InputFullFilePath);
    }

    public static void CreateOutputDirectory(this FilePathInfo info)
    {
        Directory.CreateDirectory(info.OutputFullDirectoryPath);
    }

    public static void DeleteOutputFileIfExists(this FilePathInfo io)
    {
        if (File.Exists(io.OutputFullFilePath)) File.Delete(io.OutputFullFilePath);
    }
    public static void CopyFile(this FilePathInfo io)
    {
        io.CreateOutputDirectory();
        io.DeleteOutputFileIfExists();
        File.Copy(io.InputFullFilePath, io.OutputFullFilePath);
    }
  
}
