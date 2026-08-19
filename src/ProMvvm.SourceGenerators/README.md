# ProMvvm.SourceGenerators

[![NuGet](https://img.shields.io/nuget/v/ProMvvm.SourceGenerators.svg)](https://www.nuget.org/packages/ProMvvm.SourceGenerators/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/wieslawsoltes/ProMvvm/blob/main/LICENSE)

`ProMvvm.SourceGenerators` is the compile-time analyzer package for [ProMvvm](https://github.com/wieslawsoltes/ProMvvm). It generates strongly typed, reflection-free `PropertyPath` descriptors for ordinary properties, CommunityToolkit.Mvvm `[ObservableProperty]` fields, and ReactiveUI.SourceGenerators `[Reactive]` fields.

## Installation

For normal applications, install only the main package:

```bash
dotnet add <your-project.csproj> package ProMvvm --version 0.1.0
```

`ProMvvm` depends on the matching generator package, so the analyzer is available automatically.

Use an explicit analyzer reference when you need to control its assets or version directly:

```xml
<ItemGroup>
  <PackageReference Include="ProMvvm" Version="0.1.0" />
  <PackageReference Include="ProMvvm.SourceGenerators"
                    Version="0.1.0"
                    PrivateAssets="all" />
</ItemGroup>
```

The explicit reference is especially useful for libraries that do not want the analyzer to flow to their consumers. If the ProMvvm runtime is supplied by a project or assembly reference, the generator can be the only NuGet package reference.

## Usage

Annotate a partial model with `[GeneratePropertyPaths]`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using ProMvvm;

[GeneratePropertyPaths]
public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}
```

The generator creates `SearchViewModelPropertyPaths.SearchText`, which can be used directly with ProMvvm:

```csharp
using var subscription = viewModel
    .WhenAnyValue(SearchViewModelPropertyPaths.SearchText)
    .Subscribe(observer);
```

The generated call site uses ordinary delegates and typed `PropertyPath` instances. It does not reflect, compile expressions, or require dynamic code, so it remains safe for trimming and NativeAOT.

## Package behavior

- The package targets `netstandard2.0` as a Roslyn analyzer.
- It contains no runtime or `lib` assets.
- Generated descriptors require the `ProMvvm` runtime assembly at compile time.
- The generator version is kept in lockstep with the runtime package version.
- Generic target types currently report `PMVVM001`; see the [main README](https://github.com/wieslawsoltes/ProMvvm#usage) for the complete supported surface and roadmap.

## License

ProMvvm.SourceGenerators is licensed under the [MIT License](https://github.com/wieslawsoltes/ProMvvm/blob/main/LICENSE). Copyright © 2026 Wiesław Šoltés.
