using System.Linq.Expressions;

namespace SoftwareOne.Rql.Client.Builder.Dsl;

public record Gt<T, U>(Expression<Func<T, U>> Exp, U Value) : ComparableOperator<T, U>(Exp, Value) where T : class;