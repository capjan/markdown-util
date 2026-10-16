# Icons and Emojis

MarkdownUtil turns shortcodes such as `:rocket:` or `:material-account-circle:` into emojis and icons while it renders your Markdown. More than 14,000 icons from six open source icon sets and 1,600 emojis are built in. You do not need to configure anything, and the rendered pages load nothing from a CDN.

:rocket: :tada: :white_check_mark: :material-account-circle: :fontawesome-brands-github: :octicons-heart-fill-24: :simple-dotnet: :fontawesome-solid-terminal:

## Search

Type part of a name, such as `github`, `arrow right`, or `heart`. Click a result to copy its shortcode. The filters limit the results to icons or emojis.

```icon-search
```

Add the same search to your own documentation with an empty `icon-search` code block:

````markdown
```icon-search
```
````

MarkdownUtil writes the search script and its index to the `js` folder of the site only when a page contains an icon search.

## Usage

### Using emojis

The best way to add an emoji is usually to type the emoji itself. It needs no conversion, so your Markdown reads the same on GitHub, in your editor, and in every other tool. Every major operating system has an emoji picker:

| Operating system | Shortcut |
|---|---|
| macOS | ++ctrl+cmd+space++ or ++globe+e++ |
| Windows | ++win+period++ or ++win+semicolon++ |
| Linux (GNOME and GTK apps) | ++ctrl+period++ or ++ctrl+semicolon++ |

If you prefer to keep your files plain ASCII, use a shortcode instead. Emojis use the same shortcodes as GitHub, such as `:smile:`, `:+1:`, `:warning:`, or `:de:`:

```markdown
Ship it :rocket:
```

Ship it :rocket:

MarkdownUtil writes the Unicode character into the page, so both ways produce the same result: emojis look like the emojis of the reader's operating system, and GitHub shows them too.

### Using icons

Icons use the name of their icon set as a prefix, followed by the icon name. The prefixes are the same as in [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/reference/icons-emojis/), so existing documentation keeps working.

```markdown
Sign in with :material-account-circle: or :fontawesome-brands-github: GitHub.
```

Sign in with :material-account-circle: or :fontawesome-brands-github: GitHub.

| Icon set | Prefix | Icons |
|---|---|---|
| :material-account-circle: [Material Design Icons](https://pictogrammers.com/library/mdi/) | `material-` | 7,447 |
| :fontawesome-brands-github: [Font Awesome Free](https://fontawesome.com/search?ic=free) Brands | `fontawesome-brands-` | 608 |
| :fontawesome-regular-face-laugh-wink: Font Awesome Free Regular | `fontawesome-regular-` | 272 |
| :fontawesome-solid-rocket: Font Awesome Free Solid | `fontawesome-solid-` | 2,000 |
| :octicons-heart-fill-24: [Octicons](https://primer.style/octicons/) | `octicons-` | 774 |
| :simple-dotnet: [Simple Icons](https://simpleicons.org/) | `simple-` | 3,466 |

For example, `:material-account-circle:` shows the `account-circle` icon from Material Design Icons, and `:octicons-heart-fill-24:` shows the 24-pixel version of the filled Octicons heart.

Icons are embedded into the page as inline SVG. They scale with the font size and take the color of the surrounding text, so they fit headings, links, and the light and dark theme without extra work:

> [!TIP]
> :octicons-light-bulb-16: Icons in admonitions take the color of the text around them.

Outside of MarkdownUtil, for example on GitHub, icon shortcodes are shown as plain text.

### Where shortcodes work

Shortcodes work anywhere in text, for example in paragraphs, lists, tables, links, and admonitions. They are not replaced inside code spans and code blocks, so you can still write about them, as this page does. A shortcode that MarkdownUtil does not know stays as it is.

### Styling

Add an attribute list directly after a shortcode, without a space, to give the icon or emoji an ID, classes, or HTML attributes. For example, set a color with the `style` attribute:

```markdown
:octicons-heart-fill-24:{ style="color: #e5484d" } Made with love
```

:octicons-heart-fill-24:{ style="color: #e5484d" } Made with love

## HTML output

An icon is rendered as a `span` with the class `icon` that contains the SVG; the SVG is hidden from screen readers because icons are usually decorative. An emoji is rendered as a `span` with the class `emoji` and the shortcode as its title:

```html
<span class="icon"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" aria-hidden="true" focusable="false">…</svg></span>
<span class="emoji" title=":rocket:">🚀</span>
```

## Licenses

The icon sets are published by their authors under these licenses:

- Material Design Icons: [Apache License 2.0](https://github.com/Templarian/MaterialDesign/blob/master/LICENSE), by Pictogrammers
- Font Awesome Free: [CC BY 4.0](https://fontawesome.com/license/free), by Fonticons, Inc.
- Octicons: [MIT License](https://github.com/primer/octicons/blob/main/LICENSE), by GitHub
- Simple Icons: [CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md), by the Simple Icons contributors

Simple Icons contains brand logos. The logos remain trademarks of their owners, so check a brand's guidelines before you use its logo.
