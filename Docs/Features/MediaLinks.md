# Media Links

Put a YouTube or Vimeo link on a line of its own to show the video in a responsive player. A link inside a sentence stays an ordinary link.

## Syntax

```markdown title="Markdown"
[Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)
```

[Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)

## Privacy by default

An embedded player contacts its provider as soon as it loads, even if nobody presses play. MDU therefore shows a placeholder first and loads the video only when the reader agrees:

- **Load this video once from YouTube** loads only this video on this page.
- **Always allow YouTube on this site** saves the choice in the browser's local storage, so later visits load that provider's videos directly.

The reader can review or revoke saved choices at any time under **Media settings** in the page header. Revoking removes the saved choice and unloads that provider's players on the current page.

> [!NOTE]
> Once a video is loaded, the provider may process information about the visit, and revoking cannot undo what it already received. The placeholder links to the provider's privacy policy, and Vimeo players use the `dnt=1` option to limit new cookies.
