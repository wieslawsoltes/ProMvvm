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

    private PropertyPath(
        ImmutableArray<IPropertyPathSegment> segments,
        string? singlePropertyName = null,
        Func<TSource, TValue>? singleGetter = null)
    {
        _segments = segments;
        _singlePropertyName = singlePropertyName;
        _singleGetter = singleGetter;
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

    internal bool TryGetSingle(
        [NotNullWhen(true)] out string? propertyName,
        [NotNullWhen(true)] out Func<TSource, TValue>? getter)
    {
        propertyName = _singlePropertyName;
        getter = _singleGetter;
        return getter is not null;
    }

    /// <summary>Appends a child property to this path.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PropertyPath<TSource, TNext> Then<TNext>(
        string propertyName,
        Func<TValue, TNext> getter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(getter);

        return new PropertyPath<TSource, TNext>(
            _segments.Add(new PropertyPathSegment<TValue, TNext>(propertyName, getter)));
    }
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
