using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Linq.Services.Filtering.Operators.List.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.List;

[Expression(typeof(RqlListIn), typeof(ListIn))]
public interface IListIn : IListOperator, IActualOperator { }