using ReactiveUI.Reactive.Builder;

namespace ProMvvm.Benchmarks;

internal static class ReactiveUiReactiveBenchmarkAdapter
{
    static ReactiveUiReactiveBenchmarkAdapter() =>
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();

    public static IObservable<int> Value(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Value);

    public static IObservable<int> ChildValue(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(model, value => value.Child!.Value);

    public static IObservable<int> Sum(BenchmarkModel model) =>
        ReactiveUI.Reactive.WhenAnyMixins.WhenAnyValue(
            model,
            value => value.Value,
            value => value.Other,
            static (value, other) => value + other);
}
