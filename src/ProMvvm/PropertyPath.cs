using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProMvvm;

/// <summary>
/// Describes an AOT-safe chain of observable properties. The path stores normal,
/// ahead-of-time compiled delegates and never compiles an expression or reflects over a model.
/// </summary>
/// <typeparam name="TSource">The root model type.</typeparam>
/// <typeparam name="TValue">The final property value type.</typeparam>
public sealed class PropertyPath<TSource, TValue>
    where TSource : class
{
    private readonly ImmutableArray<IPropertyPathSegment> _segments;
    private readonly string? _singlePropertyName;
    private readonly Func<TSource, TValue>? _singleGetter;
    private readonly IPropertyPathObservableFactory<TSource, TValue>? _specializedFactory;

    private PropertyPath(
        ImmutableArray<IPropertyPathSegment> segments,
        string? singlePropertyName = null,
        Func<TSource, TValue>? singleGetter = null,
        IPropertyPathObservableFactory<TSource, TValue>? specializedFactory = null)
    {
        _segments = segments;
        _singlePropertyName = singlePropertyName;
        _singleGetter = singleGetter;
        _specializedFactory = specializedFactory;
    }

    internal ImmutableArray<IPropertyPathSegment> Segments => _segments;

    internal static PropertyPath<TSource, TValue> FromSegments(
        ImmutableArray<IPropertyPathSegment> segments) => new(segments);

    internal static PropertyPath<TSource, TValue> FromSingle(
        string propertyName,
        Func<TSource, TValue> getter) => new(
        [new PropertyPathSegment<TSource, TValue>(propertyName, getter)],
        propertyName,
        getter);

    internal static PropertyPath<TSource, TValue> FromTwo<TIntermediate>(
        string propertyName1,
        Func<TSource, TIntermediate> getter1,
        string propertyName2,
        Func<TIntermediate, TValue> getter2) => new(
        [
            new PropertyPathSegment<TSource, TIntermediate>(propertyName1, getter1),
            new PropertyPathSegment<TIntermediate, TValue>(propertyName2, getter2),
        ],
        specializedFactory: new TwoSegmentObservableFactory<TSource, TIntermediate, TValue>(
            propertyName1,
            getter1,
            propertyName2,
            getter2));

    internal bool TryGetSingle(
        [NotNullWhen(true)] out string? propertyName,
        [NotNullWhen(true)] out Func<TSource, TValue>? getter)
    {
        propertyName = _singlePropertyName;
        getter = _singleGetter;
        return getter is not null;
    }

    internal bool TryCreateSpecializedObservable(
        TSource source,
        bool isDistinct,
        IEqualityComparer<TValue> comparer,
        [NotNullWhen(true)] out IObservable<TValue>? observable)
    {
        observable = _specializedFactory?.Create(source, isDistinct, comparer);
        return observable is not null;
    }

    /// <summary>Appends a child property to this path.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PropertyPath<TSource, TNext> Then<TNext>(
        string propertyName,
        Func<TValue, TNext> getter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(getter);

        if (TryGetSingle(out var rootPropertyName, out var rootGetter))
        {
            return PropertyPath<TSource, TNext>.FromTwo(
                rootPropertyName,
                rootGetter,
                propertyName,
                getter);
        }

        return new PropertyPath<TSource, TNext>(
            _segments.Add(new PropertyPathSegment<TValue, TNext>(propertyName, getter)));
    }
}

internal interface IPropertyPathObservableFactory<TSource, TValue>
    where TSource : class
{
    IObservable<TValue> Create(
        TSource source,
        bool isDistinct,
        IEqualityComparer<TValue> comparer);
}

internal sealed class TwoSegmentObservableFactory<TSource, TIntermediate, TValue>(
    string propertyName1,
    Func<TSource, TIntermediate> getter1,
    string propertyName2,
    Func<TIntermediate, TValue> getter2) : IPropertyPathObservableFactory<TSource, TValue>
    where TSource : class
{
    public IObservable<TValue> Create(
        TSource source,
        bool isDistinct,
        IEqualityComparer<TValue> comparer) => new TwoSegmentPropertyObservable<TSource, TIntermediate, TValue>(
        source,
        propertyName1,
        getter1,
        propertyName2,
        getter2,
        isDistinct,
        comparer);
}

internal interface IPropertyPathSegment
{
    string PropertyName { get; }

    object? GetValue(object instance);
}

internal sealed class PropertyPathSegment<TSource, TValue>(
    string propertyName,
    Func<TSource, TValue> getter) : IPropertyPathSegment
{
    public string PropertyName { get; } = propertyName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? GetValue(object instance) => getter((TSource)instance);
}
