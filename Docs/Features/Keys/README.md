# Keyboard Keys

## Overview

Show keyboard shortcuts the way readers see them on their keyboard. Wrap the keys in `++` and join them with `+`, and MarkdownUtil draws each key as a keycap with its usual symbol:

```markdown
Press ++ctrl+alt+del++ to open the security screen.

Save with ++cmd+s++, undo with ++cmd+z++, and redo with ++cmd+shift+z++.
```

Press ++ctrl+alt+del++ to open the security screen.

Save with ++cmd+s++, undo with ++cmd+z++, and redo with ++cmd+shift+z++.

The syntax is the same as in [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/reference/formatting/#adding-keyboard-keys), which uses the [Keys extension](https://facelessuser.github.io/pymdown-extensions/extensions/keys/) of Python Markdown Extensions, so existing documentation keeps working.

## Syntax

- Name each key, for example `++shift+tab++`. Names are not case-sensitive, and most keys have short aliases such as `cmd`, `ctrl`, `opt`, `esc`, or `del`.
- Letters and digits are their own names: `++ctrl+c++` shows ++ctrl+c++.
- Punctuation keys have names too, because `+` separates the keys: `++cmd+period++` shows ++cmd+period++, and `++ctrl+plus++` shows ++ctrl+plus++.
- Put any other text in quotes to show it as a key: `++cmd+"Ü"++` shows ++cmd+"Ü"++, and `++"Any key"++` shows ++"Any key"++.

Text between `++` that is not a key combination, such as `++new text++`, stays [inserted text](https://xoofx.github.io/markdig/docs/extensions/emphasis-extra/) and is shown underlined.

## Keys with symbols

These keys show the symbol that is printed on most keyboards. Arrow keys show only their arrow, like on the keyboard. The first name is the key's name; the others are aliases.

| Key | Names |
|---|---|
| ++cmd++ | `command`, `cmd` |
| ++opt++ | `option`, `opt` |
| ++ctrl++ | `control`, `ctrl` |
| ++shift++ | `shift` |
| ++caps-lock++ | `caps-lock`, `capital`, `cplk` |
| ++win++ | `windows`, `win` |
| ++globe++ | `globe` |
| ++enter++ | `enter`, `return` |
| ++tab++ | `tab`, `tabulator` |
| ++backtab++ | `backtab`, `bktab` |
| ++backspace++ | `backspace`, `back`, `bksp` |
| ++del++ | `delete`, `del` |
| ++esc++ | `escape`, `esc` |
| ++space++ | `space`, `spc` |
| ++up++ ++down++ ++left++ ++right++ | `arrow-up`, `arrow-down`, `arrow-left`, `arrow-right`, `up`, `down`, `left`, `right` |
| ++page-up++ ++page-down++ | `page-up`, `page-down`, `pg-up`, `pg-dn` |
| ++home++ ++end++ | `home`, `end` |
| ++eject++ ++power++ | `eject`, `power` |
| ++play-pause++ ++prev-track++ ++next-track++ | `media-play-pause`, `media-prev-track`, `media-next-track`, `play-pause`, `prev-track`, `next-track` |

The left and right variants of the modifier keys, such as `left-shift` or `rcmd`, show the same symbols.

## Other keys

All other keys of the Keys extension are available as well, among them:

- Modifiers: ++alt++ `alt`, ++altgr++ `alt-graph`, ++fn++ `function`, ++meta++ `meta`, ++super++ `super`
- Function keys: ++f1++ to ++f24++ `f1` … `f24`
- Editing: ++ins++ `insert`, ++print-screen++ `print-screen`, ++scroll-lock++ `scroll-lock`, ++break++ `break`, ++help++ `help`
- Number pad: ++num0++ `num0` … `num9`, ++num-plus++ `num-plus`, ++num-enter++ `num-enter`, ++num-lock++ `num-lock`
- Punctuation: `backslash`, `bar`, `brace-left`, `brace-right`, `bracket-left`, `bracket-right`, `colon`, `comma`, `double-quote`, `equal`, `exclam`, `grave`, `greater`, `less`, `minus`, `period`, `plus`, `question`, `semicolon`, `single-quote`, `slash`, `tilde`, `underscore`
- Media and browser: `media-play`, `media-pause`, `media-stop`, `volume-up`, `volume-down`, `volume-mute`, `browser-back`, `browser-forward`, `browser-refresh`, `browser-search`
- Mouse: ++left-button++ `left-button`, ++middle-button++ `middle-button`, ++right-button++ `right-button`

## HTML output

A shortcut is rendered as a `span` with the class `keys`. Each named key is a `kbd` element with the class `key-<name>`; the symbol is hidden from screen readers, so they read the key's name:

```html
<span class="keys"><kbd class="key-command"><span class="key-symbol" aria-hidden="true">⌘</span><span class="key-label">Cmd</span></kbd><span>+</span><kbd class="key-s"><span class="key-label">S</span></kbd></span>
```

Outside of MarkdownUtil, for example on GitHub, shortcuts are shown as plain text such as `++cmd+s++`.
