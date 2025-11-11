using Mpt.Rql.Services.Filtering.Operators.Search;

namespace Mpt.Rql.Services.Filtering.Operators.Search.Implementation;

internal class LikeInsensitive : Like, ILikeCaseInsensitive
{
    protected override bool IsInsensitive => true;
}
