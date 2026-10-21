# Media Links

Use an HTML `<video>` element to embed an MP4 directly. Put a YouTube or Vimeo link on a line of its own to show it in a responsive player; a link inside a sentence stays an ordinary link.

## Direct MP4 video

This eight-second demo scene is attached to the [v4.6.1 GitHub release](https://github.com/capjan/mdu/releases/tag/v4.6.1), so the video is available to the documentation without being stored in the Git repository.

```html title="HTML"
<video controls="controls" playsinline="playsinline" preload="none" width="960" poster="https://github.com/capjan/mdu/releases/download/v4.6.1/mdu-demoscene-preview.png">
  <source src="https://github.com/capjan/mdu/releases/download/v4.6.1/mdu-demoscene.mp4" type="video/mp4" />
  Your browser does not support embedded videos.
</video>
```

<video controls="controls" playsinline="playsinline" preload="none" width="960" poster="https://github.com/capjan/mdu/releases/download/v4.6.1/mdu-demoscene-preview.png">
  <source src="https://github.com/capjan/mdu/releases/download/v4.6.1/mdu-demoscene.mp4" type="video/mp4" />
  Your browser does not support embedded videos.
</video>

## YouTube and Vimeo

### YouTube

```markdown title="Markdown"
[Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)
```

[Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)

### Vimeo

```markdown title="Markdown"
[Watch the Vimeo demo](https://vimeo.com/76979871)
```

[Watch the Vimeo demo](https://vimeo.com/76979871)

### Privacy by default

An embedded player contacts its provider as soon as it loads, even if nobody presses play. MDU therefore shows a placeholder first and loads the video only when the reader agrees:

- **Load this video once** loads only that video on this page.
- **Always allow this provider on this site** saves the choice in the browser's local storage, so later visits load that provider's videos directly.

The reader can review or revoke saved choices at any time under **Media settings** in the page header. Revoking removes the saved choice and unloads that provider's players on the current page.

> [!NOTE]
> Once a video is loaded, the provider may process information about the visit, and revoking cannot undo what it already received. The placeholder links to the provider's privacy policy, and Vimeo players use the `dnt=1` option to limit new cookies.
