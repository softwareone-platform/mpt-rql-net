using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLikeCaseInsensitive), typeof(LikeInsensitive))]
public interface ILikeCaseInsensitive : ILike { }