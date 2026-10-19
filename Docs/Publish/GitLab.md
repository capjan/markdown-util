---
navigation.order: 2
---

# GitLab

Use GitLab CI/CD to render and publish your documentation with GitLab Pages.

Add this `.gitlab-ci.yml` to the root of your repository. It publishes when files under `docs/` change on the default branch:

```yaml title=".gitlab-ci.yml"
pages:
  stage: deploy
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet tool install MarkdownUtil --tool-path .tools
    - .tools/mdu render docs public
  pages:
    publish: public
  rules:
    - if: '$CI_PIPELINE_SOURCE == "push" && $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH'
      changes:
        - docs/**/*
```

Make sure a GitLab Runner is enabled for the project. If your Markdown files are in a folder named `Docs`, change both `docs` and `docs/**/*` to `Docs` and `Docs/**/*`.

GitLab 17.10 and later automatically includes the published folder as a job artifact.

GitLab publishes the generated `public` directory. See [GitLab Pages documentation](https://docs.gitlab.com/user/project/pages/).
