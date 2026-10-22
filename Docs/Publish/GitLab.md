---
navigation.order: 2
---

# GitLab

GitLab CI/CD can render your documentation and publish it with GitLab Pages. Add this `.gitlab-ci.yml` to the root of your repository; it publishes when files under `docs/` change on the default branch:

```yaml title=".gitlab-ci.yml"
pages:
  stage: deploy
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet tool install mdu --tool-path .tools
    - .tools/mdu render docs public
  pages:
    publish: public
  rules:
    - if: '$CI_PIPELINE_SOURCE == "push" && $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH'
      changes:
        - docs/**/*
```

GitLab publishes the generated `public` folder; since GitLab 17.10 it is included as a job artifact automatically. Make sure a GitLab Runner is enabled for the project.

> [!NOTE]
> If your source folder has another name, update both `docs` and `docs/**/*` to match.

[GitLab Pages documentation](https://docs.gitlab.com/user/project/pages/)
