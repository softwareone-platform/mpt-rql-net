using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAll), typeof(All))]
public interface IAll : ICollectionOperator, IActualOperator { }