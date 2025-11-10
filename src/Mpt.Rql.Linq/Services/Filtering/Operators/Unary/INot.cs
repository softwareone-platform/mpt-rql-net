using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Linq.Services.Filtering.Operators.Unary.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Unary;

[Expression(typeof(RqlNot), typeof(Not))]
public interface INot : IUnaryOperator { }