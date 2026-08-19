using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("MultiProperty", "Arity12", "Emission")]
public class MultiPropertyArity12Benchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new() { Value = 1 };
    private readonly BenchmarkModel _expressionModel = new() { Value = 1 };
    private readonly BenchmarkModel _reactiveUiCoreModel = new() { Value = 1 };
    private readonly BenchmarkModel _reactiveUiReactiveModel = new() { Value = 1 };
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private IDisposable _reactiveUiReactiveSubscription = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = ObserveTyped(_typedModel).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = ObserveExpression(_expressionModel).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.Sum12(_reactiveUiCoreModel)
            .Subscribe(_observer);
        _reactiveUiReactiveSubscription = ReactiveUiReactiveBenchmarkAdapter.Sum12(_reactiveUiReactiveModel)
            .Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedSubscription.Dispose();
        _expressionSubscription.Dispose();
        _reactiveUiCoreSubscription.Dispose();
        _reactiveUiReactiveSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typedModel.Value = ++_next;

    [Benchmark]
    public void ProMvvmExpression() => _expressionModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCoreModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiReactiveModel.Value = ++_next;

    internal static IObservable<int> ObserveTyped(BenchmarkModel model) => model.WhenAnyValue(
        BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value,
        BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value,
        BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value, BenchmarkPaths.Value,
        static (a, b, c, d, e, f, g, h, i, j, k, l) =>
            a + b + c + d + e + f + g + h + i + j + k + l);

#pragma warning disable IL2026
    internal static IObservable<int> ObserveExpression(BenchmarkModel model) => model.WhenAnyValue(
        value => value.Value, value => value.Value, value => value.Value,
        value => value.Value, value => value.Value, value => value.Value,
        value => value.Value, value => value.Value, value => value.Value,
        value => value.Value, value => value.Value, value => value.Value,
        static (a, b, c, d, e, f, g, h, i, j, k, l) =>
            a + b + c + d + e + f + g + h + i + j + k + l);
#pragma warning restore IL2026
}

[BenchmarkCategory("MultiProperty", "Arity12", "HotStart")]
public class MultiPropertyArity12HotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new() { Value = 1 };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        MultiPropertyArity12Benchmarks.ObserveTyped(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ProMvvmExpression() =>
        MultiPropertyArity12Benchmarks.ObserveExpression(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.Sum12(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.Sum12(_model).Subscribe(_observer).Dispose();
}

[BenchmarkCategory("NotificationAdapter", "Emission")]
public class NotificationAdapterBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _defaultModel = new();
    private readonly BenchmarkModel _adapterModel = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _defaultSubscription = null!;
    private IDisposable _adapterSubscription = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _defaultSubscription = _defaultModel.WhenAnyValue(BenchmarkPaths.Value).Subscribe(_observer);
        _adapterSubscription = _adapterModel.WhenAnyValue(
            BenchmarkPaths.Value,
            PropertyNotificationAdapters.Inpc).Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _defaultSubscription.Dispose();
        _adapterSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void DirectInpc() => _defaultModel.Value = ++_next;

    [Benchmark]
    public void ExplicitAdapter() => _adapterModel.Value = ++_next;
}
