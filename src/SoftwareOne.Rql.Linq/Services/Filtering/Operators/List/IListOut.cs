using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;

[Expression(typeof(RqlListOut), typeof(ListOut))]
public interface IListOut : IListOperator, IActualOperator { }