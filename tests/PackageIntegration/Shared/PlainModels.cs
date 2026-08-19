using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProMvvm.PackageIntegration;

[GeneratePropertyPaths]
public sealed partial class PlainModel : INotifyPropertyChanged
{
    private int _count = 1;
    private string _name = "plain";
    private PlainChild _child = new() { Value = 2 };

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count
    {
        get => _count;
        set => Set(ref _count, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public PlainChild Child
    {
        get => _child;
        set => Set(ref _child, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class PlainChild : INotifyPropertyChanged
{
    private int _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}
