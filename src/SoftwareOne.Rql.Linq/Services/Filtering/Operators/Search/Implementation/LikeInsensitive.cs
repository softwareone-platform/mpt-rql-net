using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation
{
    internal class LikeInsensitive : Like, ILikeInsensitive
    {
        protected override bool IsInsensitive => true;
    }
}
