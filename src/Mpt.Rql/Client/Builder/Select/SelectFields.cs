namespace Mpt.Rql.Client.Builder.Select;

internal record SelectFields(IList<ISelectDefinition>? Included, IList<ISelectDefinition>? Excluded);