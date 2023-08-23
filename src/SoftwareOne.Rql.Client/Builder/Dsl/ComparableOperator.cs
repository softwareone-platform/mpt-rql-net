using System.Linq.Expressions;

namespace SoftwareOne.Rql.Client.Builder.Dsl;

public abstract record ComparableOperator<T, U>(Expression<Func<T, U>> Exp, U Value) : Operator, IComparableOperator where T : class
{
    public QueryOperator ToQueryOperator()
    {
        var property = new PropertyVisitor().GetPath(Exp.Body);
        return new QueryOperator(property, ValueConverter.Convert(Value));
    }
}