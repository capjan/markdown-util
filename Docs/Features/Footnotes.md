# Footnotes

MDU renders labeled footnote references and definitions as automatically numbered footnotes.

## Syntax

The text after `^` is a label that connects a reference to its definition. It is not the visible number: MDU assigns that number automatically. Reusing a label points to the same footnote.

~~~markdown title="Markdown"
Tony's repulsor[^repulsor] overheats.
Shuri spots vibranium[^vibranium]. Tony checks the repulsor[^repulsor].

[^repulsor]: Iron Man's energy weapon.
[^vibranium]: Wakanda's energy-absorbing metal.
~~~

## Rendered example

Tony's repulsor[^repulsor] overheats.
Shuri spots vibranium[^vibranium]. Tony checks the repulsor[^repulsor].

[^repulsor]: Iron Man's energy weapon.
[^vibranium]: Wakanda's energy-absorbing metal.

## In the rendered site

- Hover over a reference, or focus it with the keyboard, to preview the footnote. The preview stays open while the pointer is over it.
- Footnotes are listed at the bottom of the page. The footnote you jumped to is emphasized; the others stay in the background.
- A return arrow appears on hover or focus. It jumps back to the text and outlines every reference to that footnote. Click elsewhere or press ++esc++ to clear the outline.
