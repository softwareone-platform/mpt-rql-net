using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Search.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLike), typeof(Like))]
public interface ILike : ISearchOperator, IActualOperator { }