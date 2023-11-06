using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Filtering
{
    internal static class FilteringError
    {
        public static Error Internal { get; } = Error.Failure("internal", "Internal filtering error occurred. Please contact RQL package maintainer.");
    }
}
