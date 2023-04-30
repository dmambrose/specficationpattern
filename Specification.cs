
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public class Specification<T>
{
    private static readonly ConcurrentDictionary<Expression<Func<T, bool>>, Func<T, bool>> _compiledCache =
        new ConcurrentDictionary<Expression<Func<T, bool>>, Func<T, bool>>();

    private readonly Expression<Func<T, bool>> _predicate;

    public string Description { get; }

    public Specification(Expression<Func<T, bool>> predicate, string description)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Description = description;
    }

    public Result<Unit> Evaluate(T item)
    {
        var compiledPredicate = _compiledCache.GetOrAdd(_predicate, expr => expr.Compile());
        if (compiledPredicate(item))
        {
            return Result<Unit>.Success(Unit.Default);
        }

        return Result<Unit>.Fail(new  Exception());
    }

    public List<Result<Unit, string>> EvaluateAll(IEnumerable<T> items)
    {
        return items.Select(Evaluate).ToList();
    }

    public IQueryable<T> Apply(IQueryable<T> query)
    {
        return query.Where(_predicate);
    }

    public Specification<T> And(Specification<T> other)
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(_predicate.Body, other._predicate.Body),
                _predicate.Parameters),
            $"{Description} and {other.Description}"
        );
    }

    public Specification<T> Or(Specification<T> other)
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(_predicate.Body, other._predicate.Body),
                _predicate.Parameters),
            $"{Description} or {other.Description}"
        );
    }

    public Specification<T> Not()
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.Not(_predicate.Body),
                _predicate.Parameters),
            $"Not {Description}"
        );
    }

    public Specification<T> Xor(Specification<T> other)
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.ExclusiveOr(_predicate.Body, other._predicate.Body),
                _predicate.Parameters),
            $"{Description} xor {other.Description}"
        );
    }

    public Specification<T> Nor(Specification<T> other)
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.Not(Expression.OrElse(_predicate.Body, other._predicate.Body)),
                _predicate.Parameters),
            $"{Description} nor {other.Description}"
        );
    }

    public Specification<T> Nand(Specification<T> other)
    {
        return new Specification<T>(
            Expression.Lambda<Func<T, bool>>(
                Expression.Not(Expression.AndAlso(_predicate.Body, other._predicate.Body)),
                _predicate.Parameters),
            $"{Description} nand {other.Description}"
        );
    }

    // Operator overloads
    public static Specification<T> operator &(Specification<T> spec1, Specification<T> spec2) => spec1.And(spec2);
    public static Specification<T> operator |(Specification<T> spec1, Specification<T> spec2) => spec1.Or(spec2);
    public static Specification<T> operator !(Specification<T> spec) => spec.Not();
    public static Specification<T> operator ^(Specification<T> spec1, Specification<T> spec2) => spec1.Xor(spec2);
}

