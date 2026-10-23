# Math

MDU renders GitHub-compatible math syntax to static SVG while it builds the site. The pages contain the formulas directly as inline SVG, so they display instantly in any browser.

## Inline math

Wrap an expression in single dollar signs:

```markdown title="Markdown"
The area of a circle is $A = \pi r^2$.
```

The area of a circle is $A = \pi r^2$.

## Display math

Use double dollar signs for a centered formula:

```markdown title="Markdown"
$$
\int_0^1 x^2\,dx = \frac{1}{3}
$$
```

$$
\int_0^1 x^2\,dx = \frac{1}{3}
$$

## Fenced math

GitHub's `math` code fence renders a display formula as well:

````markdown title="Markdown"
```math
\sum_{i=1}^{n} i
```
````

```math
\sum_{i=1}^{n} i
```

## Compatibility

- MDU supports the three forms shown above: `$...$`, `$$...$$`, and fenced `math` blocks. GitHub's additional inline delimiter form is not supported.
- The .NET LaTeX renderer implements a subset of LaTeX math commands, not a complete LaTeX engine. If an expression cannot be rendered, the page shows its TeX source.
- Formulas become inline SVG paths with theme-aware colors. The renderer's licenses are listed in the packaged `THIRD-PARTY-NOTICES.md`.

## References

- [GitHub: Writing mathematical expressions](https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/writing-mathematical-expressions) explains GitHub's Markdown delimiters.
- [Overleaf: Mathematical expressions](https://www.overleaf.com/learn/latex/Mathematical_expressions) is a reference for common LaTeX math syntax and symbols.
