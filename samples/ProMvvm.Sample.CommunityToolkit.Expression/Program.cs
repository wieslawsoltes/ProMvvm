using ProMvvm;
using ProMvvm.Sample.CommunityToolkit.Expression;
using System.Reactive.Linq;

var viewModel = new MainViewModel();

// Expression mode is intended for gradual migration in ordinary JIT applications.
// Prefer generated or handwritten PropertyPath descriptors for trimming and NativeAOT.
#pragma warning disable IL2026
using var searchSubscription = viewModel
    .WhenAnyValue(model => model.SearchText)
    .Where(static text => !string.IsNullOrWhiteSpace(text))
    .Select(static text => $"Searching for '{text}'")
    .Subscribe(Console.WriteLine);

using var categorySubscription = viewModel
    .WhenAnyValue(model => model.Options!.Category)
    .Select(static category => $"Category: {category}")
    .Subscribe(Console.WriteLine);

using var lengthSubscription = viewModel
    .WhenAnyValue<MainViewModel, int, string>(
        model => model.SearchText,
        static text => text.Length)
    .Select(static length => $"Search length: {length}")
    .Subscribe(Console.WriteLine);

using var readinessSubscription = viewModel
    .WhenAnyValue(
        model => model.SearchText,
        model => model.MinimumLength,
        static (text, minimumLength) => text.Length >= minimumLength)
    .Select(static ready => $"Ready: {ready}")
    .Subscribe(Console.WriteLine);

using var stateSubscription = viewModel
    .WhenAnyValue(
        model => model.SearchText,
        model => model.MinimumLength)
    .Select(static state => $"State: '{state.Item1}', minimum {state.Item2}")
    .Subscribe(Console.WriteLine);

using var summarySubscription = viewModel
    .WhenAnyValue(
        model => model.SearchText,
        model => model.MinimumLength,
        model => model.Options!.Category,
        static (text, minimumLength, category) =>
            $"{category}: '{text}' ({text.Length}/{minimumLength})")
    .Subscribe(Console.WriteLine);
#pragma warning restore IL2026

viewModel.SearchText = "MVVM";
viewModel.MinimumLength = 5;
viewModel.Options!.Category = "Libraries";
viewModel.Options = new SearchOptions { Category = "UI" };
