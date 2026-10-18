# Code Blocks

## Overview

Fenced code blocks display code or commands without interpreting their contents as Markdown. Add a language identifier after the opening fence to enable syntax highlighting.

## Syntax

Add a language identifier after the opening backticks:

~~~markdown title="Markdown"
```csharp
var greeting = "Hello, world!";
Console.WriteLine(greeting);
```
~~~

Add `title="..."` after the language to display a title above the code:

~~~markdown title="Markdown"
```js title="index.js"
console.log('Hello World!')
```
~~~

GitHub still displays this as a JavaScript code block, but does not render the title because GitHub Markdown has no code block title syntax. MarkdownUtil displays the title above the code.

MarkdownUtil renders the titled code block with a header and syntax highlighting:

```js title="index.js"
console.log('Hello World!')
```

Hover over a code block or focus it with the keyboard to show the copy button. It copies the source without the title.

If you omit the language identifier or use an unsupported one, the code is rendered without syntax highlighting:

~~~
This is plain code.
~~~

## Supported languages

Use one of these language identifiers:

| Language | Identifiers |
| --- | --- |
| C# | `cs`, `csharp` |
| Swift | `swift` |
| YAML | `yaml`, `yml` |
| Shell | `bash`, `zsh`, `sh`, `shell` |
| Markdown | `md`, `markdown` |

Markdown code blocks highlight headings, block markers, links, inline code, and emphasis while keeping the Markdown source visible as code. The highlighter uses lightweight language rules rather than full language parsers, so it colors selected syntax elements. Other languages remain readable as plain code.
