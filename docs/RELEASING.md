# Releasing ProMvvm

Releases are produced by the tag-driven [release workflow](../.github/workflows/release.yml). The workflow validates, tests, packs, and runs the complete local NuGet integration matrix before it publishes both packages to NuGet.org and creates the matching GitHub release.

## One-time setup

Create a GitHub environment named `release`. Protect it with the reviewers and deployment-branch or tag rules appropriate for the repository. Both publishing jobs use this environment.

Add the NuGet.org profile name as an Actions repository variable named `NUGET_USER`:

```bash
gh variable set NUGET_USER --repo wieslawsoltes/ProMvvm --body '<nuget.org-profile-name>'
```

Configure a NuGet.org trusted publishing policy for each package, `ProMvvm` and `ProMvvm.SourceGenerators`, with these values:

| Policy field | Value |
| --- | --- |
| Repository owner | `wieslawsoltes` |
| Repository | `ProMvvm` |
| Workflow file | `release.yml` |
| Environment | `release` |

The workflow uses GitHub's OpenID Connect identity through `NuGet/login@v1`; it does not need or store a long-lived NuGet API key. The NuGet.org profile name in `NUGET_USER` must match the account that owns the trusted publishing policies.

## Publish a release

1. Update `ProMvvmPackageVersion` in [`Directory.Build.props`](../Directory.Build.props). Both packages deliberately share this version.
2. Merge the version change to `main` and wait for CI to pass.
3. From an up-to-date `main`, create an annotated or signed tag whose name is exactly `v` plus the package version.
4. Push only that tag to start the release.

For example:

```bash
git switch main
git pull --ff-only
git tag -s v0.1.0 -m "ProMvvm 0.1.0"
git push origin v0.1.0
```

Use `git tag -a` instead of `git tag -s` if a signing key is not configured. Prerelease versions and tags such as `0.2.0-beta.1` and `v0.2.0-beta.1` are supported and create a GitHub prerelease.

Do not create the GitHub release manually. The tag push is the release trigger.

## Release gates and outputs

Before publishing, the workflow verifies that:

- the tag has a supported version form and exactly matches both project package versions;
- the tag resolves to the checked-out commit and that commit is reachable from `main`;
- the solution builds and all runtime, generator, and integration test suites pass;
- all 12 isolated runtime, explicit-generator, adapter, CommunityToolkit.Mvvm, ReactiveUI core, and ReactiveUI.Reactive package projects restore and pass from the local package output;
- NuGet restore metadata proves each ProMvvm package dependency came from that local output rather than NuGet.org or a machine cache;
- the generator package exposes only its analyzer asset, while the runtime package contains the runtime assembly and retains its generator dependency.

The workflow publishes `ProMvvm.SourceGenerators` before `ProMvvm`, uploads the runtime symbol package automatically with the runtime package, and attaches all packages plus `SHA256SUMS` to the generated GitHub release. NuGet pushes use `--skip-duplicate`, so a failed publishing job can be rerun safely after a partial NuGet.org upload.

Package versions are immutable. If a release contains a defect, increment the version and publish a new tag instead of moving or reusing the existing tag.
