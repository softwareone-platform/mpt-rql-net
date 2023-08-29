using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal record Le<T, U>(Expression<Func<T, U>> Exp, U Value) : ComparableOperator<T, U>(Exp, Value) where T : class;