# Publishing as a standalone public GitHub repository

The target name/visibility was approved: `pippirium/OpWeave-Sample-Operations`, **public**.

This scaffold has **not** been published by ChatGPT. The current GitHub connection has no create-repository action, and the published package is not yet installable.

From the extracted root directory on a computer with `git` and authenticated GitHub CLI (`gh`):

```sh
git init -b main
git add .
git commit -m "feat: bootstrap external OpWeave sample UPM package"
gh repo create pippirium/OpWeave-Sample-Operations --public --source=. --remote=origin --push
```

Keep the repository private until secrets/credentials and LICENSE are checked, but the owner has expressly authorized `public`. Make a tag only after the actual OpWeave Core API is implemented and Unity tests pass. Prefer pinned full commit IDs in Unity manifests and record tests on reviewed head SHA.

**Do not report W2.13 or AT-F01 as passing just because these files exist.**
