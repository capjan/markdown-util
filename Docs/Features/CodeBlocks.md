# Code Blocks

Fenced code blocks show code or commands exactly as written. A language identifier after the opening fence turns on syntax highlighting, and every block gets a copy button.

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

GitHub still displays this as a JavaScript code block, but does not render the title because GitHub Markdown has no code block title syntax. MDU displays the title above the code.

MDU renders the titled code block with a header and syntax highlighting:

```js title="index.js"
console.log('Hello World!')
```

> [!TIP]
> Hover over a code block or focus it with the keyboard to show the copy button. It copies the code without the title.

If you omit the language identifier or use an unsupported one, the code is rendered without syntax highlighting:

~~~
This is plain code.
~~~

## Supported languages

Use one of these language identifiers:

| Language | Identifiers |
| --- | --- |
| C# | `cs`, `csharp` |
| JavaScript | `js`, `javascript`, `jsx` |
| Swift | `swift` |
| TypeScript | `ts`, `typescript` |
| Python | `py`, `python` |
| Kotlin | `kt`, `kotlin` |
| JSON | `json` |
| SQL | `sql` |
| HTML | `html`, `htm` |
| CSS | `css` |
| Java | `java` |
| Rust | `rs`, `rust` |
| Go | `go`, `golang` |
| YAML | `yaml`, `yml` |
| Shell | `bash`, `zsh`, `sh`, `shell` |
| Markdown | `md`, `markdown` |

Markdown code blocks highlight headings, block markers, links, inline code, and emphasis while keeping the Markdown source visible as code. The highlighter uses lightweight language rules rather than full language parsers, so it colors selected syntax elements. Other languages remain readable as plain code.
