using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("Subscription", "Allocation")]
public class SubscriptionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new() { Value = 42 };
    private readonly BenchmarkObserver<int> _observer = new();
    private IObservable<int> _typed = null!;
    private IObservable<int> _expression = null!;
    private IObservable<int> _reactiveUiReactive = null!;
    private IObservable<int> _reactiveUiCore = null!;

    [GlobalSetup]
    public void Setup()
    {
        _typed = _model.WhenAnyValue(BenchmarkPaths.Value);
#pragma warning disable IL2026
        _expression = _model.WhenAnyValue(value => value.Value);
#pragma warning restore IL2026
        _reactiveUiReactive = ReactiveUiReactiveBenchmarkAdapter.Value(_model);
        _reactiveUiCore = ReactiveUiCoreBenchmarkAdapter.Value(_model);
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTypedSubscribeDispose() => _typed.Subscribe(_observer).Dispose();

    [Benchmark]
    public void ProMvvmExpressionSubscribeDispose() => _expression.Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactiveSubscribeDispose() => _reactiveUiReactive.Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiCoreSubscribeDispose() => _reactiveUiCore.Subscribe(_observer).Dispose();
}
