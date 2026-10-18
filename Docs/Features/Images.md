# Images

## Overview

MarkdownUtil renders images with standard Markdown syntax. Images keep their relative paths in the generated site, and local image files are copied to the output folder when you run `mdu render` or `mdu serve`.

## Syntax

Add alternative text in square brackets and the image URL in parentheses. An optional title appears when a visitor hovers over the image:

~~~markdown title="Markdown"
![Hand checking off items on a to-do list](https://images.unsplash.com/photo-1768055105012-4c53791b2877)
~~~

![Hand checking off items on a to-do list](https://images.unsplash.com/photo-1768055105012-4c53791b2877)
> Photo by [Jakub Żerdzicki](https://unsplash.com/@jakubzerdzicki) on [unsplash](https://unsplash.com/photos/hand-checking-off-items-on-a-to-do-list-fXlL5I0IvK0)

Alternative text is included in the generated image element and helps describe the image to people using assistive technology.

## Local images

Keep local image files inside the documentation folder and link to them with a relative path from the Markdown file. For example, a page can refer to an image in its own `images` subfolder as `images/page.png`.

`mdu render` and `mdu serve` copy linked local images while preserving their relative paths. Supported file extensions are `.bmp`, `.png`, `.gif`, `.jpg`, `.jpeg`, `.webp`, and `.svg`. The image target must exist; `mdu lint` reports a missing local image target under Rule 4.

Images scale to fit the page width while keeping their proportions, so wide images also fit on smaller screens.

In the rendered site, click an image to open a larger view. If the image links to another page, its link still works; images linked to their own file open in the larger view. Close the view with its close button, by clicking the backdrop, or with `Esc`.

External image URLs remain hosted at their original location and are not copied into the output folder.
