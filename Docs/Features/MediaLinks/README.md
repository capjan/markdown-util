# Media Links

Add a YouTube or Vimeo link on its own paragraph to show the video in a responsive player. Before loading it, MarkdownUtil explains that the page will connect to the provider and waits for the reader to allow it.

An embedded player contacts its provider as soon as it loads. That request can share technical information and the page visit with YouTube or Vimeo, even if playback never starts. We wait for the reader's choice so opening a page does not automatically contact a video provider. This avoids unwanted third-party requests and gives readers control over whether to load a video once or save a provider permission that they can later change in Media settings.

## Syntax

Use a standard Markdown link on its own line:

~~~markdown
[:simple-youtube: Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)
~~~

[:simple-youtube: Watch the first version of ten fingers on YouTube](https://youtu.be/hyeCMRQhVOI)
 
By default, the video is not requested from YouTube until the reader selects **Load this video once from YouTube** or **Always allow YouTube on this site**. A one-time choice applies only to that video on the current page. A saved choice applies to that provider across the site and is stored in the browser's local storage, so later visits can load that provider's videos automatically. The reader can open **Media settings** in the page header at any time to review or revoke saved provider permissions. Revoking a permission removes the saved choice and unloads that provider's players currently shown on the page; it cannot undo data the provider already received. Once a video is loaded, the provider may process information about the visit. The placeholder links to the provider's privacy policy. Vimeo embeds also use Vimeo's `dnt=1` player option to limit new cookies.

Links that appear within a sentence remain ordinary clickable links.
