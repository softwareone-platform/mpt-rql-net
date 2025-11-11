using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.List.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.List;

[Expression(typeof(RqlListOut), typeof(ListOut))]
public interface IListOut : IListOperator, IActualOperator { }