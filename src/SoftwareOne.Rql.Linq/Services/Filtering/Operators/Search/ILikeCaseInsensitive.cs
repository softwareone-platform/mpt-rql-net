using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLikeCaseInsensitive), typeof(LikeInsensitive))]
public interface ILikeCaseInsensitive : ILike { }