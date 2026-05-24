using System.Linq.Expressions;

namespace LoanSuperMarket.Application.Common.Specifications;

/// <summary>
/// Generic specification pattern for building composable query predicates.
/// Enables reusable, testable query logic without coupling to EF Core.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }

    List<Expression<Func<T, object>>> Includes { get; }

    Expression<Func<T, object>>? OrderBy { get; }

    Expression<Func<T, object>>? OrderByDescending { get; }

    int? Take { get; }

    int? Skip { get; }
}

/// <summary>
/// Base implementation of the specification pattern.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; protected set; } = _ => true;

    public List<Expression<Func<T, object>>> Includes { get; } = [];

    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public int? Take { get; protected set; }

    public int? Skip { get; protected set; }

    protected void AddInclude(Expression<Func<T, object>> include)
    {
        Includes.Add(include);
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}
