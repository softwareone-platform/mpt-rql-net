#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public record SelectFields(IList<ISelect> Included, IList<ISelect> Excluded);