using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

internal record SelectFields(IList<ISelectDefinition>? Included, IList<ISelectDefinition>? Excluded);