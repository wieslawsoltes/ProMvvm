#!/usr/bin/env bash

set -euo pipefail

package_directory="${1:-}"
package_version="${2:-}"

if [[ -z "$package_directory" || -z "$package_version" ]]; then
  echo "Usage: $0 <package-directory> <package-version> [project ...]" >&2
  exit 2
fi

shift 2

if [[ ! -d "$package_directory" ]]; then
  echo "Package directory '$package_directory' does not exist." >&2
  exit 1
fi

package_directory="$(cd "$package_directory" && pwd)"
runtime_package="$package_directory/ProMvvm.$package_version.nupkg"
generator_package="$package_directory/ProMvvm.SourceGenerators.$package_version.nupkg"

./eng/validate-packages.sh "$package_version" "$package_directory"

package_fingerprint="$(
  shasum -a 256 "$runtime_package" "$generator_package" |
    shasum -a 256 |
    awk '{ print $1 }'
)"
integration_packages_directory="$(pwd)/artifacts/package-integration/cache/$package_version/$package_fingerprint"
runtime_extract_directory="$(pwd)/artifacts/package-integration/runtime/$package_fingerprint"
runtime_assembly="$runtime_extract_directory/lib/net10.0/ProMvvm.dll"
mkdir -p "$integration_packages_directory"
mkdir -p "$runtime_extract_directory"

if [[ ! -f "$runtime_assembly" ]]; then
  unzip -q "$runtime_package" 'lib/net10.0/ProMvvm.dll' -d "$runtime_extract_directory"
fi

package_projects=(
  tests/PackageIntegration/Plain.Typed/ProMvvm.PackageIntegration.Plain.Typed.csproj
  tests/PackageIntegration/Plain.Expression/ProMvvm.PackageIntegration.Plain.Expression.csproj
  tests/PackageIntegration/NotificationAdapters/ProMvvm.PackageIntegration.NotificationAdapters.csproj
  tests/PackageIntegration/SourceGenerators.Plain/ProMvvm.PackageIntegration.SourceGenerators.Plain.csproj
  tests/PackageIntegration/CommunityToolkit.Typed/ProMvvm.PackageIntegration.CommunityToolkit.Typed.csproj
  tests/PackageIntegration/CommunityToolkit.Expression/ProMvvm.PackageIntegration.CommunityToolkit.Expression.csproj
  tests/PackageIntegration/SourceGenerators.CommunityToolkit/ProMvvm.PackageIntegration.SourceGenerators.CommunityToolkit.csproj
  tests/PackageIntegration/ReactiveUIReactive.Typed/ProMvvm.PackageIntegration.ReactiveUIReactive.Typed.csproj
  tests/PackageIntegration/ReactiveUIReactive.Expression/ProMvvm.PackageIntegration.ReactiveUIReactive.Expression.csproj
  tests/PackageIntegration/SourceGenerators.ReactiveUI/ProMvvm.PackageIntegration.SourceGenerators.ReactiveUI.csproj
  tests/PackageIntegration/ReactiveUICore.Typed/ProMvvm.PackageIntegration.ReactiveUICore.Typed.csproj
  tests/PackageIntegration/ReactiveUICore.Expression/ProMvvm.PackageIntegration.ReactiveUICore.Expression.csproj
)

if (( $# > 0 )); then
  package_projects=("$@")
fi

for package_project in "${package_projects[@]}"; do
  if [[ ! -f "$package_project" ]]; then
    echo "Package integration project '$package_project' does not exist." >&2
    exit 1
  fi

  echo "Restoring $package_project from local ProMvvm packages."
  NUGET_PACKAGES="$integration_packages_directory" \
    dotnet restore "$package_project" \
      --force \
      --no-cache \
      --packages "$integration_packages_directory" \
      --source "$package_directory" \
      --source https://api.nuget.org/v3/index.json \
      -p:ProMvvmPackageVersion="$package_version" \
      -p:ProMvvmRuntimeAssembly="$runtime_assembly"

  restored_package_ids=(promvvm promvvm.sourcegenerators)
  if [[ "$package_project" == *'/SourceGenerators.'* ]]; then
    restored_package_ids=(promvvm.sourcegenerators)
    project_assets="$(dirname "$package_project")/obj/project.assets.json"
    if grep -Fq "\"ProMvvm/$package_version\"" "$project_assets"; then
      echo "$package_project must use the extracted runtime assembly, not a ProMvvm package reference." >&2
      exit 1
    fi
  fi

  for package_id in "${restored_package_ids[@]}"; do
    package_metadata="$integration_packages_directory/$package_id/$package_version/.nupkg.metadata"
    if [[ ! -f "$package_metadata" ]]; then
      echo "$package_project did not restore $package_id $package_version." >&2
      exit 1
    fi

    if ! grep -Fq "\"source\": \"$package_directory\"" "$package_metadata"; then
      echo "$package_project restored $package_id from a source other than '$package_directory'." >&2
      exit 1
    fi
  done

  echo "Testing $package_project."
  NUGET_PACKAGES="$integration_packages_directory" \
    dotnet test "$package_project" \
      -c Release \
      --no-restore \
      -p:ProMvvmPackageVersion="$package_version" \
      -p:ProMvvmRuntimeAssembly="$runtime_assembly" \
      --logger "console;verbosity=minimal"
done

echo "All ${#package_projects[@]} local-package integration projects passed."
