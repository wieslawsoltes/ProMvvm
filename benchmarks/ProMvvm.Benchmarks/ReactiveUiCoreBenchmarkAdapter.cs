using ReactiveUI.Builder;

namespace ProMvvm.Benchmarks;

internal static class ReactiveUiCoreBenchmarkAdapter
{
    static ReactiveUiCoreBenchmarkAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

    public static IObservable<int> Value(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value.Value);

    public static IObservable<int> ChildValue(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Value);

    public static IObservable<int> Sum(BenchmarkModel model) =>
        ReactiveUI.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value,
            value => value.Other,
            static (value, other) => value + other);
}
