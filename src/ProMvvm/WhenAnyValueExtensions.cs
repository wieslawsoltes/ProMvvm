using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ProMvvm;

/// <summary>High-performance observation extensions for <see cref="System.ComponentModel.INotifyPropertyChanged"/> models.</summary>
public static class WhenAnyValueExtensions
{
    /// <summary>Observes an AOT-safe typed property path.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<TValue> WhenAnyValue<TSource, TValue>(
        this TSource source,
        PropertyPath<TSource, TValue> path,
        bool isDistinct = true,
        IEqualityComparer<TValue>? comparer = null)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(path);

        return path.TryGetSingle(out var propertyName, out var getter)
            ? new SinglePropertyObservable<TSource, TValue>(
                source,
                propertyName,
                getter,
                isDistinct,
                comparer ?? EqualityComparer<TValue>.Default)
            : new PropertyPathObservable<TSource, TValue>(
            source,
            path,
            isDistinct,
            comparer ?? EqualityComparer<TValue>.Default);
    }

    /// <summary>
    /// Observes one property using an ahead-of-time compiled getter. This overload is
    /// reflection-free and safe for trimming and NativeAOT.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IObservable<TValue> WhenAnyValue<TSource, TValue>(
        this TSource source,
        Func<TSource, TValue> getter,
        string propertyName,
        bool isDistinct = true,
        IEqualityComparer<TValue>? comparer = null)
        where TSource : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(source);

        return new SinglePropertyObservable<TSource, TValue>(
            source,
            propertyName,
            getter,
            isDistinct,
            comparer ?? EqualityComparer<TValue>.Default);
    }

    /// <summary>
    /// ReactiveUI-compatible expression overload. Prefer the typed path or getter overload
    /// in trimmed applications and NativeAOT builds.
    /// </summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected property or field metadata. Use the PropertyPath or getter overload for trim-safe and NativeAOT-safe observation.")]
    public static IObservable<TValue> WhenAnyValue<TSource, TValue>(
        this TSource source,
        Expression<Func<TSource, TValue>> property,
        bool isDistinct = true,
        IEqualityComparer<TValue>? comparer = null)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.WhenAnyValue(
            ExpressionPropertyPath.Create(property),
            isDistinct,
            comparer);
    }
}
