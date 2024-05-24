using SoftwareOne.Rql.Linq.Core.Result;

namespace SoftwareOne.Rql.Linq.Services.Filtering
{
    internal static class FilteringError
    {
        public static Error Internal { get; } = Error.General("Internal filtering error occurred. Please contact RQL package maintainer.", "internal");
    }
}
