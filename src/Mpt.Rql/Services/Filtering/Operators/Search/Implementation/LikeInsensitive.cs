namespace Mpt.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

internal class LikeInsensitive : Like, ILikeCaseInsensitive
{
    protected override bool IsInsensitive => true;
}
