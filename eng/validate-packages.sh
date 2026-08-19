#!/usr/bin/env bash

set -euo pipefail

release_version="${1:-}"
package_directory="${2:-artifacts/packages}"

if [[ -z "$release_version" ]]; then
  echo "Usage: $0 <package-version> [package-directory]" >&2
  exit 2
fi

generator_package="$package_directory/ProMvvm.SourceGenerators.$release_version.nupkg"
runtime_package="$package_directory/ProMvvm.$release_version.nupkg"
runtime_symbols="$package_directory/ProMvvm.$release_version.snupkg"

for release_file in "$generator_package" "$runtime_package" "$runtime_symbols"; do
  if [[ ! -f "$release_file" ]]; then
    echo "Expected release artifact '$release_file' was not produced." >&2
    exit 1
  fi
done

package_count="$(find "$package_directory" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) | wc -l | tr -d ' ')"
if [[ "$package_count" != "3" ]]; then
  echo "Expected exactly three package artifacts, found $package_count in '$package_directory'." >&2
  exit 1
fi

generator_entries="$(unzip -Z1 "$generator_package")"
runtime_entries="$(unzip -Z1 "$runtime_package")"
runtime_nuspec="$(unzip -p "$runtime_package" 'ProMvvm.nuspec')"

if ! grep -Fxq 'analyzers/dotnet/cs/ProMvvm.SourceGenerators.dll' <<< "$generator_entries"; then
  echo "Generator package does not contain its analyzer DLL at the expected path." >&2
  exit 1
fi

if grep -Eq '^lib/' <<< "$generator_entries"; then
  echo "Generator package must not expose compile or runtime assets under lib/." >&2
  exit 1
fi

if ! grep -Fxq 'lib/net10.0/ProMvvm.dll' <<< "$runtime_entries"; then
  echo "Runtime package does not contain lib/net10.0/ProMvvm.dll." >&2
  exit 1
fi

if grep -Fq 'ProMvvm.SourceGenerators.dll' <<< "$runtime_entries"; then
  echo "Runtime package must consume, not embed, the source generator." >&2
  exit 1
fi

if ! grep -Eq "<dependency id=\"ProMvvm.SourceGenerators\" version=\"$release_version\"" <<< "$runtime_nuspec"; then
  echo "Runtime package does not depend on ProMvvm.SourceGenerators $release_version." >&2
  exit 1
fi

if grep -Eq '<dependency id="ProMvvm.SourceGenerators"[^>]*exclude="[^"]*Analyzers' <<< "$runtime_nuspec"; then
  echo "Runtime package excludes the analyzer asset from its generator dependency." >&2
  exit 1
fi

echo "Validated ProMvvm release packages for version $release_version."
