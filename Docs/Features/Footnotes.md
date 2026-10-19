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

Hover over a reference or move keyboard focus to it to preview the footnote. The preview stays open while the pointer is over it.

Footnotes appear at the bottom of the page. Their return arrows appear on hover or keyboard focus. Clicking an arrow returns to the text and outlines every reference to that footnote. Click elsewhere or press Escape to clear the highlighting. The footnote reached by a reference is emphasized while the others remain secondary.
