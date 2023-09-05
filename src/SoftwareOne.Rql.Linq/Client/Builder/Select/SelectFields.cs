using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Select;

internal record SelectFields(IList<ISelect>? Included, IList<ISelect>? Excluded);