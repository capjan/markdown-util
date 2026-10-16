# Formatting

MarkdownUtil uses Markdig to render CommonMark syntax and its enabled extensions. Markdown remains readable as plain text in the source file. The examples below show the syntax and the rendered result.

## Headings and paragraphs

Use one to six `#` characters for heading levels one through six. The rendered headings receive an automatic ID and a permalink. Separate paragraphs with a blank line.

~~~markdown
## A section heading

This is one paragraph. A line break in the source
continues the paragraph.

This is a second paragraph.
~~~

### A section heading

This is one paragraph. A line break in the source
continues the paragraph.

This is a second paragraph.

## Line breaks

A single newline in the Markdown source continues the same paragraph. To render a hard line break, end the line with two spaces or a backslash. The two-space form is preferred for readability: it keeps the break unobtrusive in the Markdown source. Press the ++space++ key twice at the end of the line:

~~~markdown
This line ends here.  
This line starts below it.
~~~

Rendered result:

This line ends here.  
This line starts below it.

The backslash is an alternative:

~~~markdown
This line ends here.\
This line starts below it.
~~~

Rendered result:

This line ends here.\
This line starts below it.

## Inline formatting

Use asterisks for emphasis and backticks for inline code. Markdig's emphasis extras also support strikethrough, subscript, superscript, inserted text, and marked text:

~~~markdown
*italic*, **bold**, ***bold italic***, ~~deleted~~, H~2~O, x^2^, ++inserted++, ==marked==, and `inline code`.
~~~

*italic*, **bold**, ***bold italic***, ~~deleted~~, H~2~O, x^2^, ++inserted++, ==marked==, and `inline code`.

The `++...++` syntax also represents known keyboard shortcuts, such as ++cmd+s++. See [Keyboard Keys](../Keys/README.md) for details.

## Lists

Use `-`, `*`, or `+` for unordered lists, and a number followed by a period for ordered lists. Indent nested items by two spaces:

~~~markdown
- First item
- Second item
  - Nested item

1. First step
2. Second step
~~~

- First item
- Second item
  - Nested item

1. First step
2. Second step

Task lists use `[ ]` for an open item and `[x]` for a completed item:

~~~markdown
- [x] Write the page
- [ ] Review the examples
~~~

- [x] Write the page
- [ ] Review the examples

## Code and tables

Use triple backticks to fence a code block. Add a language name to enable syntax highlighting:

~~~~markdown
```csharp
var greeting = "Hello, world!";
Console.WriteLine(greeting);
```
~~~~

```csharp
var greeting = "Hello, world!";
Console.WriteLine(greeting);
```

Separate table columns with pipes and add a row of hyphens below the header:

~~~markdown
| Syntax | Result |
| --- | --- |
| `**bold**` | **bold** |
| `*italic*` | *italic* |
~~~

| Syntax | Result |
| --- | --- |
| `**bold**` | **bold** |
| `*italic*` | *italic* |

See [Code Blocks](../CodeBlocks/README.md) for supported highlighter languages and [Tables](../Tables/README.md) for alignment and more examples.

## Links and images

Create a link with a label and a destination. An optional title appears when the reader hovers over the link. Plain URLs such as `https://github.com/xoofx/markdig` are also recognized automatically.

~~~markdown
[Markdig](https://github.com/xoofx/markdig "Markdig on GitHub")
~~~

[Markdig](https://github.com/xoofx/markdig "Markdig on GitHub")

Images use the same link-like syntax with an exclamation mark and alternative text: `![Description](image.png)`. See [Images](../Images/README.md) for how local image files are handled.

## Block quotes and separators

Start each quoted line with `>`; block quotes can contain other Markdown formatting, including lists:

~~~markdown
> A block quote can contain *emphasis*.
>
> - It can also contain a list.
~~~

> A block quote can contain *emphasis*.
>
> - It can also contain a list.

Use a line containing three hyphens, asterisks, or underscores to add a horizontal separator:

~~~markdown
---
~~~

---

## Footnotes

Markdig supports footnote references and definitions:

~~~markdown
MarkdownUtil uses Markdig.[^markdig]

[^markdig]: Markdig is a Markdown processor for .NET.
~~~

MarkdownUtil uses Markdig.[^markdig]

[^markdig]: Markdig is a Markdown processor for .NET.

## Related features

For GitHub-style callouts, see [Admonitions](../Admonitions/README.md).
