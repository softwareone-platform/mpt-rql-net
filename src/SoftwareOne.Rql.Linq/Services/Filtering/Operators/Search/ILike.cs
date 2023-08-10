using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLike), typeof(Like))]
public interface ILike : ISearchOperator { }