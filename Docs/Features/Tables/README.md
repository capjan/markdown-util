# Tables

## Examples

These examples show basic tables, column alignment, and Markdown formatting inside table cells.

### Basic table

**Markdown source:**

```markdown title="Markdown"
| Command      | Purpose                    | Output        |
| ------------ | -------------------------- | ------------- |
| `mdu lint`   | Check Markdown files       | Lint results  |
| `mdu serve`  | Preview documentation      | Local website |
| `mdu render` | Build static documentation | HTML files    |
```

**Rendered result:**

| Command      | Purpose                    | Output        |
| ------------ | -------------------------- | ------------- |
| `mdu lint`   | Check Markdown files       | Lint results  |
| `mdu serve`  | Preview documentation      | Local website |
| `mdu render` | Build static documentation | HTML files    |

### Column alignment

**Markdown source:**

```markdown title="Markdown"
| Left aligned | Center aligned | Right aligned |
| :----------- | :------------: | ------------: |
| Markdown     | Markdown       | Markdown      |
| Short text   | Centered text  | 123.45        |
```

**Rendered result:**

| Left aligned | Center aligned | Right aligned |
| :----------- | :------------: | ------------: |
| Markdown     | Markdown       | Markdown      |
| Short text   | Centered text  | 123.45        |

### Formatting inside cells

**Markdown source:**

```markdown title="Markdown"
| Feature  | Example                      |
| -------- | ---------------------------- |
| Emphasis | **Important** and *helpful*  |
| Code     | `mdu render`                 |
| Link     | [GitHub](https://github.com) |
```

**Rendered result:**

| Feature  | Example                      |
| -------- | ---------------------------- |
| Emphasis | **Important** and *helpful*  |
| Code     | `mdu render`                 |
| Link     | [GitHub](https://github.com) |
