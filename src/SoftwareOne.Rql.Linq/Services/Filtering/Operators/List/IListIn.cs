using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;

[Expression(typeof(RqlListIn), typeof(ListIn))]
public interface IListIn : IListOperator, IActualOperator { }