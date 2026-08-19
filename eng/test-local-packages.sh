#!/usr/bin/env bash

set -euo pipefail

package_configuration="${1:-Release}"
if (( $# > 0 )); then
  shift
fi

package_version="$(
  dotnet msbuild src/ProMvvm/ProMvvm.csproj \
    -nologo \
    -getProperty:PackageVersion \
    -p:Configuration="$package_configuration" |
    tr -d '\r'
)"
package_directory="artifacts/package-integration/feed/$package_version"
mkdir -p "$package_directory"

dotnet pack src/ProMvvm.SourceGenerators \
  -c "$package_configuration" \
  -o "$package_directory"
dotnet pack src/ProMvvm \
  -c "$package_configuration" \
  -o "$package_directory"

./eng/run-package-integrations.sh "$package_directory" "$package_version" "$@"
