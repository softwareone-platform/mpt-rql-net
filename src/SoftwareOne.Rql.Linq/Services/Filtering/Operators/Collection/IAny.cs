using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;

[Expression(typeof(RqlAny), typeof(Any))]
public interface IAny : ICollectionOperator, IActualOperator { }