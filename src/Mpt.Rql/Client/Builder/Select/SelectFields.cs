using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Builder.Select;

internal record SelectFields(IList<ISelectDefinition>? Included, IList<ISelectDefinition>? Excluded);