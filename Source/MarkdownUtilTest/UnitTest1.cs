using System.Runtime.InteropServices;

namespace MarkdownUtilTest;

public class UnitTest1
{
    [Theory]
    [InlineData("/folder1/folder2/folder3/", "/folder1/folder2/folder3/folder4/", "folder4/")]
    [InlineData("/folder1/folder2/folder3/", "/folder1/folder2/folder3/folder4", "folder4")]
    public void Test1(string origin, string target, string expected)
    {
        var result = Path.GetRelativePath(origin, target);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // replace the windows backslash with an slash to prevent breaking the test
            result = result.Replace("\\", "/");            
        }

        Assert.Equal(expected, result);
    }
}