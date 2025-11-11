using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAny), typeof(Any))]
public interface IAny : ICollectionOperator, IActualOperator { }