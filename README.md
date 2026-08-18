# ProMvvm

ProMvvm is an experimental, .NET 10-first replacement path for ReactiveUI MVVM functionality. The first implemented slice is `WhenAnyValue`: a cold, dependency-free `IObservable<T>` engine for `INotifyPropertyChanged` models, with an AOT-first typed API and a ReactiveUI-compatible expression migration API.

The runtime package has no dependency on ReactiveUI, CommunityToolkit.Mvvm, or System.Reactive. Its BCL `IObservable<T>` results compose directly with System.Reactive 7, while its notification boundary works with both ReactiveUI 24 generated properties and CommunityToolkit.Mvvm generated properties.

See [the architecture guide](docs/ARCHITECTURE.md) for the runtime state machines, typed and expression execution paths, performance decisions, AOT boundary, and a design-level comparison with both ReactiveUI 24 distributions.

## AOT-first API

For one property, pass an ahead-of-time compiled getter and the notification name:

```csharp
using ProMvvm;
using System.Reactive.Linq;

using var subscription = viewModel
    .WhenAnyValue(static model => model.SearchText, nameof(viewModel.SearchText))
    .Where(static text => !string.IsNullOrWhiteSpace(text))
    .Subscribe(Console.WriteLine);
```

Use `PropertyPath` for reusable and nested paths:

```csharp
var city = PropertyPath
    .Create<PersonViewModel, Address?>(nameof(PersonViewModel.Address), static x => x.Address)
    .Then(nameof(Address.City), static x => x!.City);

using var subscription = viewModel.WhenAnyValue(city).Subscribe(Console.WriteLine);
```

These overloads use only normal delegates and property-change events. They do not reflect, compile expressions, or require dynamic code.
Single-property plus two- and three-segment typed paths use specialized generic sinks that avoid value boxing and the general watcher graph on their steady-state notification paths.

Annotate a model to generate reusable descriptors for every accessible property:

```csharp
[GeneratePropertyPaths]
public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}

using var subscription = viewModel
    .WhenAnyValue(SearchViewModelPropertyPaths.SearchText)
    .Subscribe(Console.WriteLine);
```

The generator recognizes ordinary properties, CommunityToolkit.Mvvm `[ObservableProperty]` fields, and ReactiveUI.SourceGenerators `[Reactive]` fields. The generated call site is terse, reusable, reflection-free, trim-safe, and NativeAOT-safe.

For gradual migration, expression syntax is also available:

```csharp
#pragma warning disable IL2026 // migration compatibility path
using var subscription = viewModel.WhenAnyValue(x => x.Address!.City)
    .Subscribe(Console.WriteLine);
#pragma warning restore IL2026
```

Expression overloads are intentionally marked `RequiresUnreferencedCode`. Exact single-property expressions cache their resolved metadata and use a bound getter with the same specialized sink as typed paths, so steady-state notifications do not reflect. Other compatible expression shapes use the general reflected path engine. Use typed paths in trimming or NativeAOT applications.

## Current semantics

- Emits the current value synchronously for each subscription.
- Produces a cold observable with independent subscriptions.
- Observes nested `INotifyPropertyChanged` chains and rewires when an intermediate object changes.
- Suppresses output while an intermediate object is `null`; a `null` final value remains valid.
- Applies final-value distinctness by default, including across temporarily invalid nested chains.
- Supports custom equality comparers and disabling distinctness.
- Supports typed and expression selector and tuple overloads from arity 2 through arity 12.
- Accepts explicit notification adapters for custom events or `IObservable<string?>` change streams, including mixed object chains, without global registration.
- Serializes model event handling and observer notification per subscription.
- Detaches all handlers on disposal or terminal getter/selector error.

## Compatibility samples

- [ReactiveUI 24 typed sample](samples/ProMvvm.Sample.ReactiveUI) uses generated ProMvvm descriptors with `ReactiveUI.Reactive` 24.0.0 and `ReactiveUI.SourceGenerators` 3.2.0.
- [CommunityToolkit.Mvvm typed sample](samples/ProMvvm.Sample.CommunityToolkit) uses generated ProMvvm descriptors with `[ObservableProperty]` properties.
- [ReactiveUI 24 expression sample](samples/ProMvvm.Sample.ReactiveUI.Expression) demonstrates direct, nested, selector, and tuple expression observations.
- [CommunityToolkit.Mvvm expression sample](samples/ProMvvm.Sample.CommunityToolkit.Expression) demonstrates the same expression migration surface on Toolkit-generated properties.

The typed samples are the trim-safe and NativeAOT-safe examples. The expression samples intentionally target ordinary JIT applications and keep the `RequiresUnreferencedCode` boundary visible at their call sites.

Run them with:

```bash
dotnet run --project samples/ProMvvm.Sample.ReactiveUI -c Release
dotnet run --project samples/ProMvvm.Sample.CommunityToolkit -c Release
dotnet run --project samples/ProMvvm.Sample.ReactiveUI.Expression -c Release
dotnet run --project samples/ProMvvm.Sample.CommunityToolkit.Expression -c Release
```

## Verification

```bash
dotnet build ProMvvm.slnx -c Release
dotnet test tests/ProMvvm.Tests -c Release
dotnet test tests/ProMvvm.SourceGenerators.Tests -c Release
dotnet test tests/ProMvvm.IntegrationTests -c Release

dotnet publish tests/ProMvvm.AotSmoke -c Release -r osx-arm64 --self-contained
./tests/ProMvvm.AotSmoke/bin/Release/net10.0/osx-arm64/publish/ProMvvm.AotSmoke
```

Release unit tests enforce 100% line coverage, at least 98% branch coverage, and 100% method coverage. Dedicated generator tests validate emitted descriptors and overloads; integration tests compile and execute both source-generator ecosystems.
CI builds and tests on Linux, Windows, and macOS, and full-trim and NativeAOT smoke executables run on all three platforms. Both framework samples are also NativeAOT-published and executed.

## Benchmarks

The BenchmarkDotNet suite compares four paths under the same notification models:

1. ProMvvm typed/AOT paths.
2. ProMvvm expression compatibility paths.
3. ReactiveUI 24 core expression paths.
4. ReactiveUI.Reactive 24 expression paths.

It covers cold construction, warmed end-to-end hot start, generated-descriptor hot start, subscribe + initial emission + disposal, leaf emission, burst throughput (1, 100, and 10,000 changes), two- and three-segment hot start, nested leaf emission and rewiring, arity-2 and arity-12 projection, explicit-adapter overhead, and allocation for every case.

```bash
dotnet run --project benchmarks/ProMvvm.Benchmarks -c Release -- --filter '*'
```

See [benchmarks/README.md](benchmarks/README.md) for focused commands and measurement rules.

## Status and next parity work

The current compatibility milestone includes generated arity-2-through-12 APIs and specialized sinks, generated property descriptors, explicit notification adapters, two- and three-segment specialization, inherited-property expression fast paths, concurrent stress coverage, and cross-platform trim/NativeAOT matrices. It remains a focused `WhenAnyValue` implementation rather than a complete ReactiveUI replacement; binding, commands, activation, routing, before-change observation, and string/dynamic paths remain outside the current slice.

The compatibility namespace is `ProMvvm`, so both libraries may be referenced during gradual migration. The explicit getter + property-name overload also avoids extension ambiguity when `ReactiveUI.Reactive` is imported.
