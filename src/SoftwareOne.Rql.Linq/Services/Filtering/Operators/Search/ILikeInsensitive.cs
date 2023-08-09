using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;

[Expression(typeof(RqlLikeInsensitive), typeof(LikeInsensitive))]
public interface ILikeInsensitive : ILike { }