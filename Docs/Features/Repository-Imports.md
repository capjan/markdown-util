# Repository Imports

MDU can combine Markdown pages from several Git repositories into one documentation site. Add an import declaration to the front matter of a page where the imported section should appear:

```yaml title="docs/API/README.md"
---
title: API reference
mdu:
  import:
    source: https://github.com/example/api-docs/tree/main/Docs
navigation:
  order: 20
---
```

Copy the repository URL that points to the folder. MDU supports the common browse URL formats used by GitHub and GitHub Enterprise (`/tree/<revision>/<folder>`), GitLab (`/-/tree/<revision>/<folder>`), Bitbucket (`/src/<revision>/<folder>`), and Gitea or Forgejo (`/src/branch/<branch>/<folder>`). Hosts that use the GitHub-style `/tree/` or source-style `/src/` route also work, including self-hosted instances. For a host with a different browser URL, use its HTTPS clone URL and append `#<revision>/<folder>`, for example `https://git.example.net/team/api.git#main/Docs`. GitLab URLs may include `?ref_type=heads` or `?ref_type=tags`.

The revision may be omitted. In that case MDU uses the `main` branch. A repository URL without a folder imports the repository root; a folder can be specified without a revision, for example `https://github.com/example/api-docs/tree/Docs`.

MDU imports the folder's page tree at the declaration's position. The imported folder must contain a `README.md` or `index.md` at its root. The declaration page's front matter controls the root page metadata; keep the declaration page's body empty and put its content in the imported root page instead. Front matter on imported child pages is preserved. Imports declared on local child pages are expanded recursively; imports declared inside the imported repository are rejected.

Edit links on imported pages point to the matching file in the source repository. GitHub-style URLs use their edit route, GitLab URLs use GitLab's edit route, and source-style hosts open the source file page where their edit action is available.

Git must be installed. MDU retrieves repositories with the Git command-line tool, so public repositories work without credentials and private repositories use the authentication already configured for Git. Do not put credentials in the source URL.
