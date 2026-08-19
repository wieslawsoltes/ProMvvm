using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
    public void OptimizesInheritedDirectAndNestedProperties()
    {
        var child = new ObservableModel { Name = "one" };
        var model = new DerivedObservableModel { Count = 1, Child = child };
        var counts = new List<int>();
        var names = new List<string?>();

        using var countSubscription = model.WhenAnyValue(value => value.Count).Subscribe(counts.Add);
        using var nameSubscription = model.WhenAnyValue(value => value.Child!.Name).Subscribe(names.Add);
        model.Count = 2;
        child.Name = "two";

        Assert.Equal([1, 2], counts);
        Assert.Equal(["one", "two"], names);
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
    public void ReusesAndAlternatesCachedThreePropertyPaths()
    {
        var leaf = new ObservableModel { Count = 4 };
        var middle = new ObservableModel { Child = leaf };
        var model = new ObservableModel { Child = middle };

        using var count = model.WhenAnyValue(value => value.Child!.Child!.Count).Subscribe(_ => { });
        using var subscribers = model.WhenAnyValue(value => value.Child!.Child!.SubscriberCount)
            .Subscribe(_ => { });
        using var cachedSubscribers = model.WhenAnyValue(value => value.Child!.Child!.SubscriberCount)
            .Subscribe(_ => { });
        using var cachedCount = model.WhenAnyValue(value => value.Child!.Child!.Count).Subscribe(_ => { });

        Assert.Equal(4, model.SubscriberCount);
        Assert.Equal(4, middle.SubscriberCount);
        Assert.Equal(4, leaf.SubscriberCount);
    }

    [Fact]
    public void ObservesFieldExpression()
    {
        var model = new FieldModel { Value = 1 };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value.Value).Subscribe(values.Add);
        using var cachedSubscription = model.WhenAnyValue(value => value.Value).Subscribe(_ => { });
        model.SetValue(2);

        Assert.Equal([1, 2], values);
    }

    [Fact]
    public void NullFieldMatchesReactiveUiInvalidOperationBehavior()
    {
        var model = new FieldModel();
        Exception? error = null;

        using var subscription = model.WhenAnyValue(value => value.Text)
            .Subscribe(_ => { }, value => error = value);

        Assert.IsType<InvalidOperationException>(error);
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
        var parameter = Expression.Parameter(typeof(ObservableModel), "value");
        var explicitGetterCall = Expression.Lambda<Func<ObservableModel, int>>(
            Expression.Call(
                parameter,
                typeof(ObservableModel).GetProperty(nameof(ObservableModel.Count))!.GetMethod!),
            parameter);

        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(value => value.GetName()));
        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(_ => 42));
        Assert.Throws<ArgumentException>(() => model.WhenAnyValue(explicitGetterCall));
    }

    [Fact]
    public void ObservesConstantIndexerUsingReactiveUiNotificationName()
    {
        var model = new IndexerModel();
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value[0]).Subscribe(values.Add);
        model.Set(0, 1);
        model.Set(0, 2, "Item");
        model.Set(0, 3, null);
        model.Set(0, 4, string.Empty);

        Assert.Equal([0, 1, 3, 4], values);
        Assert.Equal(1, model.SubscriberCount);
    }

    [Fact]
    public void ObservesStringAndMultiArgumentIndexers()
    {
        var model = new IndexerModel();
        var namedValues = new List<string>();
        var matrixValues = new List<int>();

        using var named = model.WhenAnyValue(value => value["first"]).Subscribe(namedValues.Add);
        using var matrix = model.WhenAnyValue(value => value[1, 2]).Subscribe(matrixValues.Add);
        model.Set("first", "two");
        model.Set(1, 2, 34);

        Assert.Equal(["one", "two"], namedValues);
        Assert.Equal([12, 34], matrixValues);
    }

    [Fact]
    public void RewiresNestedConstantIndexerAndDetachesOldChild()
    {
        var oldChild = new IndexerModel();
        var model = new IndexerModel { Child = oldChild };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value.Child![0]).Subscribe(values.Add);
        oldChild.Set(0, 1);
        var newChild = new IndexerModel();
        model.Child = newChild;
        oldChild.Set(0, 2);
        newChild.Set(0, 3);

        Assert.Equal([0, 1, 0, 3], values);
        Assert.Equal(0, oldChild.SubscriberCount);
        Assert.Equal(1, newChild.SubscriberCount);
    }

    [Fact]
    public void NestedIndexerCacheDistinguishesAlternatingConstants()
    {
        var child = new IndexerModel();
        var model = new IndexerModel { Child = child };
        var values = new List<int>();

        using var zero = model.WhenAnyValue(value => value.Child![0]).Subscribe(values.Add);
        using var one = model.WhenAnyValue(value => value.Child![1]).Subscribe(values.Add);
        using var cachedZero = model.WhenAnyValue(value => value.Child![0]).Subscribe(values.Add);

        Assert.Equal([0, 10, 0], values);
    }

    [Fact]
    public void SupportsMembersAfterConstantIndexer()
    {
        var child = new IndexerModel { Child = new IndexerModel() };
        var model = new IndexerModel { Child = child };
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value.Child!.Child![0]).Subscribe(values.Add);
        child.Child!.Set(0, 7);

        Assert.Equal([0, 7], values);
    }

    [Fact]
    public void RejectsCapturedAndArrayIndicesLikeReactiveUi()
    {
        var model = new IndexerModel();
        var index = 0;

        var captured = Assert.Throws<NotSupportedException>(() =>
            model.WhenAnyValue(value => value[index]));
        var array = Assert.Throws<ArgumentException>(() =>
            model.WhenAnyValue(value => value.Values[0]));
        var capturedArray = Assert.Throws<NotSupportedException>(() =>
            model.WhenAnyValue(value => value.Values[index]));

        Assert.Equal("Index expressions are only supported with constants.", captured.Message);
        Assert.Equal("Array index expressions are only supported with constants.", capturedArray.Message);
        Assert.Contains("valid member info", array.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportsReactiveUiArrayLengthRewrite()
    {
        var model = new IndexerModel();
        var nestedValues = new List<int>();
        var directValues = new List<int>();
        int[] source = [1, 2, 3, 4];

        using var nested = model.WhenAnyValue(value => value.Values.Length)
            .Subscribe(nestedValues.Add);
        using var direct = source.WhenAnyValue(value => value.Length)
            .Subscribe(directValues.Add);

        Assert.Equal([3], nestedValues);
        Assert.Equal([4], directValues);
    }

    [Fact]
    public void SupportsExplicitIndexExpressionAndNullConstant()
    {
        var model = new IndexerModel();
        var intValues = new List<int>();
        var stringValues = new List<string>();
        var parameter = Expression.Parameter(typeof(IndexerModel), "value");
        var intProperty = typeof(IndexerModel).GetProperty("Item", [typeof(int)])!;
        var stringProperty = typeof(IndexerModel).GetProperty("Item", [typeof(string)])!;
        var intExpression = Expression.Lambda<Func<IndexerModel, int>>(
            Expression.MakeIndex(parameter, intProperty, [Expression.Constant(0)]),
            parameter);
        var stringExpression = Expression.Lambda<Func<IndexerModel, string>>(
            Expression.MakeIndex(
                parameter,
                stringProperty,
                [Expression.Constant(null, typeof(string))]),
            parameter);

        using var intSubscription = model.WhenAnyValue(intExpression).Subscribe(intValues.Add);
        using var stringSubscription = model.WhenAnyValue(stringExpression).Subscribe(stringValues.Add);

        Assert.Equal([0], intValues);
        Assert.Equal(["<null>"], stringValues);
    }

    [Fact]
    public void GeneralIndexerFallbackCoversOneThroughFiveArguments()
    {
        var model = new IndexerModel();
        long one = 0;
        long two = 0;
        var three = 0;
        var four = 0;
        var five = 0;

        using var subscription1 = model.WhenAnyValue(value => value[2L])
            .Subscribe(value => one = value);
        using var subscription2 = model.WhenAnyValue(value => value[2L, 3L])
            .Subscribe(value => two = value);
        using var subscription3 = model.WhenAnyValue(value => value[1, 2, 3])
            .Subscribe(value => three = value);
        using var subscription4 = model.WhenAnyValue(value => value[1, 2, 3, 4])
            .Subscribe(value => four = value);
        using var subscription5 = model.WhenAnyValue(value => value[1, 2, 3, 4, 5])
            .Subscribe(value => five = value);

        Assert.Equal(102, one);
        Assert.Equal(5, two);
        Assert.Equal(6, three);
        Assert.Equal(10, four);
        Assert.Equal(15, five);
    }

    [Fact]
    public void CustomIndexerNameUsesItsAccessorNotificationName()
    {
        var model = new CustomIndexerModel();
        var values = new List<int>();

        using var subscription = model.WhenAnyValue(value => value[(short)2], isDistinct: false)
            .Subscribe(values.Add);
        model.Raise("Item[]");
        model.Raise("Entry[]");

        Assert.Equal([202, 202], values);
    }

    [Fact]
    public void DirectIndexerHonorsDistinctnessAndDisposal()
    {
        var model = new IndexerModel();
        var distinctValues = new List<int>();
        var allValues = new List<int>();

        var distinct = model.WhenAnyValue(value => value[0]).Subscribe(distinctValues.Add);
        var all = model.WhenAnyValue(value => value[0], isDistinct: false).Subscribe(allValues.Add);
        model.Set(0, 0);
        distinct.Dispose();
        all.Dispose();
        model.Set(0, 1);

        Assert.Equal([0], distinctValues);
        Assert.Equal([0, 0], allValues);
        Assert.Equal(0, model.SubscriberCount);
    }

    [Fact]
    public void DirectIndexerCacheIsThreadSafeAcrossAlternatingConstants()
    {
        var failures = 0;

        Parallel.For(0, 1_000, index =>
        {
            var model = new IndexerModel();
            var observed = -1;
            var expected = index % 2 == 0 ? 0 : 10;

            using var subscription = index % 2 == 0
                ? model.WhenAnyValue(value => value[0]).Subscribe(value => observed = value)
                : model.WhenAnyValue(value => value[1]).Subscribe(value => observed = value);

            if (observed != expected)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Equal(0, failures);
    }

    [Fact]
    public void ConstantObjectIndexCacheUsesReferenceIdentity()
    {
        var model = new IndexerModel();
        var firstKey = new object();
        var secondKey = new object();
        var firstExpression = CreateObjectIndexerExpression(firstKey);
        var secondExpression = CreateObjectIndexerExpression(secondKey);
        var values = new List<int>();

        using var first = model.WhenAnyValue(firstExpression).Subscribe(values.Add);
        using var second = model.WhenAnyValue(secondExpression).Subscribe(values.Add);
        using var cachedFirst = model.WhenAnyValue(firstExpression).Subscribe(values.Add);

        Assert.Equal(
            [
                RuntimeHelpers.GetHashCode(firstKey),
                RuntimeHelpers.GetHashCode(secondKey),
                RuntimeHelpers.GetHashCode(firstKey),
            ],
            values);
    }

    [Fact]
    public void ConstantObjectIndexCacheDistinguishesNullAndRuntimeTypes()
    {
        var model = new IndexerModel();
        object stringKey = "1";
        object integerKey = 1;
        var stringExpression = CreateObjectIndexerExpression(stringKey);
        var integerExpression = CreateObjectIndexerExpression(integerKey);
        var nullExpression = CreateObjectIndexerExpression(null);
        var values = new List<int>();

        using var stringSubscription = model.WhenAnyValue(stringExpression).Subscribe(values.Add);
        using var integerSubscription = model.WhenAnyValue(integerExpression).Subscribe(values.Add);
        using var nullSubscription = model.WhenAnyValue(nullExpression).Subscribe(values.Add);

        Assert.Equal(
            [
                RuntimeHelpers.GetHashCode(stringKey),
                RuntimeHelpers.GetHashCode(integerKey),
                -1,
            ],
            values);
    }

    [Fact]
    public void GeneralFourSegmentIndexerPathObservesLeaf()
    {
        var leaf = new IndexerModel();
        var level2 = new IndexerModel { Child = leaf };
        var level1 = new IndexerModel { Child = level2 };
        var model = new IndexerModel { Child = level1 };
        var values = new List<int>();

        using var subscription = model
            .WhenAnyValue(value => value.Child!.Child!.Child![0])
            .Subscribe(values.Add);
        leaf.Set(0, 9);
        var replacement = new IndexerModel
        {
            Child = new IndexerModel
            {
                Child = new IndexerModel
                {
                    Child = new IndexerModel(),
                },
            },
        };
        model.Child = replacement.Child;
        replacement.Child!.Child!.Child!.Set(0, 8);

        Assert.Equal([0, 9, 0, 8], values);
    }

    [Fact]
    public void GeneralFourSegmentFieldPathReadsInitialValue()
    {
        var model = new FieldChainModel
        {
            Child = new FieldChainModel
            {
                Child = new FieldChainModel
                {
                    Child = new FieldChainModel { Value = 42 },
                },
            },
        };
        var values = new List<int>();

        using var subscription = model
            .WhenAnyValue(value => value.Child!.Child!.Child!.Value)
            .Subscribe(values.Add);
        model.Child!.Child!.Child!.Value = 43;
        model.Child.Child.Child.Raise(nameof(FieldChainModel.Value));

        Assert.Equal([42, 43], values);
    }

    private static Expression<Func<IndexerModel, int>> CreateObjectIndexerExpression(object? key)
    {
        var parameter = Expression.Parameter(typeof(IndexerModel), "value");
        var property = typeof(IndexerModel).GetProperty("Item", [typeof(object)])!;
        return Expression.Lambda<Func<IndexerModel, int>>(
            Expression.MakeIndex(parameter, property, [Expression.Constant(key, typeof(object))]),
            parameter);
    }
}
