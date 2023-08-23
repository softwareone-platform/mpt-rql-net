using System.Linq.Expressions;

namespace SoftwareOne.Rql.Client.Builder.Dsl;

public record In<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : MultiComparableOperator<T, U>(Exp, Values) where T : class;