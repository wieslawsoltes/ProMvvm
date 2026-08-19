using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("Nested", "HotStart")]
public class NestedHotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new()
    {
        Child = new BenchmarkChild { Value = 42 },
    };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        _model.WhenAnyValue(BenchmarkPaths.ChildValue).Subscribe(_observer).Dispose();

    [Benchmark]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue(value => value.Child!.Value).Subscribe(_observer).Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.ChildValue(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.ChildValue(_model).Subscribe(_observer).Dispose();
}

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

[BenchmarkCategory("Nested", "ThreeSegment", "LeafEmission")]
public class DeepNestedLeafBenchmarks : BenchmarkConfig
{
    private readonly DeepBenchmarkState _typed = new();
    private readonly DeepBenchmarkState _expression = new();
    private readonly DeepBenchmarkState _reactiveUiCore = new();
    private readonly DeepBenchmarkState _reactiveUiReactive = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private IDisposable _reactiveUiReactiveSubscription = null!;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typed.Model.WhenAnyValue(BenchmarkPaths.DeepChildValue).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expression.Model
            .WhenAnyValue(value => value.Child!.Child!.Value).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter
            .DeepChildValue(_reactiveUiCore.Model).Subscribe(_observer);
        _reactiveUiReactiveSubscription = ReactiveUiReactiveBenchmarkAdapter
            .DeepChildValue(_reactiveUiReactive.Model).Subscribe(_observer);
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
    public void ProMvvmTyped() => _typed.EmitLeaf();

    [Benchmark]
    public void ProMvvmExpression() => _expression.EmitLeaf();

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCore.EmitLeaf();

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiReactive.EmitLeaf();
}

[BenchmarkCategory("Nested", "ThreeSegment", "HotStart")]
public class DeepNestedHotStartBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _model = new()
    {
        Child = new BenchmarkChild
        {
            Child = new BenchmarkChild { Value = 42 },
        },
    };
    private readonly BenchmarkObserver<int> _observer = new();

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() =>
        _model.WhenAnyValue(BenchmarkPaths.DeepChildValue).Subscribe(_observer).Dispose();

    [Benchmark]
#pragma warning disable IL2026
    public void ProMvvmExpression() =>
        _model.WhenAnyValue(value => value.Child!.Child!.Value).Subscribe(_observer).Dispose();
#pragma warning restore IL2026

    [Benchmark]
    public void ReactiveUiCore() =>
        ReactiveUiCoreBenchmarkAdapter.DeepChildValue(_model).Subscribe(_observer).Dispose();

    [Benchmark]
    public void ReactiveUiReactive() =>
        ReactiveUiReactiveBenchmarkAdapter.DeepChildValue(_model).Subscribe(_observer).Dispose();
}

[BenchmarkCategory("Nested", "ThreeSegment", "Rewire")]
public class DeepNestedRewireBenchmarks : BenchmarkConfig
{
    private readonly DeepBenchmarkState _typed = new();
    private readonly DeepBenchmarkState _expression = new();
    private readonly DeepBenchmarkState _reactiveUiCore = new();
    private readonly DeepBenchmarkState _reactiveUiReactive = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private IDisposable _reactiveUiReactiveSubscription = null!;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typed.Model.WhenAnyValue(BenchmarkPaths.DeepChildValue).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expression.Model
            .WhenAnyValue(value => value.Child!.Child!.Value).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter
            .DeepChildValue(_reactiveUiCore.Model).Subscribe(_observer);
        _reactiveUiReactiveSubscription = ReactiveUiReactiveBenchmarkAdapter
            .DeepChildValue(_reactiveUiReactive.Model).Subscribe(_observer);
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
    public void ProMvvmTyped() => _typed.RewireLeaf();

    [Benchmark]
    public void ProMvvmExpression() => _expression.RewireLeaf();

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCore.RewireLeaf();

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiReactive.RewireLeaf();
}

internal sealed class DeepBenchmarkState
{
    private readonly BenchmarkChild _first = new() { Value = 1 };
    private readonly BenchmarkChild _second = new() { Value = 2 };
    private int _next;
    private bool _toggle;

    public DeepBenchmarkState() =>
        Model = new BenchmarkModel
        {
            Child = new BenchmarkChild { Child = _first },
        };

    public BenchmarkModel Model { get; }

    public void EmitLeaf() => Model.Child!.Child!.Value = ++_next;

    public void RewireLeaf()
    {
        _toggle = !_toggle;
        Model.Child!.Child = _toggle ? _second : _first;
    }
}
