using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ProMvvm;

/// <summary>Multi-property observation extensions.</summary>
public static partial class WhenAnyValueMultiExtensions
{
    /// <summary>Observes two AOT-safe paths and projects their latest values.</summary>
    public static IObservable<TResult> WhenAnyValue<TSource, T1, T2, TResult>(
        this TSource source,
        PropertyPath<TSource, T1> property1,
        PropertyPath<TSource, T2> property2,
        Func<T1, T2, TResult> selector,
        bool isDistinct = true,
        IEqualityComparer<TResult>? comparer = null)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(property1);
        ArgumentNullException.ThrowIfNull(property2);
        ArgumentNullException.ThrowIfNull(selector);

        return new CombineLatestObservable<T1, T2, TResult>(
            source.WhenAnyValue(property1, isDistinct),
            source.WhenAnyValue(property2, isDistinct),
            selector,
            isDistinct,
            comparer ?? EqualityComparer<TResult>.Default);
    }

    /// <summary>ReactiveUI-compatible two-property expression overload.</summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected metadata. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<TResult> WhenAnyValue<TSource, T1, T2, TResult>(
        this TSource source,
        Expression<Func<TSource, T1>> property1,
        Expression<Func<TSource, T2>> property2,
        Func<T1, T2, TResult> selector,
        bool isDistinct = true,
        IEqualityComparer<TResult>? comparer = null)
        where TSource : class =>
        source.WhenAnyValue(
            ExpressionPropertyPath.Create(property1),
            ExpressionPropertyPath.Create(property2),
            selector,
            isDistinct,
            comparer);

    /// <summary>Observes two AOT-safe paths and emits tuples.</summary>
    public static IObservable<(T1 Value1, T2 Value2)> WhenAnyValue<TSource, T1, T2>(
        this TSource source,
        PropertyPath<TSource, T1> property1,
        PropertyPath<TSource, T2> property2,
        bool isDistinct = true) where TSource : class =>
        source.WhenAnyValue(
            property1,
            property2,
            static (value1, value2) => (value1, value2),
            isDistinct);

    /// <summary>ReactiveUI-compatible two-property tuple expression overload.</summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected metadata. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<(T1 Value1, T2 Value2)> WhenAnyValue<TSource, T1, T2>(
        this TSource source,
        Expression<Func<TSource, T1>> property1,
        Expression<Func<TSource, T2>> property2,
        bool isDistinct = true) where TSource : class =>
        source.WhenAnyValue(
            ExpressionPropertyPath.Create(property1),
            ExpressionPropertyPath.Create(property2),
            static (value1, value2) => (value1, value2),
            isDistinct);
}
