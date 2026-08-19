using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[BenchmarkCategory("SteadyState", "LeafEmission")]
public class EmissionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new();
    private readonly BenchmarkModel _expressionModel = new();
    private readonly BenchmarkModel _stringModel = new();
    private readonly BenchmarkModel _reactiveUiModel = new();
    private readonly BenchmarkModel _reactiveUiCoreModel = new();
    private readonly BenchmarkModel _reactiveUiStringModel = new();
    private readonly BenchmarkModel _reactiveUiCoreStringModel = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _stringSubscription = null!;
    private IDisposable _reactiveUiSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;
    private IDisposable _reactiveUiStringSubscription = null!;
    private IDisposable _reactiveUiCoreStringSubscription = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typedModel.WhenAnyValue(BenchmarkPaths.Value).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expressionModel.WhenAnyValue(value => value.Value).Subscribe(_observer);
        _stringSubscription = _stringModel.WhenAnyValue<BenchmarkModel, int>(nameof(BenchmarkModel.Value))
            .Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiSubscription = ReactiveUiReactiveBenchmarkAdapter.Value(_reactiveUiModel).Subscribe(_observer);
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.Value(_reactiveUiCoreModel).Subscribe(_observer);
        _reactiveUiStringSubscription = ReactiveUiReactiveBenchmarkAdapter.ValueByName(_reactiveUiStringModel)
            .Subscribe(_observer);
        _reactiveUiCoreStringSubscription = ReactiveUiCoreBenchmarkAdapter.ValueByName(_reactiveUiCoreStringModel)
            .Subscribe(_observer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedSubscription.Dispose();
        _expressionSubscription.Dispose();
        _stringSubscription.Dispose();
        _reactiveUiSubscription.Dispose();
        _reactiveUiCoreSubscription.Dispose();
        _reactiveUiStringSubscription.Dispose();
        _reactiveUiCoreStringSubscription.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void ProMvvmTyped() => _typedModel.Value = ++_next;

    [Benchmark]
    public void ProMvvmExpression() => _expressionModel.Value = ++_next;

    [Benchmark]
    public void ProMvvmString() => _stringModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactive() => _reactiveUiModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCore() => _reactiveUiCoreModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiReactiveString() => _reactiveUiStringModel.Value = ++_next;

    [Benchmark]
    public void ReactiveUiCoreString() => _reactiveUiCoreStringModel.Value = ++_next;
}

[BenchmarkCategory("Throughput", "Burst")]
public class BurstEmissionBenchmarks : BenchmarkConfig
{
    private readonly BenchmarkModel _typedModel = new();
    private readonly BenchmarkModel _expressionModel = new();
    private readonly BenchmarkModel _reactiveUiModel = new();
    private readonly BenchmarkModel _reactiveUiCoreModel = new();
    private readonly BenchmarkObserver<int> _observer = new();
    private IDisposable _typedSubscription = null!;
    private IDisposable _expressionSubscription = null!;
    private IDisposable _reactiveUiSubscription = null!;
    private IDisposable _reactiveUiCoreSubscription = null!;

    [Params(1, 100, 10_000)]
    public int ChangeCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _typedSubscription = _typedModel.WhenAnyValue(BenchmarkPaths.Value).Subscribe(_observer);
#pragma warning disable IL2026
        _expressionSubscription = _expressionModel.WhenAnyValue(value => value.Value).Subscribe(_observer);
#pragma warning restore IL2026
        _reactiveUiSubscription = ReactiveUiReactiveBenchmarkAdapter.Value(_reactiveUiModel).Subscribe(_observer);
        _reactiveUiCoreSubscription = ReactiveUiCoreBenchmarkAdapter.Value(_reactiveUiCoreModel).Subscribe(_observer);
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
        for (var index = 0; index < ChangeCount; index++)
        {
            _typedModel.Value = index;
        }
    }

    [Benchmark]
    public void ProMvvmExpression()
    {
        for (var index = 0; index < ChangeCount; index++)
        {
            _expressionModel.Value = index;
        }
    }

    [Benchmark]
    public void ReactiveUiReactive()
    {
        for (var index = 0; index < ChangeCount; index++)
        {
            _reactiveUiModel.Value = index;
        }
    }

    [Benchmark]
    public void ReactiveUiCore()
    {
        for (var index = 0; index < ChangeCount; index++)
        {
            _reactiveUiCoreModel.Value = index;
        }
    }
}
