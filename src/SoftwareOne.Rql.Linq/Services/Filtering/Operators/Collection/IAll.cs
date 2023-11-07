using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAll), typeof(All))]
public interface IAll : ICollectionOperator, IActualOperator { }