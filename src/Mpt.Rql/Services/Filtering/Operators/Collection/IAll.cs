using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAll), typeof(All))]
public interface IAll : ICollectionOperator, IActualOperator { }