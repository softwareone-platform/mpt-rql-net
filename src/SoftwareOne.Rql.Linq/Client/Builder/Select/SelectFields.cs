namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

public record SelectFields(IList<ISelect> Included, IList<ISelect> Excluded);