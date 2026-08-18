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

    /// <summary>Observes two typed paths through an explicit adapter and projects their values.</summary>
    public static IObservable<TResult> WhenAnyValue<TSource, T1, T2, TResult>(
        this TSource source,
        PropertyPath<TSource, T1> property1,
        PropertyPath<TSource, T2> property2,
        Func<T1, T2, TResult> selector,
        IPropertyNotificationAdapter notificationAdapter,
        bool isDistinct = true,
        IEqualityComparer<TResult>? comparer = null)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(property1);
        ArgumentNullException.ThrowIfNull(property2);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(notificationAdapter);

        return new CombineLatestObservable<T1, T2, TResult>(
            source.WhenAnyValue(property1, notificationAdapter, isDistinct),
            source.WhenAnyValue(property2, notificationAdapter, isDistinct),
            selector,
            isDistinct,
            comparer ?? EqualityComparer<TResult>.Default);
    }

    /// <summary>ReactiveUI-compatible two-property expression overload.</summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected metadata. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<TResult> WhenAnyValue<TSource, TResult, T1, T2>(
        this TSource source,
        Expression<Func<TSource, T1>> property1,
        Expression<Func<TSource, T2>> property2,
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
            source.WhenAnyValue(ExpressionPropertyPath.Create(property1), isDistinct),
            source.WhenAnyValue(ExpressionPropertyPath.Create(property2), isDistinct),
            selector,
            isDistinct && comparer is not null,
            comparer ?? EqualityComparer<TResult>.Default);
    }

    /// <summary>ReactiveUI-compatible two-property string-name projection overload.</summary>
    [RequiresUnreferencedCode("String property compatibility resolves public property metadata by name. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<TResult> WhenAnyValue<TSource, TResult, T1, T2>(
        this TSource source,
        string property1Name,
        string property2Name,
        Func<T1, T2, TResult> selector,
        bool isDistinct = true)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return new CombineLatestObservable<T1, T2, TResult>(
            source.WhenAnyValue(
                StringPropertyPath.Create<TSource, T1>(source, property1Name),
                isDistinct),
            source.WhenAnyValue(
                StringPropertyPath.Create<TSource, T2>(source, property2Name),
                isDistinct),
            selector,
            false,
            EqualityComparer<TResult>.Default);
    }

    /// <summary>Observes two expression paths through an explicit adapter.</summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected metadata. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<TResult> WhenAnyValue<TSource, TResult, T1, T2>(
        this TSource source,
        Expression<Func<TSource, T1>> property1,
        Expression<Func<TSource, T2>> property2,
        Func<T1, T2, TResult> selector,
        IPropertyNotificationAdapter notificationAdapter,
        bool isDistinct = true,
        IEqualityComparer<TResult>? comparer = null)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(property1);
        ArgumentNullException.ThrowIfNull(property2);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(notificationAdapter);

        return new CombineLatestObservable<T1, T2, TResult>(
            source.WhenAnyValue(
                ExpressionPropertyPath.Create(property1),
                notificationAdapter,
                isDistinct),
            source.WhenAnyValue(
                ExpressionPropertyPath.Create(property2),
                notificationAdapter,
                isDistinct),
            selector,
            isDistinct && comparer is not null,
            comparer ?? EqualityComparer<TResult>.Default);
    }

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

    /// <summary>ReactiveUI-compatible two-property string-name tuple overload.</summary>
    [RequiresUnreferencedCode("String property compatibility resolves public property metadata by name. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<(T1 Value1, T2 Value2)> WhenAnyValue<TSource, T1, T2>(
        this TSource source,
        string property1Name,
        string property2Name,
        bool isDistinct = true)
        where TSource : class =>
        source.WhenAnyValue(
            StringPropertyPath.Create<TSource, T1>(source, property1Name),
            StringPropertyPath.Create<TSource, T2>(source, property2Name),
            static (value1, value2) => (value1, value2),
            isDistinct);

    /// <summary>Observes two typed paths through an explicit adapter and emits tuples.</summary>
    public static IObservable<(T1 Value1, T2 Value2)> WhenAnyValue<TSource, T1, T2>(
        this TSource source,
        PropertyPath<TSource, T1> property1,
        PropertyPath<TSource, T2> property2,
        IPropertyNotificationAdapter notificationAdapter,
        bool isDistinct = true) where TSource : class =>
        source.WhenAnyValue(
            property1,
            property2,
            static (value1, value2) => (value1, value2),
            notificationAdapter,
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

    /// <summary>Observes two expression paths through an explicit adapter and emits tuples.</summary>
    [RequiresUnreferencedCode("Expression compatibility uses reflected metadata. Use typed PropertyPath arguments for trimming and NativeAOT.")]
    public static IObservable<(T1 Value1, T2 Value2)> WhenAnyValue<TSource, T1, T2>(
        this TSource source,
        Expression<Func<TSource, T1>> property1,
        Expression<Func<TSource, T2>> property2,
        IPropertyNotificationAdapter notificationAdapter,
        bool isDistinct = true) where TSource : class =>
        source.WhenAnyValue(
            ExpressionPropertyPath.Create(property1),
            ExpressionPropertyPath.Create(property2),
            static (value1, value2) => (value1, value2),
            notificationAdapter,
            isDistinct);
}
