using ReactiveUI.Builder;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProMvvm.ReactiveUiCoreIntegrationTests;

public sealed class ReactiveUiCoreParityTests
{
    static ReactiveUiCoreParityTests() =>
        RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp();

    [Fact]
    public void MultiSourceSelectorDistinctsInputsButNotProjectedResults()
    {
        var model = new ParityModel { A = 1, B = 2 };
        var proValues = new List<string>();
        var reactiveUiValues = new List<string>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueMultiExtensions.WhenAnyValue(
                model,
                value => value.A,
                value => value.B,
                static (_, _) => "same")
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactiveUi = ReactiveUI.WhenAnyMixins.WhenAnyValue(
                model,
                value => value.A,
                value => value.B,
                (Func<int, int, string>)(static (_, _) => "same"))
            .Subscribe(reactiveUiValues.Add);

        model.A = 3;
        model.B = 4;
        model.A = 3;

        Assert.Equal(["same", "same", "same"], proValues);
        Assert.Equal(proValues, reactiveUiValues);
    }

    [Fact]
    public void ConstantIndexerUsesItemArrayNotificationName()
    {
        var model = new ParityModel();
        var proValues = new List<int>();
        var reactiveUiValues = new List<int>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueExtensions.WhenAnyValue(model, value => value[0])
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactiveUi = ReactiveUI.WhenAnyMixins.WhenAnyValue(model, value => value[0])
            .Subscribe(reactiveUiValues.Add);

        model.SetIndex(0, 1, "Item[]");
        model.SetIndex(0, 2, "Item");
        model.SetIndex(0, 3, null);

        Assert.Equal([0, 1, 3], proValues);
        Assert.Equal(proValues, reactiveUiValues);
    }

    [Fact]
    public void ArrayLengthRewriteMatchesReactiveUiCore()
    {
        var model = new ParityModel();
        var proValues = new List<int>();
        var reactiveUiValues = new List<int>();

#pragma warning disable IL2026
        using var pro = WhenAnyValueExtensions.WhenAnyValue(model, value => value.Values.Length)
            .Subscribe(proValues.Add);
#pragma warning restore IL2026
        using var reactiveUi = ReactiveUI.WhenAnyMixins.WhenAnyValue(
                model,
                value => value.Values.Length)
            .Subscribe(reactiveUiValues.Add);

        Assert.Equal([3], proValues);
        Assert.Equal(proValues, reactiveUiValues);
    }

    private sealed class ParityModel : INotifyPropertyChanged
    {
        private readonly int[] _indices = [0];
        private int _a;
        private int _b;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int A
        {
            get => _a;
            set => Set(ref _a, value);
        }

        public int B
        {
            get => _b;
            set => Set(ref _b, value);
        }

        public int this[int index] => _indices[index];

        public int[] Values { get; } = [1, 2, 3];

        public void SetIndex(int index, int value, string? notificationName)
        {
            _indices[index] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(notificationName));
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
