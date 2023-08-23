namespace SoftwareOne.Rql.Client.Builder.Select;

public record SelectFields(IList<ISelect> Included, IList<ISelect> Excluded);