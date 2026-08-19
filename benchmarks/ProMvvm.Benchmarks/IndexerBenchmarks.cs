using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("Indexer", "HotStart")]
public class IndexerHotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkIndexerModel _model = new();
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        _model.WhenAnyValue(BenchmarkPaths.Index).Subscribe(_observer).Dispose();

    [Benchmark]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue(value => value[0]).Subscribe(_observer).Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.Index(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.Index(_model).Subscribe(_observer).Dispose();
}

[BenchmarkCategory("Indexer", "LeafEmission")]
public class IndexerEmissionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkIndexerModel _typedModel = new();
    private readonly BenchmarkIndexerModel _expressionModel = new();
    private readonly BenchmarkIndexerModel _coreModel = new();
    private readonly BenchmarkIndexerModel _reactiveModel = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typed = null!;
    private IDisposable _expression = null!;
    private IDisposable _core = null!;
    private IDisposable _reactive = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _typed = _typedModel.WhenAnyValue(BenchmarkPaths.Index).Subscribe(_observer);
#pragma warning disable IL2026
        _expression = _expressionModel.WhenAnyValue(value => value[0]).Subscribe(_observer);
#pragma warning restore IL2026
        _core = ReactiveUiCoreBenchmarkAdapter.Index(_coreModel).Subscribe(_observer);
        _reactive = ReactiveUiReactiveBenchmarkAdapter.Index(_reactiveModel).Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typed.Dispose();
        _expression.Dispose();
        _core.Dispose();
        _reactive.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typedModel.SetIndex(++_next);

    [Benchmark]
    public void ProMvvmExpression() => _expressionModel.SetIndex(++_next);

    [Benchmark]
    public void ReactiveUiCore() => _coreModel.SetIndex(++_next);

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveModel.SetIndex(++_next);
}

[BenchmarkCategory("Indexer", "Nested", "HotStart")]
public class NestedIndexerHotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkIndexerModel _model = new()
    {
        Child = new BenchmarkIndexerModel(),
    };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        _model.WhenAnyValue(BenchmarkPaths.ChildIndex).Subscribe(_observer).Dispose();

    [Benchmark]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue(value => value.Child![0]).Subscribe(_observer).Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.ChildIndex(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.ChildIndex(_model).Subscribe(_observer).Dispose();
}

[BenchmarkCategory("Indexer", "Nested", "LeafEmission")]
public class NestedIndexerEmissionBenchmarks : BenchmarkConfig
{
    private readonly IndexerBenchmarkState _typed = new();
    private readonly IndexerBenchmarkState _expression = new();
    private readonly IndexerBenchmarkState _core = new();
    private readonly IndexerBenchmarkState _reactive = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _coreSubscription = null!;
    private IDisposable _reactiveSubscription = null!;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typed.Model.WhenAnyValue(BenchmarkPaths.ChildIndex).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expression.Model
            .WhenAnyValue(value => value.Child![0]).Subscribe(_observer);
#pragma warning restore IL2026
        _coreSubscription = ReactiveUiCoreBenchmarkAdapter.ChildIndex(_core.Model).Subscribe(_observer);
        _reactiveSubscription = ReactiveUiReactiveBenchmarkAdapter.ChildIndex(_reactive.Model).Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedSubscription.Dispose();
        _expressionSubscription.Dispose();
        _coreSubscription.Dispose();
        _reactiveSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typed.Emit();

    [Benchmark]
    public void ProMvvmExpression() => _expression.Emit();

    [Benchmark]
    public void ReactiveUiCore() => _core.Emit();

    [Benchmark]
    public void ReactiveUiReactive() => _reactive.Emit();
}

[BenchmarkCategory("Indexer", "Nested", "Rewire")]
public class NestedIndexerRewireBenchmarks : BenchmarkConfig
{
    private readonly IndexerBenchmarkState _typed = new();
    private readonly IndexerBenchmarkState _expression = new();
    private readonly IndexerBenchmarkState _core = new();
    private readonly IndexerBenchmarkState _reactive = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _coreSubscription = null!;
    private IDisposable _reactiveSubscription = null!;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typed.Model.WhenAnyValue(BenchmarkPaths.ChildIndex).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expression.Model
            .WhenAnyValue(value => value.Child![0]).Subscribe(_observer);
#pragma warning restore IL2026
        _coreSubscription = ReactiveUiCoreBenchmarkAdapter.ChildIndex(_core.Model).Subscribe(_observer);
        _reactiveSubscription = ReactiveUiReactiveBenchmarkAdapter.ChildIndex(_reactive.Model).Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedSubscription.Dispose();
        _expressionSubscription.Dispose();
        _coreSubscription.Dispose();
        _reactiveSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typed.Rewire();

    [Benchmark]
    public void ProMvvmExpression() => _expression.Rewire();

    [Benchmark]
    public void ReactiveUiCore() => _core.Rewire();

    [Benchmark]
    public void ReactiveUiReactive() => _reactive.Rewire();
}

[BenchmarkCategory("Indexer", "RuntimeInvocation")]
public class IndexerInvocationBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkIndexerModel _model = new();
    private readonly object?[] _arguments = [0];
    private readonly PropertyInfo _property = typeof(BenchmarkIndexerModel).GetProperty("Item")!;
    private readonly MethodInvoker _invoker;
    private readonly Func<BenchmarkIndexerModel, int, int> _typedGetter;

    public IndexerInvocationBenchmarks()
    {
        var getter = _property.GetMethod!;
        _invoker = MethodInvoker.Create(getter);
        _typedGetter = getter.CreateDelegate<Func<BenchmarkIndexerModel, int, int>>();
    }

    [Benchmark(Baseline = true)]
    public object? PropertyInfoGetValue() => _property.GetValue(_model, _arguments);

    [Benchmark]
    public object? MethodInvokerInvoke() => _invoker.Invoke(_model, _arguments[0]);

    [Benchmark]
    public int TypedDelegate() => _typedGetter(_model, 0);
}

internal sealed class IndexerBenchmarkState
{
    private readonly BenchmarkIndexerModel _first = new();
    private readonly BenchmarkIndexerModel _second = new();
    private bool _toggle;
    private int _next;

    public IndexerBenchmarkState() => Model = new BenchmarkIndexerModel { Child = _first };

    public BenchmarkIndexerModel Model { get; }

    public void Emit() => Model.Child!.SetIndex(++_next);

    public void Rewire()
    {
        _toggle = !_toggle;
        Model.Child = _toggle ? _second : _first;
    }
}
