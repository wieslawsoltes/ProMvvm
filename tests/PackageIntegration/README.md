# Local NuGet package integration matrix

These projects validate the packages that users install, rather than project references to `src/`. Each dependency graph has its own project so framework packages, analyzers, and extension methods cannot leak between combinations.

| Project | ProMvvm package mode | Framework | Feature surface |
| --- | --- | --- | --- |
| `Plain.Typed` | `ProMvvm` with automatic analyzer | Plain `INotifyPropertyChanged` | Generated direct and nested paths, multi-source typed selector |
| `Plain.Expression` | `ProMvvm` with automatic analyzer | Plain `INotifyPropertyChanged` | Direct, property-name, nested, and multi-source migration APIs |
| `NotificationAdapters` | `ProMvvm` with automatic analyzer | Custom event source | Explicit notification adapter without `INotifyPropertyChanged` or a service locator |
| `SourceGenerators.Plain` | Explicit `ProMvvm.SourceGenerators` | Plain `INotifyPropertyChanged` | Generated descriptors and generated arity-12 overload |
| `CommunityToolkit.Typed` | `ProMvvm` with automatic analyzer | CommunityToolkit.Mvvm | Generated direct and nested paths, multi-source typed selector |
| `CommunityToolkit.Expression` | `ProMvvm` with automatic analyzer | CommunityToolkit.Mvvm | Direct, nested, and multi-source expression APIs |
| `SourceGenerators.CommunityToolkit` | Explicit `ProMvvm.SourceGenerators` | CommunityToolkit.Mvvm | Discovery of Toolkit `[ObservableProperty]` fields |
| `ReactiveUIReactive.Typed` | `ProMvvm` with automatic analyzer | ReactiveUI.Reactive 24 | Generated direct and nested paths, multi-source typed selector |
| `ReactiveUIReactive.Expression` | `ProMvvm` with automatic analyzer | ReactiveUI.Reactive 24 | Expression parity, coexistence, nested rewiring, and multi-source selector |
| `SourceGenerators.ReactiveUI` | Explicit `ProMvvm.SourceGenerators` | ReactiveUI.Reactive 24 | Discovery of ReactiveUI.SourceGenerators `[Reactive]` fields |
| `ReactiveUICore.Typed` | `ProMvvm` with automatic analyzer | ReactiveUI core 24 | Generated direct and nested paths, multi-source typed selector |
| `ReactiveUICore.Expression` | `ProMvvm` with automatic analyzer | ReactiveUI core 24 | Expression parity, coexistence, nested rewiring, and multi-source selector |

The explicit-generator projects use `ProMvvm.SourceGenerators` as their only ProMvvm package reference. Their runtime assembly reference is extracted from the freshly built local `ProMvvm.nupkg`, which proves the analyzer package works separately without falling back to a source-tree build output.

Run the complete matrix from the repository root:

```bash
./eng/test-local-packages.sh Release
```

The command packs both projects, validates package layout, restores every integration project, verifies from NuGet's `.nupkg.metadata` that the ProMvvm packages came from the local feed, and runs all tests.

Pass one or more project paths after the configuration to run a focused subset while developing:

```bash
./eng/test-local-packages.sh Release \
  tests/PackageIntegration/CommunityToolkit.Typed/ProMvvm.PackageIntegration.CommunityToolkit.Typed.csproj
```

To test packages that are already packed, provide their directory and version:

```bash
./eng/run-package-integrations.sh artifacts/packages 0.1.0
```

The projects are intentionally excluded from `ProMvvm.slnx`: an ordinary solution build uses project references, while this matrix must restore from a freshly packed local feed. Normal and release CI invoke the scripts explicitly.
