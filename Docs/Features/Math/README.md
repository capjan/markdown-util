# Math

MarkdownUtil renders GitHub-compatible math syntax to static SVG while building the site. The generated pages contain
the formulas directly, so browsers need no JavaScript math renderer or external font service.

## Inline math

Wrap an expression in single dollar signs. For example, the area of a circle is $A = \pi r^2$.

**Markdown:**

```markdown
The area of a circle is $A = \pi r^2$.
```

## Display math

Use double dollar signs for a centered formula:

$$
\int_0^1 x^2\,dx = \frac{1}{3}
$$

**Markdown:**

```markdown
$$
\int_0^1 x^2\,dx = \frac{1}{3}
$$
```

## Fenced math

GitHub's `math` code fence renders a display formula:

```math
\sum_{i=1}^{n} i
```

**Markdown:**

````markdown
```math
\sum_{i=1}^{n} i
```
````

## Compatibility

MarkdownUtil supports the three forms shown above: `$...$`, `$$...$$`, and fenced `math` blocks. GitHub documents an
additional inline delimiter form; MarkdownUtil does not currently support that form. Its .NET LaTeX renderer also
implements a subset of LaTeX math commands rather than a complete LaTeX document engine. If an expression cannot be
rendered, MarkdownUtil keeps its TeX source visible in the page.

The formulas are saved as inline SVG with paths and theme-aware colors. They do not add a JavaScript or Java runtime.
The renderer dependencies and their licenses are listed in the packaged `THIRD-PARTY-NOTICES.md`.

## References

- [GitHub: Writing mathematical expressions](https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/writing-mathematical-expressions) explains GitHub's Markdown delimiters.
- [Overleaf: Mathematical expressions](https://www.overleaf.com/learn/latex/Mathematical_expressions) is a reference for common LaTeX math syntax and symbols.
