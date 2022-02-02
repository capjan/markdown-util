// using System.Text.RegularExpressions;
// using MarkdownDocument.Model;
//
// namespace MarkdownUtil.Utils;
//
// public class MarkdownUtil
// {
//
//     public static string GetTitle(FileInfo fileInfo)
//     {
//         var content = File.ReadAllText(fileInfo.FullName);
//         var m = Regex.Match(content, @"^#\s+(?<title>[^\n]+)");
//         return m.Success ? m.Groups["title"].Value : "";
//     }
//
//     public static MarkdownHeader ReadHeader(FileInfo fileInfo)
//     {
//
//     }
// }
