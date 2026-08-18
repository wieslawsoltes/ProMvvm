using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("Construction", "ColdObservable")]
public class ConstructionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new();

    [Benchmark(Baseline = true)]
    public IObservable<int> ProMvvmTyped() => _model.WhenAnyValue(BenchmarkPaths.Value);

    [Benchmark]
    public IObservable<int> ProMvvmTypedGetter() =>
        _model.WhenAnyValue(static value => value.Value, nameof(BenchmarkModel.Value));

    [Benchmark]
#pragma warning disable IL2026
    public IObservable<int> ProMvvmExpression() => _model.WhenAnyValue(value => value.Value);
#pragma warning restore IL2026

    [Benchmark]
    public IObservable<int> ReactiveUiReactive() => ReactiveUiReactiveBenchmarkAdapter.Value(_model);

    [Benchmark]
    public IObservable<int> ReactiveUiCore() => ReactiveUiCoreBenchmarkAdapter.Value(_model);
}
