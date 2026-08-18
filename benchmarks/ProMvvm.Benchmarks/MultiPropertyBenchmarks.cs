using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("MultiProperty", "CombineLatest")]
public class MultiPropertyBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new() { Other = 1 };
    private readonly BenchmarkModel _expressionModel = new() { Other = 1 };
    private readonly BenchmarkModel _reactiveUiModel = new() { Other = 1 };
    private readonly BenchmarkModel _reactiveUiCoreModel = new() { Other = 1 };
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typedModel.WhenAnyValue(
            BenchmarkPaths.Value,
            BenchmarkPaths.Other,
            static (value, other) => value + other).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expressionModel.WhenAnyValue(
            value => value.Value,
            value => value.Other,
            static (value, other) => value + other).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiSubscription = ReactiveUiReactiveBenchmarkAdapter.Sum(_reactiveUiModel).Subscribe(_observer);
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.Sum(_reactiveUiCoreModel).Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedSubscription.Dispose();
        _expressionSubscription.Dispose();
        _reactiveUiSubscription.Dispose();
        _reactiveUiCoreSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typedModel.Value = ++_next;

    [Benchmark]
    public void ProMvvmExpression() => _expressionModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCoreModel.Value = ++_next;
}
