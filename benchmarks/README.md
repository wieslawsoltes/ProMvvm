# ProMvvm performance suite

All benchmarks use BenchmarkDotNet, .NET 10, the same hand-written `INotifyPropertyChanged` model, the same observer shape, and Release builds. Both ReactiveUI 24 distributions are initialized once through their required `RxAppBuilder` paths before their observations are created. `ReactiveUI` is the optimized core distribution; `ReactiveUI.Reactive` is the System.Reactive-compatible distribution.

## Matrix

| Area | Work measured | Compared implementations |
|---|---|---|
| Construction | Create a cold observable without subscribing | typed path, typed getter, expression, ReactiveUI core, ReactiveUI.Reactive |
| Hot start | Create a warmed observable, subscribe, receive its initial value, and dispose | typed path, typed getter, generated descriptor, expression, ReactiveUI core, ReactiveUI.Reactive |
| Subscription | Subscribe, receive initial value, dispose | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Leaf emission | One already-subscribed property change | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Burst | 1, 100, or 10,000 property changes | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested leaf | Change a leaf on an established two-level chain | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested rewire | Replace the intermediate object and rewire handlers | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested hot start | Construct, subscribe, initialize, and dispose a two-segment path | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment hot start | Construct, subscribe, initialize, and dispose a three-segment path | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment leaf | Change a leaf on an established three-segment chain | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment rewire | Replace the final parent and rewire only the affected suffix | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Multi-property | Change one input of an arity-2 selector | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Arity-12 emission | Notify twelve subscribed inputs and project each latest set | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Arity-12 hot start | Create, subscribe, synchronously initialize twelve inputs, and dispose | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Notification adapter | Change one property through direct INPC or the explicit INPC adapter | direct INPC, explicit adapter |

`MemoryDiagnoser` is enabled for every benchmark. ProMvvm typed methods are the baseline within each group.

## Latest verified results

Measured on 2026-08-18 with BenchmarkDotNet 0.15.8, .NET 10.0.5, macOS 26.6, and an Apple M3 Pro. Both ReactiveUI distributions are version 24.0.0. Each cell is mean time / managed allocation per operation.

| Scenario | ProMvvm typed | ProMvvm expression | ReactiveUI core | ReactiveUI.Reactive |
|---|---:|---:|---:|---:|
| Cold construction | 7.580 ns / 56 B (path); 6.596 ns / 56 B (getter) | 191.180 ns / 616 B | 214.658 ns / 712 B | 214.934 ns / 712 B |
| Hot start | 41.51 ns / 232 B (path); 33.15 ns / 176 B (getter); 41.45 ns / 232 B (generated) | 218.75 ns / 792 B | 325.76 ns / 1,440 B | 327.12 ns / 1,440 B |
| Subscribe + initial value + dispose | 35.37 ns / 176 B | 38.31 ns / 176 B | 126.97 ns / 728 B | 132.56 ns / 728 B |
| Single emission | 8.783 ns / 24 B | 11.264 ns / 24 B | 29.214 ns / 88 B | 29.234 ns / 88 B |
| Burst: 1 change | 9.565 ns / 24 B | 11.200 ns / 24 B | 24.933 ns / 48 B | 25.083 ns / 48 B |
| Burst: 100 changes | 0.974 us / 2,400 B | 1.173 us / 2,400 B | 3.035 us / 8,800 B | 2.929 us / 8,800 B |
| Burst: 10,000 changes | 99.014 us / 240,000 B | 117.010 us / 240,000 B | 293.277 us / 880,000 B | 292.396 us / 880,000 B |
| Two-property selector | 23.75 ns / 24 B | 25.35 ns / 24 B | 47.71 ns / 88 B | 47.53 ns / 88 B |
| Twelve-property selector | 206.7 ns / 24 B | 226.9 ns / 24 B | 552.3 ns / 792 B | 553.8 ns / 792 B |
| Twelve-property hot start | 1.220 us / 5.95 KB | 3.457 us / 12.51 KB | 4.773 us / 21.95 KB | 4.859 us / 21.95 KB |
| Two-segment hot start | 63.93 ns / 352 B | 335.99 ns / 1,104 B | 611.36 ns / 2,152 B | 620.88 ns / 2,152 B |
| Nested leaf change | 9.940 ns / 24 B | 19.166 ns / 48 B | 31.295 ns / 88 B | 31.174 ns / 88 B |
| Nested rewire | 17.67 ns / 24 B | 29.50 ns / 48 B | 102.61 ns / 376 B | 106.67 ns / 376 B |
| Three-segment hot start | 84.81 ns / 472 B | 424.30 ns / 1,392 B | 880.31 ns / 2,864 B | 882.81 ns / 2,864 B |
| Three-segment leaf change | 8.796 ns / 24 B | 16.655 ns / 48 B | 29.244 ns / 88 B | 28.998 ns / 88 B |
| Three-segment rewire | 20.055 ns / 24 B | 30.511 ns / 48 B | 109.761 ns / 376 B | 106.872 ns / 376 B |

The explicit-adapter emission result is 8.330 ns / 24 B versus 8.344 ns / 24 B for direct INPC, which is indistinguishable at this measurement resolution.

Hot start means a warmed process and expression-path cache, while still including each operation's observable construction, runtime expression-tree construction where applicable, subscription, synchronous initial value, and disposal. It does not mean a shared hot observable.

Before the three-segment specialization, the same leaf benchmark measured 17.33 ns / 48 B typed and 23.06 ns / 48 B expression; rewiring measured 30.23 ns / 48 B typed and 39.11 ns / 48 B expression. The post-change rows therefore show approximately 49% faster typed leaf delivery, 28% faster expression leaf delivery, 34% faster typed rewiring, and 22% faster expression rewiring. Typed notification allocation fell from 48 B to 24 B in both cases.

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
