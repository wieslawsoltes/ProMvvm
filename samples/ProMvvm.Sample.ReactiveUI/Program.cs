using ProMvvm;
using ProMvvm.Sample.ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using RxReactiveObject = ReactiveUI.Reactive.ReactiveObject;

var viewModel = new MainViewModel();

using var subscription = viewModel.WhenAnyValue(
        static model => model.SearchText,
        nameof(MainViewModel.SearchText))
    .Where(static text => !string.IsNullOrWhiteSpace(text))
    .Select(static text => $"Searching for '{text}'")
    .Subscribe(Console.WriteLine);

viewModel.SearchText = "System.Reactive 7";
