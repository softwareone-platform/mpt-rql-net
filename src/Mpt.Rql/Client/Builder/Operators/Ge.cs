using System.Linq.Expressions;

namespace Mpt.Rql.Client.Builder.Operators;

internal record Ge<T, U>(Expression<Func<T, U>> Exp, U Value) : ComparableOperator<T, U>(Exp, Value) where T : class;