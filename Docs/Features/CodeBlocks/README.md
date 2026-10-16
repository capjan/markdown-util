# Code Blocks

## Overview

Fenced code blocks display code or commands without interpreting their contents as Markdown. Add a language identifier after the opening fence to enable syntax highlighting.

## Syntax

Add a language identifier after the opening backticks:

~~~markdown
```csharp
var greeting = "Hello, world!";
Console.WriteLine(greeting);
```
~~~

The rendered result keeps the code in a scrollable block and highlights supported syntax:

```csharp
var greeting = "Hello, world!";
Console.WriteLine(greeting);
```

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
