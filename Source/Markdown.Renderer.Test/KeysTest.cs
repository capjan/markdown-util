using Markdig;
using Markdown.Renderer.MarkdigExtension;

namespace Markdown.Renderer.Test;

public class KeysTest
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseKeyboardKeys()
        .Build();

    private static string Render(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline).ReplaceLineEndings("\n");

    [Fact]
    public void RendersKeysWithSymbolsAndSeparators()
    {
        Assert.Equal(
            "<p>Save with <span class=\"keys\">" +
            "<kbd class=\"key-command\"><span class=\"key-symbol\" aria-hidden=\"true\">⌘</span><span class=\"key-label\">Cmd</span></kbd>" +
            "<span>+</span>" +
            "<kbd class=\"key-s\"><span class=\"key-label\">S</span></kbd>" +
            "</span>.</p>\n",
            Render("Save with ++cmd+s++."));
    }

    [Theory]
    [InlineData("++ctrl++", "key-control", "⌃", "Ctrl")]
    [InlineData("++Shift++", "key-shift", "⇧", "Shift")]
    [InlineData("++opt++", "key-option", "⌥", "Option")]
    [InlineData("++return++", "key-enter", "⏎", "Enter")]
    [InlineData("++esc++", "key-escape", "⎋", "Esc")]
    [InlineData("++win++", "key-windows", "⊞", "Win")]
    [InlineData("++bksp++", "key-backspace", "⌫", "Backspace")]
    public void ResolvesAliasesToKeysWithSymbols(string markdown, string className, string symbol, string label)
    {
        Assert.Equal(
            $"<p><span class=\"keys\"><kbd class=\"{className}\"><span class=\"key-symbol\" aria-hidden=\"true\">{symbol}</span>" +
            $"<span class=\"key-label\">{label}</span></kbd></span></p>\n",
            Render(markdown));
    }

    [Fact]
    public void ShowsOnlyTheSymbolOfArrowKeys()
    {
        Assert.Equal(
            "<p><span class=\"keys\"><kbd class=\"key-arrow-up\"><span class=\"key-symbol\" aria-hidden=\"true\">↑</span>" +
            "<span class=\"visually-hidden\">Up</span></kbd></span></p>\n",
            Render("++up++"));
    }

    [Fact]
    public void RendersQuotedKeysWithoutClass()
    {
        Assert.Equal(
            "<p><span class=\"keys\"><kbd class=\"key-alt\"><span class=\"key-label\">Alt</span></kbd><span>+</span>" +
            "<kbd>Ü</kbd><span>+</span><kbd>&lt;Any + key&gt;</kbd></span></p>\n",
            Render("++alt+\"&Uuml;\"+\"<Any + key>\"++"));
    }

    [Fact]
    public void ReadsTheClosingPlusSignsAfterTheLastKey()
    {
        Assert.Equal(
            "<p><span class=\"keys\"><kbd class=\"key-control\"><span class=\"key-symbol\" aria-hidden=\"true\">⌃</span><span class=\"key-label\">Ctrl</span></kbd>" +
            "<span>+</span><kbd class=\"key-plus\"><span class=\"key-label\">+</span></kbd></span>+</p>\n",
            Render("++ctrl+plus+++"));
    }

    [Theory]
    [InlineData("++inserted text++", "<p><ins>inserted text</ins></p>\n")]
    [InlineData("++unknown++", "<p><ins>unknown</ins></p>\n")]
    [InlineData("++ctrl+unknown++", "<p><ins>ctrl+unknown</ins></p>\n")]
    [InlineData("C++ and C++", "<p>C++ and C++</p>\n")]
    [InlineData("a + b", "<p>a + b</p>\n")]
    [InlineData("`++cmd+s++`", "<p><code>++cmd+s++</code></p>\n")]
    public void KeepsOtherPlusSignsUnchanged(string markdown, string expected)
    {
        Assert.Equal(expected, Render(markdown));
    }

    [Fact]
    public void RendersKeysInHeadingsButLeavesThemOutOfIdentifiers()
    {
        Assert.StartsWith("<h2 id=\"press\">Press <span class=\"keys\">", Render("## Press ++cmd+k++"));
    }
}
