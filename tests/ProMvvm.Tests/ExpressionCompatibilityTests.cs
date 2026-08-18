using System.Diagnostics.CodeAnalysis;

namespace ProMvvm.Tests;

[SuppressMessage("Trimming", "IL2026", Justification = "These tests intentionally cover the reflection-based compatibility surface.")]
public sealed class ExpressionCompatibilityTests
{
    [Fact]
    public void ObservesPropertyExpression()
    {
        var model = new ObservableModel { Count = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value.Count).Subscribe(values.Add);
        model.Count = 2;

        Assert.Equal([1, 2], values);
    }

    [Fact]
    public void ReusesCachedPathsAcrossSameTypedProperties()
    {
        var model = new ObservableModel { Count = 3 };

        using var count = model.WhenAnyValue(value => value.Count).Subscribe(_ => { });
        using var subscriberCount = model.WhenAnyValue(value => value.SubscriberCount).Subscribe(_ => { });
        using var cachedSubscriberCount = model.WhenAnyValue(value => value.SubscriberCount).Subscribe(_ => { });
        using var cachedCount = model.WhenAnyValue(value => value.Count).Subscribe(_ => { });

        Assert.Equal(4, model.SubscriberCount);
    }

    [Fact]
    public void CachedPathsAreThreadSafe()
    {
        var failures = 0;

        Parallel.For(0, 1_000, index =>
        {
            var model = new ObservableModel { Count = index + 10 };
            var observed = -1;
            var expected = index % 2 == 0 ? model.Count : 1;

            using var subscription = index % 2 == 0
                ? model.WhenAnyValue(value => value.Count).Subscribe(value => observed = value)
                : model.WhenAnyValue(value => value.SubscriberCount).Subscribe(value => observed = value);

            if (observed != expected)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Equal(0, failures);
    }

    [Fact]
    public void ObservesNestedExpression()
    {
        var model = new ObservableModel { Child = new ObservableModel { Name = "one" } };
        var values = new List<string?>();

        using var subscription = model.WhenAnyValue(value => value.Child!.Name).Subscribe(values.Add);
        model.Child = new ObservableModel { Name = "two" };

        Assert.Equal(["one", "two"], values);
    }

    [Fact]
    public void ReusesAndAlternatesCachedTwoPropertyPaths()
    {
        var model = new ObservableModel
        {
            Child = new ObservableModel { Count = 4 },
        };

        using var count = model.WhenAnyValue(value => value.Child!.Count).Subscribe(_ => { });
        using var subscribers = model.WhenAnyValue(value => value.Child!.SubscriberCount).Subscribe(_ => { });
        using var cachedSubscribers = model.WhenAnyValue(value => value.Child!.SubscriberCount).Subscribe(_ => { });
        using var cachedCount = model.WhenAnyValue(value => value.Child!.Count).Subscribe(_ => { });

        Assert.Equal(4, model.SubscriberCount);
        Assert.Equal(4, model.Child!.SubscriberCount);
    }

    [Fact]
    public void ObservesFieldExpression()
    {
        var model = new FieldModel { Value = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value.Value).Subscribe(values.Add);
        model.SetValue(2);

        Assert.Equal([1, 2], values);
    }

    [Fact]
    public void SupportsValueConversionAtExpressionLeaf()
    {
        var model = new ObservableModel { Count = 7 };
        var values = new List<object>();

        using var subscription = model.WhenAnyValue(value => (object)value.Count).Subscribe(values.Add);
        model.Count = 8;

        Assert.Equal([7, 8], values);
    }

    [Fact]
    public void PropertyGetterFailureIsReportedAndUnsubscribed()
    {
        var model = new ObservableModel();
        Exception? error = null;

        using var subscription = model.WhenAnyValue(value => value.Throwing)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void RejectsNonMemberExpression()
    {
        var model = new ObservableModel();

        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(value => value.GetName()));
        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(_ => 42));
    }
}
