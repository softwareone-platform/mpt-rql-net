using System.Linq.Expressions;

namespace Mpt.Rql.Client.Builder.Operators;

internal record In<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : MultiComparableOperator<T, U>(Exp, Values) where T : class;