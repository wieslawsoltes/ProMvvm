using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("HotStart", "SingleSelector")]
public class SingleSelectorHotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new() { Value = 42 };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue<BenchmarkModel, int, int>(
                value => value.Value,
                static value => value)
            .Subscribe(_observer)
            .Dispose();

    [Benchmark]
    public void ProMvvmString() =>
        _model.WhenAnyValue<BenchmarkModel, int, int>(
                nameof(BenchmarkModel.Value),
                static value => value)
            .Subscribe(_observer)
            .Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCoreExpression() =>
        ReactiveUiCoreBenchmarkAdapter.SelectedValue(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiCoreString() =>
        ReactiveUiCoreBenchmarkAdapter.SelectedValueByName(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactiveExpression() =>
        ReactiveUiReactiveBenchmarkAdapter.SelectedValue(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactiveString() =>
        ReactiveUiReactiveBenchmarkAdapter.SelectedValueByName(_model).Subscribe(_observer).Dispose();
}

[BenchmarkCategory("SteadyState", "SingleSelector", "LeafEmission")]
public class SingleSelectorEmissionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _expressionModel = new();
    private readonly BenchmarkModel _stringModel = new();
    private readonly BenchmarkModel _coreExpressionModel = new();
    private readonly BenchmarkModel _coreStringModel = new();
    private readonly BenchmarkModel _reactiveExpressionModel = new();
    private readonly BenchmarkModel _reactiveStringModel = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private readonly List<IDisposable> _subscriptions = [];
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable IL2026
        _subscriptions.Add(_expressionModel.WhenAnyValue<BenchmarkModel, int, int>(
            value => value.Value, static value => value).Subscribe(_observer));
        _subscriptions.Add(_stringModel.WhenAnyValue<BenchmarkModel, int, int>(
            nameof(BenchmarkModel.Value), static value => value).Subscribe(_observer));
#pragma warning restore IL2026
        _subscriptions.Add(ReactiveUiCoreBenchmarkAdapter.SelectedValue(_coreExpressionModel).Subscribe(_observer));
        _subscriptions.Add(ReactiveUiCoreBenchmarkAdapter.SelectedValueByName(_coreStringModel).Subscribe(_observer));
        _subscriptions.Add(ReactiveUiReactiveBenchmarkAdapter.SelectedValue(_reactiveExpressionModel).Subscribe(_observer));
        _subscriptions.Add(ReactiveUiReactiveBenchmarkAdapter.SelectedValueByName(_reactiveStringModel).Subscribe(_observer));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmExpression() => _expressionModel.Value = ++_next;

    [Benchmark]
    public void ProMvvmString() => _stringModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCoreExpression() => _coreExpressionModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCoreString() => _coreStringModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactiveExpression() => _reactiveExpressionModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactiveString() => _reactiveStringModel.Value = ++_next;
}
