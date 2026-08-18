using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("Nested", "LeafEmission")]
public class NestedLeafBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _expressionModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _reactiveUiModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _reactiveUiCoreModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typedModel.WhenAnyValue(BenchmarkPaths.ChildValue).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expressionModel.WhenAnyValue(value => value.Child!.Value).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiSubscription = ReactiveUiReactiveBenchmarkAdapter.ChildValue(_reactiveUiModel).Subscribe(_observer);
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.ChildValue(_reactiveUiCoreModel).Subscribe(_observer);
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
    public void ProMvvmTyped() => _typedModel.Child!.Value = ++_next;

    [Benchmark]
    public void ProMvvmExpression() => _expressionModel.Child!.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiModel.Child!.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCoreModel.Child!.Value = ++_next;
}

[BenchmarkCategory("Nested", "Rewire")]
public class NestedRewireBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _expressionModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _reactiveUiModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkModel _reactiveUiCoreModel = new() { Child = new BenchmarkChild() };
    private readonly BenchmarkObserver<int> _observer = new();
    private readonly BenchmarkChild _first = new() { Value = 1 };
    private readonly BenchmarkChild _second = new() { Value = 2 };
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private bool _toggle;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typedModel.WhenAnyValue(BenchmarkPaths.ChildValue).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expressionModel.WhenAnyValue(value => value.Child!.Value).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiSubscription = ReactiveUiReactiveBenchmarkAdapter.ChildValue(_reactiveUiModel).Subscribe(_observer);
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.ChildValue(_reactiveUiCoreModel).Subscribe(_observer);
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
    public void ProMvvmTyped()
    {
        _toggle = !_toggle;
        _typedModel.Child = _toggle ? _first : _second;
    }

    [Benchmark]
    public void ProMvvmExpression()
    {
        _toggle = !_toggle;
        _expressionModel.Child = _toggle ? _first : _second;
    }

    [Benchmark]
    public void ReactiveUiReactive()
    {
        _toggle = !_toggle;
        _reactiveUiModel.Child = _toggle ? _first : _second;
    }

    [Benchmark]
    public void ReactiveUiCore()
    {
        _toggle = !_toggle;
        _reactiveUiCoreModel.Child = _toggle ? _first : _second;
    }
}
