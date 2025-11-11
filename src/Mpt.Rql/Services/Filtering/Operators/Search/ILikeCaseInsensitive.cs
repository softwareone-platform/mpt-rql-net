using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Services.Filtering.Operators.Search.Implementation;

namespace Mpt.Rql.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLikeCaseInsensitive), typeof(LikeInsensitive))]
public interface ILikeCaseInsensitive : ILike { }