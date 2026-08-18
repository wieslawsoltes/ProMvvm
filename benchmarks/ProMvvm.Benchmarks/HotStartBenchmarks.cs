using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("HotStart", "Construction", "Subscription", "InitialEmission")]
public class HotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new() { Value = 42 };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        _model.WhenAnyValue(BenchmarkPaths.Value).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ProMvvmTypedGetter() =>
        _model.WhenAnyValue(static value => value.Value, nameof(BenchmarkModel.Value))
            .Subscribe(_observer)
            .Dispose();

    [Benchmark]
    public void ProMvvmGeneratedDescriptor() =>
        _model.WhenAnyValue(BenchmarkModelPropertyPaths.Value)
            .Subscribe(_observer)
            .Dispose();

    [Benchmark]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue(value => value.Value).Subscribe(_observer).Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.Value(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.Value(_model).Subscribe(_observer).Dispose();
}
