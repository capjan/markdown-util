# Tables

MDU renders GitHub-style pipe tables. Wide tables scroll horizontally on small screens instead of breaking the layout.

## Basic table

Separate columns with `|` and the header from the body with a row of dashes:

```markdown title="Markdown"
| Command      | Purpose                    | Output        |
| ------------ | -------------------------- | ------------- |
| `mdu lint`   | Check Markdown files       | Lint results  |
| `mdu serve`  | Preview documentation      | Local website |
| `mdu render` | Build static documentation | HTML files    |
```

| Command      | Purpose                    | Output        |
| ------------ | -------------------------- | ------------- |
| `mdu lint`   | Check Markdown files       | Lint results  |
| `mdu serve`  | Preview documentation      | Local website |
| `mdu render` | Build static documentation | HTML files    |

## Column alignment

Colons in the separator row align a column left, center, or right:

```markdown title="Markdown"
| Left aligned | Center aligned | Right aligned |
| :----------- | :------------: | ------------: |
| Markdown     | Markdown       | Markdown      |
| Short text   | Centered text  | 123.45        |
```

| Left aligned | Center aligned | Right aligned |
| :----------- | :------------: | ------------: |
| Markdown     | Markdown       | Markdown      |
| Short text   | Centered text  | 123.45        |

## Formatting inside cells

Cells can contain inline Markdown such as emphasis, code, links, icons, and keyboard keys:

```markdown title="Markdown"
| Feature  | Example                          |
| -------- | -------------------------------- |
| Emphasis | **Important** and *helpful*      |
| Code     | `mdu render`                     |
| Link     | [GitHub](https://github.com)     |
| Icon     | :material-check-circle: Done     |
| Keys     | ++ctrl+c++                       |
```

| Feature  | Example                          |
| -------- | -------------------------------- |
| Emphasis | **Important** and *helpful*      |
| Code     | `mdu render`                     |
| Link     | [GitHub](https://github.com)     |
| Icon     | :material-check-circle: Done     |
| Keys     | ++ctrl+c++                       |
