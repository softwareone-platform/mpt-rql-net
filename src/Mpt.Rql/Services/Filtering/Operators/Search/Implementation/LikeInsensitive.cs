using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Services.Filtering.Operators.Search.Implementation;

internal class LikeInsensitive(IRqlSettings settings) : Like(settings), ILikeCaseInsensitive { }
