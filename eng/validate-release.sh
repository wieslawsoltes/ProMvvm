#!/usr/bin/env bash

set -euo pipefail

release_tag="${1:-}"
release_main_ref="${2:-origin/main}"

if [[ -z "$release_tag" ]]; then
  echo "Usage: $0 <release-tag> [main-ref]" >&2
  exit 2
fi

if [[ ! "$release_tag" =~ ^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$ ]]; then
  echo "Release tag '$release_tag' must use vMAJOR.MINOR.PATCH or vMAJOR.MINOR.PATCH-PRERELEASE." >&2
  exit 1
fi

release_version="${release_tag#v}"
runtime_version="$(dotnet msbuild src/ProMvvm/ProMvvm.csproj -nologo -getProperty:PackageVersion -p:Configuration=Release | tr -d '\r')"
generator_version="$(dotnet msbuild src/ProMvvm.SourceGenerators/ProMvvm.SourceGenerators.csproj -nologo -getProperty:PackageVersion -p:Configuration=Release | tr -d '\r')"

if [[ "$runtime_version" != "$release_version" ]]; then
  echo "ProMvvm package version '$runtime_version' does not match tag version '$release_version'." >&2
  exit 1
fi

if [[ "$generator_version" != "$release_version" ]]; then
  echo "ProMvvm.SourceGenerators package version '$generator_version' does not match tag version '$release_version'." >&2
  exit 1
fi

release_commit="$(git rev-list -n 1 "$release_tag")"
head_commit="$(git rev-parse HEAD)"

if [[ "$release_commit" != "$head_commit" ]]; then
  echo "Release tag '$release_tag' resolves to $release_commit, but the checked-out commit is $head_commit." >&2
  exit 1
fi

if ! git merge-base --is-ancestor "$release_commit" "$release_main_ref"; then
  echo "Release commit $release_commit is not reachable from $release_main_ref." >&2
  exit 1
fi

release_prerelease=false
if [[ "$release_version" == *-* ]]; then
  release_prerelease=true
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "version=$release_version"
    echo "prerelease=$release_prerelease"
  } >> "$GITHUB_OUTPUT"
fi

echo "Validated release $release_tag ($release_version) at $release_commit."
