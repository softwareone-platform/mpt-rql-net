using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;

[Expression(typeof(RqlNot), typeof(Not))]
public interface INot : IUnaryOperator { }