using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAny), typeof(Any))]
public interface IAny : ICollectionOperator, IActualOperator { }