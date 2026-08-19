# ProMvvm

[![CI](https://github.com/wieslawsoltes/ProMvvm/actions/workflows/ci.yml/badge.svg)](https://github.com/wieslawsoltes/ProMvvm/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ProMvvm.svg)](https://www.nuget.org/packages/ProMvvm/)
[![NuGet downloads](https://img.shields.io/nuget/dt/ProMvvm.svg)](https://www.nuget.org/packages/ProMvvm/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## NuGet packages

| Package | Current version | Contents | Availability |
| --- | --- | --- | --- |
| [`ProMvvm`](https://www.nuget.org/packages/ProMvvm/) | `0.1.0` | Runtime, `WhenAnyValue` extensions, typed property paths, and notification adapters; installs the generator automatically | NuGet.org |
| [`ProMvvm.SourceGenerators`](https://www.nuget.org/packages/ProMvvm.SourceGenerators/) | `0.1.0` | Compile-time generation of reflection-free property descriptors | NuGet.org |

Installing `ProMvvm` is the recommended path: it brings in the matching `ProMvvm.SourceGenerators` analyzer automatically. The generator is also published separately for advanced build setups that want an explicit analyzer reference. It has no runtime assets, but its generated descriptors reference the `ProMvvm` runtime API.

ProMvvm is a .NET 10-first, high-performance MVVM property-observation library. The `0.1.0` release focuses on `WhenAnyValue`: a cold `IObservable<T>` engine with an AOT-first typed API and a ReactiveUI-compatible expression migration API.

The runtime has no dependency on ReactiveUI, CommunityToolkit.Mvvm, or System.Reactive. It observes any compatible model through `INotifyPropertyChanged` by default, works with both ReactiveUI 24 and CommunityToolkit.Mvvm generated properties, and accepts explicit adapters for other notification mechanisms. Its BCL `IObservable<T>` results compose directly with System.Reactive 7.

See [the architecture guide](docs/ARCHITECTURE.md) for the runtime state machines, typed and expression execution paths, performance decisions, AOT boundary, and design-level comparison with the `ReactiveUI` and `ReactiveUI.Reactive` 24 distributions.

## Getting started

ProMvvm targets .NET 10. Install it from NuGet.org with:

```bash
dotnet add <your-project.csproj> package ProMvvm --version 0.1.0
```

This single reference installs the runtime and the matching source generator. No second package command is needed for normal applications.

System.Reactive is optional. Add it when you want Rx operators such as `Where`, `Select`, and the `Subscribe(Action<T>)` convenience overload:

```bash
dotnet add <your-project.csproj> package System.Reactive --version 7.0.0
```

Annotate a partial model to generate terse, reusable property descriptors. This CommunityToolkit.Mvvm model is one example; ordinary properties and ReactiveUI.SourceGenerators `[Reactive]` fields are supported too:

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

Observe the generated descriptor. Every subscription immediately emits the current value and then emits matching changes:

```csharp
using ProMvvm;
using System.Reactive.Linq;

var viewModel = new SearchViewModel();

using var subscription = viewModel
    .WhenAnyValue(SearchViewModelPropertyPaths.SearchText)
    .Where(static text => !string.IsNullOrWhiteSpace(text))
    .Subscribe(Console.WriteLine);

viewModel.SearchText = "NativeAOT";
```

Generated descriptors are the recommended default: their call sites are strongly typed, reflection-free, trim-safe, and NativeAOT-safe.

## Usage

ProMvvm exposes the same observation shapes through three API styles. Choose typed paths for new code, expressions for source-compatible ReactiveUI migration, and property-name strings only where a migration boundary requires them.

| Observation | Typed/AOT API | Expression API | String-name API |
| --- | --- | --- | --- |
| One property | Generated or handwritten `PropertyPath`, or getter plus name | Yes | Yes |
| Nested path | `PropertyPath.Then(...)` | Yes | No |
| Constant indexer | A typed path using the indexer notification name | Yes | No |
| Single-value projection | Compose with Rx `Select` | Fused selector overload | Fused selector overload |
| Multi-value projection | Arity 2–12 | Arity 2–12 | Arity 2–12 |
| Multi-value tuple | Arity 2–12 | Arity 2–12 | Arity 2–12 |
| Explicit notification adapter | Yes | Yes | No |
| Trimming and NativeAOT | Supported | `RequiresUnreferencedCode` | `RequiresUnreferencedCode` |

### Generated property descriptors

`[GeneratePropertyPaths]` produces a `<ModelName>PropertyPaths` class containing a descriptor for every accessible property:

```csharp
[GeneratePropertyPaths]
public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SearchOptions? _options;

    [ObservableProperty]
    private int _minimumLength = 3;

    public System.Collections.ObjectModel.ObservableCollection<string> Items { get; } =
        ["first", "second"];
}

public partial class SearchOptions : ObservableObject
{
    [ObservableProperty]
    private string _city = string.Empty;
}

var searchText = viewModel.WhenAnyValue(SearchViewModelPropertyPaths.SearchText);
```

The generator understands ordinary properties, CommunityToolkit.Mvvm `[ObservableProperty]` fields, and ReactiveUI.SourceGenerators `[Reactive]` fields. The main package installs the matching analyzer package automatically, and generated descriptors require no runtime reflection.

#### Explicit generator package reference

Most applications should reference only `ProMvvm`. Library authors and advanced build setups can make the analyzer reference explicit—for example, to mark it private or control it independently in central package management:

```xml
<ItemGroup>
  <PackageReference Include="ProMvvm" Version="0.1.0" />
  <PackageReference Include="ProMvvm.SourceGenerators"
                    Version="0.1.0"
                    PrivateAssets="all" />
</ItemGroup>
```

The explicit reference resolves to the same analyzer version already required by `ProMvvm`; NuGet loads it once. `PrivateAssets="all"` is useful in a library when its consumers should not inherit the analyzer. If the runtime is supplied through a project or assembly reference instead of NuGet, `ProMvvm.SourceGenerators` can likewise be installed as the only package reference.

The generator package is compile-time-only: it contains `analyzers/dotnet/cs/ProMvvm.SourceGenerators.dll` and no `lib` or runtime assembly. Generated descriptors still use `ProMvvm.PropertyPath`, so the runtime assembly must be available to the consuming compilation.

### Direct typed getters

For a one-off property, pass an ahead-of-time compiled getter and its notification name:

```csharp
var searchText = viewModel.WhenAnyValue(
    static model => model.SearchText,
    nameof(SearchViewModel.SearchText));
```

Use `isDistinct: false` to emit repeated values, or supply a comparer:

```csharp
var allValues = viewModel.WhenAnyValue(
    static model => model.SearchText,
    nameof(SearchViewModel.SearchText),
    isDistinct: false);

var ordinalIgnoreCase = viewModel.WhenAnyValue(
    SearchViewModelPropertyPaths.SearchText,
    comparer: StringComparer.OrdinalIgnoreCase);
```

### Handwritten and nested typed paths

Create reusable paths when generated descriptors are not appropriate. Append `Then` segments for nested observation:

```csharp
var city = PropertyPath
    .Create<SearchViewModel, SearchOptions?>(
        nameof(SearchViewModel.Options),
        static model => model.Options)
    .Then(
        nameof(SearchOptions.City),
        static options => options!.City);

using var subscription = viewModel.WhenAnyValue(city).Subscribe(Console.WriteLine);
```

Nested paths detach from the old object and attach to the new one whenever an intermediate property changes. A `null` intermediate suppresses output until the chain becomes valid again; a `null` final value is still a valid emission. Two- and three-segment paths use specialized sinks, while longer paths use the general watcher graph.

An indexer is a normal typed segment when its notification name is known:

```csharp
var firstItem = SearchViewModelPropertyPaths.Items
    .Then("Item[]", static items => items[0]);

var firstItemChanges = viewModel.WhenAnyValue(firstItem);
```

### Expression compatibility

ReactiveUI-style expressions are available for gradual migration in ordinary JIT applications:

```csharp
#pragma warning disable IL2026 // ProMvvm expression migration API
var direct = viewModel.WhenAnyValue(model => model.SearchText);
var nested = viewModel.WhenAnyValue(model => model.Options!.City);
var firstItem = viewModel.WhenAnyValue(model => model.Items[0]);
#pragma warning restore IL2026
```

The parser supports property and field chains, inherited properties, array `Length`, and constant indexer arguments such as `[0]` or `["key"]`. It observes `Item[]`, or the type's custom `IndexerName`, and rewires nested indexer paths. Captured or otherwise nonconstant indices and array-element expressions are rejected to preserve ReactiveUI 24 behavior.

Expression overloads are marked `RequiresUnreferencedCode` because they resolve reflected metadata. Cached, exact single-property expressions use a bound getter and the same optimized sink as typed calls after resolution, but typed descriptors remain the correct API for trimming and NativeAOT.

### Property-name compatibility

Public properties can be observed by name for migration code that cannot supply an expression:

```csharp
#pragma warning disable IL2026
var searchText = viewModel.WhenAnyValue<SearchViewModel, string>(
    nameof(SearchViewModel.SearchText));
#pragma warning restore IL2026
```

Property metadata is cached, but string-name overloads are not trim-safe and cannot express nested paths.

### Projections

Typed paths compose naturally with System.Reactive:

```csharp
var length = viewModel
    .WhenAnyValue(SearchViewModelPropertyPaths.SearchText)
    .Select(static text => text.Length);
```

Expression and string-name migration APIs also provide fused arity-1 selector overloads:

```csharp
#pragma warning disable IL2026
var expressionLength = viewModel.WhenAnyValue<SearchViewModel, int, string>(
    model => model.SearchText,
    static text => text.Length);

var stringLength = viewModel.WhenAnyValue<SearchViewModel, int, string>(
    nameof(SearchViewModel.SearchText),
    static text => text.Length);
#pragma warning restore IL2026
```

### Multiple properties and tuples

Typed, expression, and string-name overloads combine 2 through 12 sources. Pass a selector to project the latest values:

```csharp
var canSearch = viewModel.WhenAnyValue(
    SearchViewModelPropertyPaths.SearchText,
    SearchViewModelPropertyPaths.MinimumLength,
    static (text, minimumLength) => text.Length >= minimumLength);

#pragma warning disable IL2026
var expressionCanSearch = viewModel.WhenAnyValue(
    model => model.SearchText,
    model => model.MinimumLength,
    static (text, minimumLength) => text.Length >= minimumLength);

var stringCanSearch = viewModel.WhenAnyValue<
    SearchViewModel, bool, string, int>(
        nameof(SearchViewModel.SearchText),
        nameof(SearchViewModel.MinimumLength),
        static (text, minimumLength) => text.Length >= minimumLength);
#pragma warning restore IL2026
```

Omit the selector to receive a named `ValueTuple`; tuple overloads also cover arities 2 through 12:

```csharp
var typedState = viewModel.WhenAnyValue(
    SearchViewModelPropertyPaths.SearchText,
    SearchViewModelPropertyPaths.MinimumLength);

#pragma warning disable IL2026
var expressionState = viewModel.WhenAnyValue(
    model => model.SearchText,
    model => model.MinimumLength);

var stringState = viewModel.WhenAnyValue<SearchViewModel, string, int>(
    nameof(SearchViewModel.SearchText),
    nameof(SearchViewModel.MinimumLength));
#pragma warning restore IL2026
```

For typed multi-source projections, `isDistinct` controls both input and projected-result distinctness and `comparer` can customize the result comparison. Expression and string compatibility overloads preserve ReactiveUI's input-only distinctness by default; an expression result comparer opts into projected-result distinctness. Use Rx `DistinctUntilChanged` after a string projection when output distinctness is required.

### Notification adapters

By default ProMvvm listens directly to `INotifyPropertyChanged`. Custom event sources and `IObservable<string?>` change streams can be adapted explicitly at the call site without a registry, global provider, or service locator:

```csharp
var customAdapter = PropertyNotificationAdapters.Create<CustomViewModel>(
    static (model, onPropertyChanged) =>
        model.SubscribePropertyChanges(onPropertyChanged));

var changesAdapter = PropertyNotificationAdapters.FromObservable<StreamViewModel>(
    static model => model.ChangedPropertyNames);

var mixedAdapter = PropertyNotificationAdapters.FirstSupported(
    customAdapter,
    changesAdapter,
    PropertyNotificationAdapters.Inpc);

var value = customViewModel.WhenAnyValue(
    static model => model.Value,
    nameof(CustomViewModel.Value),
    mixedAdapter);
```

`SubscribePropertyChanges` in this example is an application method that attaches the custom event and returns an `IDisposable`. `FirstSupported` tries adapters in the supplied order for every object in a nested chain, so different levels can use different notification mechanisms. Single typed and expression observations accept adapters; multi-source typed and expression projections and tuples accept them from arity 2 through 12. String-name overloads do not.

### ReactiveUI and CommunityToolkit.Mvvm models

ProMvvm observes behavior rather than requiring a base class. Add `[GeneratePropertyPaths]` to either source-generator style:

```csharp
// CommunityToolkit.Mvvm
[GeneratePropertyPaths]
public partial class ToolkitViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
}

// ReactiveUI.SourceGenerators; ReactiveObject may come from either ReactiveUI 24 package.
[GeneratePropertyPaths]
public partial class ReactiveViewModel : ReactiveObject
{
    [Reactive] private string _name = string.Empty;
}
```

Both produce `...PropertyPaths.Name`, observed with the same ProMvvm call. See the complete [CommunityToolkit.Mvvm sample](samples/ProMvvm.Sample.CommunityToolkit), [ReactiveUI 24 sample](samples/ProMvvm.Sample.ReactiveUI), and their expression-mode counterparts listed below.

## ProMvvm compared with ReactiveUI usage

ReactiveUI 24 core and `ReactiveUI.Reactive` expose the same expression-oriented `WhenAnyValue` call shapes from different namespaces. ProMvvm expression mode intentionally keeps those shapes familiar, while typed descriptors provide its reflection-free fast path.

| Scenario | ProMvvm | ReactiveUI 24 |
| --- | --- | --- |
| Namespace | `using ProMvvm;` | `using ReactiveUI;` or `using ReactiveUI.Reactive;` |
| Model requirement | Any class using INPC or an explicit local adapter | ReactiveUI object/provider ecosystem, including INPC providers |
| Recommended single property | `vm.WhenAnyValue(ModelPropertyPaths.Name)` | `vm.WhenAnyValue(x => x.Name)` |
| Expression migration | `vm.WhenAnyValue(x => x.Name)` | `vm.WhenAnyValue(x => x.Name)` |
| Nested property | Typed `.Then(...)` or `vm.WhenAnyValue(x => x.Child.Name)` | `vm.WhenAnyValue(x => x.Child.Name)` |
| Projection | Typed observation plus `.Select(...)`, or compatible expression selector | Expression selector |
| Multiple values | Typed paths, expressions, or names through arity 12 | Expressions or names through arity 12 |
| Tuple output | Built-in typed, expression, and name overloads through arity 12 | Use the available ReactiveUI overload or project a tuple |
| Constant indexer | Typed `Item[]` path or compatible expression | Expression |
| Notification extension | Explicit per-call adapter | Globally configured observable-for-property providers/services |
| Trimming/NativeAOT | Typed APIs and generated descriptors are supported | Expression/provider pipeline is not ProMvvm's AOT-safe path |
| Observable type | BCL `IObservable<T>`; System.Reactive is optional | System.Reactive-based observable pipeline |

The simplest expression migration is usually a namespace change:

```csharp
// ReactiveUI
using ReactiveUI;

var oldStream = viewModel.WhenAnyValue(model => model.SearchText);
```

becomes:

```csharp
// ProMvvm expression compatibility
using ProMvvm;

#pragma warning disable IL2026
var migratedStream = viewModel.WhenAnyValue(model => model.SearchText);
#pragma warning restore IL2026
```

For new ProMvvm code, replace the expression with a generated descriptor:

```csharp
var optimizedStream = viewModel.WhenAnyValue(
    SearchViewModelPropertyPaths.SearchText);
```

When both libraries are referenced, importing both extension namespaces can make identical expression overloads ambiguous. During incremental migration, call the ProMvvm extension explicitly or use the unambiguous typed getter/descriptor form:

```csharp
#pragma warning disable IL2026
var proExpression = ProMvvm.WhenAnyValueExtensions.WhenAnyValue(
    viewModel,
    model => model.SearchText);
#pragma warning restore IL2026

var proTyped = viewModel.WhenAnyValue(
    static model => model.SearchText,
    nameof(SearchViewModel.SearchText));
```

ProMvvm does not require ReactiveUI builder initialization or global service configuration. It is currently a focused `WhenAnyValue` implementation, not a replacement for ReactiveUI binding, commands, activation, routing, or the rest of the framework.

## Runtime semantics

- Emits the current value synchronously for each subscription.
- Produces a cold observable with independent subscriptions.
- Observes nested `INotifyPropertyChanged` chains and rewires when an intermediate object changes.
- Suppresses output while an intermediate object is `null`; a `null` final value remains valid.
- Applies final-value distinctness by default for single and typed paths, including across temporarily invalid nested chains.
- Supports custom equality comparers and disabling distinctness.
- Supports a ReactiveUI-compatible arity-1 selector plus typed, expression, and string-name selector overloads through arity 12.
- Supports typed, expression, and string-name tuple overloads from arity 2 through arity 12.
- Supports constant-argument expression indexers, custom indexer notification names, nested rewiring, and ReactiveUI-compatible array-length/error behavior.
- Accepts explicit notification adapters for custom events or `IObservable<string?>` change streams, including mixed object chains, without global registration.
- Serializes model event handling and observer notification per subscription.
- Detaches all handlers on disposal or terminal getter/selector error.

## Samples

- [ReactiveUI 24 typed sample](samples/ProMvvm.Sample.ReactiveUI) uses generated ProMvvm descriptors with `ReactiveUI.Reactive` 24.0.0 and `ReactiveUI.SourceGenerators` 3.2.0.
- [CommunityToolkit.Mvvm typed sample](samples/ProMvvm.Sample.CommunityToolkit) uses generated ProMvvm descriptors with `[ObservableProperty]` properties.
- [ReactiveUI 24 expression sample](samples/ProMvvm.Sample.ReactiveUI.Expression) demonstrates direct, nested, fused single-selector, arity-2/3 selector, and tuple expression observations.
- [CommunityToolkit.Mvvm expression sample](samples/ProMvvm.Sample.CommunityToolkit.Expression) demonstrates the same expression migration surface on Toolkit-generated properties.

The typed samples are trim-safe and NativeAOT-safe. The expression samples intentionally target ordinary JIT applications and keep the `RequiresUnreferencedCode` boundary visible at their call sites.

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
dotnet test tests/ProMvvm.ReactiveUiCoreIntegrationTests -c Release

dotnet publish tests/ProMvvm.AotSmoke -c Release -r osx-arm64 --self-contained
./tests/ProMvvm.AotSmoke/bin/Release/net10.0/osx-arm64/publish/ProMvvm.AotSmoke
```

Release unit tests enforce 100% line coverage, at least 98% branch coverage, and 100% method coverage. Dedicated generator tests validate emitted descriptors and overloads; integration tests compile and execute both source-generator ecosystems.

CI builds and tests on Linux, Windows, and macOS, and full-trim and NativeAOT smoke executables run on all three platforms. Both framework samples are also NativeAOT-published and executed. Package smoke tests restore from the freshly built local feed and validate both automatic generator installation through `ProMvvm` and an explicit `ProMvvm.SourceGenerators` reference.

## Benchmarks

The BenchmarkDotNet suite compares typed, expression, and direct string-name ProMvvm paths with both ReactiveUI 24 distributions under the same notification models:

1. ProMvvm typed/AOT paths.
2. ProMvvm expression compatibility paths.
3. ReactiveUI 24 core expression paths.
4. ReactiveUI.Reactive 24 expression paths.

It covers cold construction, warmed end-to-end hot start, generated-descriptor hot start, direct and projected string-name calls, fused single selectors, subscribe + initial emission + disposal, leaf emission, burst throughput (1, 100, and 10,000 changes), two- and three-segment hot start, nested leaf emission and rewiring, constant-indexer hot start/emission/rewiring, arity-2 and arity-12 projection, explicit-adapter overhead, and allocation for every case.

Representative results from the latest verified Apple M3 Pro run on .NET 10 are shown below. Values are mean time / managed allocation per operation; benchmark results vary by machine and runtime.

| Scenario | ProMvvm typed | ProMvvm expression | ReactiveUI core | ReactiveUI.Reactive |
| --- | ---: | ---: | ---: | ---: |
| Hot start | 45.194 ns / 232 B | 244.856 ns / 792 B | 362.789 ns / 1,440 B | 373.471 ns / 1,440 B |
| Single emission | 8.413 ns / 24 B | 10.261 ns / 24 B | 27.458 ns / 88 B | 27.490 ns / 88 B |
| Two-property selector | 23.75 ns / 24 B | 25.35 ns / 24 B | 47.71 ns / 88 B | 47.53 ns / 88 B |
| Two-segment hot start | 63.93 ns / 352 B | 335.99 ns / 1,104 B | 611.36 ns / 2,152 B | 620.88 ns / 2,152 B |
| Twelve-property selector | 206.7 ns / 24 B | 226.9 ns / 24 B | 552.3 ns / 792 B | 553.8 ns / 792 B |

```bash
dotnet run --project benchmarks/ProMvvm.Benchmarks -c Release -- --filter '*'
```

See [benchmarks/README.md](benchmarks/README.md) for the complete result tables, environment metadata, focused commands, and measurement rules.

## Project status

`ProMvvm` `0.1.0` is the first public release and completes the planned `WhenAnyValue` compatibility milestone.

| Area | `0.1.0` status |
| --- | --- |
| Packages | Dependency-free `ProMvvm` runtime plus a compile-time-only `ProMvvm.SourceGenerators` analyzer package; the runtime package installs the analyzer automatically |
| Typed API | Generated and handwritten paths, direct getters, nested paths, selectors, and tuples through arity 12 |
| Migration API | ReactiveUI-compatible expression and string-name observation, including selectors and tuples through arity 12 |
| Notifications | Direct INPC plus explicit custom-event and observable-stream adapters; no service locator |
| Frameworks | Validated with CommunityToolkit.Mvvm, `ReactiveUI` 24, and `ReactiveUI.Reactive` 24 |
| Deployment | Trim-safe and NativeAOT-safe typed surface, validated on Linux, Windows, and macOS |
| Quality | Concurrent stress tests, generator/integration suites, 100% line and method coverage, and at least 98% branch coverage |
| Performance | BenchmarkDotNet coverage for cold construction, hot start, emissions, bursts, nested rewiring, indexers, adapters, and multi-source sinks |

The release includes the performance-driven specializations developed for direct typed observation, cached expression and string compatibility, two- and three-segment nested paths, constant indexers, fused arity-1 selectors, and multi-source sinks. The public compatibility namespace is `ProMvvm`, so both libraries can remain referenced during gradual migration. Generated descriptors and the getter + property-name overload avoid extension ambiguity when ReactiveUI is also imported.

The current scope is deliberately narrower than the whole ReactiveUI framework. Binding, commands, activation, routing, before-change observation, arbitrary dynamic paths, and nonconstant index expressions are not part of `0.1.0`.

### Next parity work

Future milestones will be evaluated in this order, with compatibility tests and benchmark evidence required for each addition:

1. Before-change observation and the remaining property-observation shapes needed for broader ReactiveUI migration.
2. Typed, descriptor-driven binding APIs that preserve the trimming and NativeAOT guarantees of the current core.
3. Source-generator support for more generic, nested, and inherited model shapes with deterministic generated names.
4. Additional platform execution coverage for mobile and browser targets as .NET 10 runners and NativeAOT support permit.
5. ReactiveUI source-generated observation benchmarks and new specializations only where end-to-end measurements justify their runtime and code-size cost.

Commands, activation, and routing remain longer-term compatibility areas rather than commitments for the next release.

## License

ProMvvm is licensed under the [MIT License](LICENSE). Copyright © 2026 Wiesław Šoltés.
