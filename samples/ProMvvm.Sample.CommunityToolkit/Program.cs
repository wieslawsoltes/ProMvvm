using CommunityToolkit.Mvvm.ComponentModel;
using ProMvvm;
using ProMvvm.Sample.CommunityToolkit;
using System.Reactive.Linq;

var viewModel = new MainViewModel();

using var subscription = viewModel.WhenAnyValue(MainViewModelPropertyPaths.SearchText)
    .Where(static text => !string.IsNullOrWhiteSpace(text))
    .Select(static text => $"Searching for '{text}'")
    .Subscribe(Console.WriteLine);

viewModel.SearchText = "NativeAOT";
