# ProMvvm performance suite

All benchmarks use BenchmarkDotNet, .NET 10, the same hand-written `INotifyPropertyChanged` model, the same observer shape, and Release builds. Both ReactiveUI 24 distributions are initialized once through their required `RxAppBuilder` paths before their observations are created. `ReactiveUI` is the optimized core distribution; `ReactiveUI.Reactive` is the System.Reactive-compatible distribution.

## Matrix

| Area | Work measured | Compared implementations |
|---|---|---|
| Construction | Create a cold observable without subscribing | typed path, typed getter, expression, ReactiveUI core, ReactiveUI.Reactive |
| Hot start | Create a warmed observable, subscribe, receive its initial value, and dispose | typed path, typed getter, expression, ReactiveUI core, ReactiveUI.Reactive |
| Subscription | Subscribe, receive initial value, dispose | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Leaf emission | One already-subscribed property change | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Burst | 1, 100, or 10,000 property changes | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested leaf | Change a leaf on an established two-level chain | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested rewire | Replace the intermediate object and rewire handlers | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Multi-property | Change one input of a two-property selector | typed, expression, ReactiveUI core, ReactiveUI.Reactive |

`MemoryDiagnoser` is enabled for every benchmark. ProMvvm typed methods are the baseline within each group.

## Latest verified results

Measured on 2026-08-18 with BenchmarkDotNet 0.15.8, .NET 10.0.5, macOS 26.6, and an Apple M3 Pro. Both ReactiveUI distributions are version 24.0.0. Each cell is mean time / managed allocation per operation.

| Scenario | ProMvvm typed | ProMvvm expression | ReactiveUI core | ReactiveUI.Reactive |
|---|---:|---:|---:|---:|
| Cold construction | 7.580 ns / 56 B (path); 6.596 ns / 56 B (getter) | 191.180 ns / 616 B | 214.658 ns / 712 B | 214.934 ns / 712 B |
| Hot start | 46.47 ns / 232 B (path); 38.44 ns / 176 B (getter) | 242.35 ns / 792 B | 367.17 ns / 1,440 B | 386.74 ns / 1,440 B |
| Subscribe + initial value + dispose | 35.37 ns / 176 B | 38.31 ns / 176 B | 126.97 ns / 728 B | 132.56 ns / 728 B |
| Single emission | 8.783 ns / 24 B | 11.264 ns / 24 B | 29.214 ns / 88 B | 29.234 ns / 88 B |
| Burst: 1 change | 9.565 ns / 24 B | 11.200 ns / 24 B | 24.933 ns / 48 B | 25.083 ns / 48 B |
| Burst: 100 changes | 0.974 us / 2,400 B | 1.173 us / 2,400 B | 3.035 us / 8,800 B | 2.929 us / 8,800 B |
| Burst: 10,000 changes | 99.014 us / 240,000 B | 117.010 us / 240,000 B | 293.277 us / 880,000 B | 292.396 us / 880,000 B |
| Two-property selector | 25.01 ns / 24 B | 26.74 ns / 24 B | 50.55 ns / 88 B | 51.00 ns / 88 B |
| Nested leaf change | 16.69 ns / 48 B | 21.83 ns / 48 B | 28.81 ns / 88 B | 28.90 ns / 88 B |
| Nested rewire | 28.98 ns / 48 B | 39.40 ns / 48 B | 106.69 ns / 376 B | 107.70 ns / 376 B |

Hot start means a warmed process and expression-path cache, while still including each operation's observable construction, runtime expression-tree construction where applicable, subscription, synchronous initial value, and disposal. It does not mean a shared hot observable.

List all cases without running them:

```bash
dotnet run --project benchmarks/ProMvvm.Benchmarks -c Release -- --list flat
```

Run one group:

```bash
dotnet run --project benchmarks/ProMvvm.Benchmarks -c Release -- \
  --filter '*SubscriptionBenchmarks*'
```

Run the complete comparison and export reports:

```bash
dotnet run --project benchmarks/ProMvvm.Benchmarks -c Release -- --filter '*'
```

Run groups separately when comparing ratios. BenchmarkDotNet's `--join` option uses one baseline across the joined report, so its displayed ratio column is not normalized independently for each benchmark class.

Do not compare results collected with a debugger, different power modes, or other active workloads. Commit the generated Markdown report only when the machine/runtime metadata and the exact package versions are recorded with it.
