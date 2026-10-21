---
navigation.order: 3
---

# Custom

[Install MDU](../Setup.md), then render your documentation:

```bash
mdu render
```

By default, MDU reads the only `docs` folder and writes to `site`. To use different folders, pass the input and output paths after `render`, for example `mdu render ./content ./public/docs`. The generated site contains plain HTML, CSS, and JavaScript and needs no server-side code.

If the sources are on GitHub, pass the edit URL of your docs folder with `--editPageRoot`. Every page then gets an edit link, and the header shows a repository button:

```bash
mdu render --editPageRoot "https://github.com/example/project/edit/main/docs"
```
