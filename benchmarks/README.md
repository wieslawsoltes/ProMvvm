# ProMvvm performance suite

All benchmarks use BenchmarkDotNet, .NET 10, the same hand-written `INotifyPropertyChanged` model, the same observer shape, and Release builds. Both ReactiveUI 24 distributions are initialized once through their required `RxAppBuilder` paths before their observations are created. `ReactiveUI` is the optimized core distribution; `ReactiveUI.Reactive` is the System.Reactive-compatible distribution.

## Matrix

| Area | Work measured | Compared implementations |
|---|---|---|
| Construction | Create a cold observable without subscribing | typed path, typed getter, expression, direct string name, ReactiveUI core, ReactiveUI.Reactive |
| Hot start | Create a warmed observable, subscribe, receive its initial value, and dispose | typed path, typed getter, generated descriptor, expression, direct string name, ReactiveUI core, ReactiveUI.Reactive |
| Subscription | Subscribe, receive initial value, dispose | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Leaf emission | One already-subscribed property change | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Burst | 1, 100, or 10,000 property changes | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested leaf | Change a leaf on an established two-level chain | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested rewire | Replace the intermediate object and rewire handlers | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested hot start | Construct, subscribe, initialize, and dispose a two-segment path | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment hot start | Construct, subscribe, initialize, and dispose a three-segment path | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment leaf | Change a leaf on an established three-segment chain | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Three-segment rewire | Replace the final parent and rewire only the affected suffix | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Direct indexer hot start | Construct, subscribe, read a constant index, and dispose | typed equivalent, expression, ReactiveUI core, ReactiveUI.Reactive |
| Direct indexer emission | Raise `Item[]` on an established observation | typed equivalent, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested indexer hot start | Start a property-plus-indexer chain | typed equivalent, expression, ReactiveUI core, ReactiveUI.Reactive |
| Nested indexer emission/rewire | Raise the nested index or replace its owner | typed equivalent, expression, ReactiveUI core, ReactiveUI.Reactive |
| Indexer invocation primitive | Invoke the same getter | `PropertyInfo.GetValue`, .NET 10 `MethodInvoker`, bound delegate |
| Multi-property | Change one input of an arity-2 selector | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Single selector | Project one expression/string property during hot start or an established change | expression and string APIs in ProMvvm, ReactiveUI core, and ReactiveUI.Reactive |
| Direct string name | Construct, hot-start, or notify one public property resolved by name | ProMvvm, ReactiveUI core, ReactiveUI.Reactive |
| Arity-12 emission | Notify twelve subscribed inputs and project each latest set | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Arity-12 hot start | Create, subscribe, synchronously initialize twelve inputs, and dispose | typed, expression, ReactiveUI core, ReactiveUI.Reactive |
| Notification adapter | Change one property through direct INPC or the explicit INPC adapter | direct INPC, explicit adapter |

`MemoryDiagnoser` is enabled for every benchmark. ProMvvm typed methods are the baseline within each group.

## Latest verified results

Measured on 2026-08-18 with BenchmarkDotNet 0.15.8, .NET 10.0.5, macOS 26.6, and an Apple M3 Pro. Both ReactiveUI distributions are version 24.0.0. Each cell is mean time / managed allocation per operation.

| Scenario | ProMvvm typed | ProMvvm expression | ReactiveUI core | ReactiveUI.Reactive |
|---|---:|---:|---:|---:|
| Cold construction | 7.128 ns / 56 B (path); 6.220 ns / 56 B (getter) | 180.445 ns / 616 B | 201.745 ns / 712 B | 197.418 ns / 712 B |
| Hot start | 45.194 ns / 232 B (path); 36.128 ns / 176 B (getter); 44.567 ns / 232 B (generated) | 244.856 ns / 792 B | 362.789 ns / 1,440 B | 373.471 ns / 1,440 B |
| Subscribe + initial value + dispose | 35.37 ns / 176 B | 38.31 ns / 176 B | 126.97 ns / 728 B | 132.56 ns / 728 B |
| Single emission | 8.413 ns / 24 B | 10.261 ns / 24 B | 27.458 ns / 88 B | 27.490 ns / 88 B |
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
| Direct constant-indexer hot start | 42.857 ns / 232 B | 257.366 ns / 888 B | 464.584 ns / 1,736 B | 461.893 ns / 1,736 B |
| Direct constant-indexer emission | 9.499 ns / 24 B | 11.416 ns / 24 B | 31.944 ns / 88 B | 32.085 ns / 88 B |
| Nested constant-indexer hot start | 64.265 ns / 352 B | 430.202 ns / 1,424 B | 767.082 ns / 2,456 B | 778.810 ns / 2,456 B |
| Nested constant-indexer emission | 8.914 ns / 24 B | 17.420 ns / 48 B | 33.388 ns / 88 B | 33.098 ns / 88 B |
| Nested constant-indexer rewire | 18.861 ns / 24 B | 30.728 ns / 48 B | 120.223 ns / 376 B | 114.236 ns / 376 B |

The compatibility additions use separate rows because the typed column has no reflection-based string-name equivalent. These were measured in the same post-optimization run and environment:

| Scenario | ProMvvm | ReactiveUI core | ReactiveUI.Reactive |
|---|---:|---:|---:|
| Direct string cold construction | 8.250 ns / 56 B | 87.511 ns / 240 B | 88.096 ns / 240 B |
| Direct string hot start | 49.528 ns / 232 B | 178.831 ns / 616 B | 201.444 ns / 616 B |
| Direct string emission | 8.969 ns / 24 B | 23.08 ns / 88 B | 22.61 ns / 88 B |
| Expression single-selector hot start | 214.18 ns / 808 B | 331.07 ns / 1,528 B | 328.91 ns / 1,528 B |
| Expression single-selector emission | 10.337 ns / 24 B | 27.794 ns / 88 B | 27.699 ns / 88 B |
| String single-selector hot start | 43.88 ns / 248 B | 154.86 ns / 616 B | 158.74 ns / 616 B |
| String single-selector emission | 9.248 ns / 24 B | 23.401 ns / 88 B | 22.310 ns / 88 B |

The direct-property selector specialization was benchmark-driven. Before fusion, ProMvvm measured 270.08 ns / 872 B for the expression hot start and 71.23 ns / 312 B for the string hot start. The retained specialization reduced those to 214.18 ns / 808 B (about 20.7% faster) and 43.88 ns / 248 B (about 38.4% faster), while the post-change steady-state rows remained at the model event's 24-byte allocation floor.

The constant-indexer implementation was optimized in the same way. A structural last-entry cache removed repeated argument-array construction; bound delegates specialize the common `int`, `string`, and `(int,int)` signatures; other signatures use .NET 10 `MethodInvoker` with fixed arity through four arguments and a span fallback above that. Direct expression hot start improved from 281.131 ns / 976 B to 257.366 ns / 888 B, while direct expression emission improved from 17.843 ns / 48 B to 11.416 ns / 24 B. The isolated invocation measurements were 9.371 ns / 24 B for `PropertyInfo.GetValue`, 5.845 ns / 24 B for `MethodInvoker`, and 0.257 ns / 0 B for a bound delegate, which is why the implementation uses specialization first and `MethodInvoker` only as the general fallback.

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
