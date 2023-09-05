using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Select;

namespace SoftwareOne.Rql.Linq.Client;

internal class SelectGenerator : ISelectGenerator
{
    private readonly IPropertyVisitor _propertyVisitor;

    public SelectGenerator(IPropertyVisitor propertyVisitor)
    {
        _propertyVisitor = propertyVisitor;
    }
    public string? Generate(ISelectDefinitionProvider? select)
    {
        if (select == null)
            return default;

        var definition = select.GetDefinition();

        var concat = ProcessList(definition.Included, false).Concat(ProcessList(definition.Excluded, true)).ToList();

        if (!concat.Any())
            return default;

        return string.Join(',', concat);
    }

    private IEnumerable<string> ProcessList(IList<ISelect>? input, bool isExclude)
    {
        if (input == null)
            yield break;

        foreach (var item in input)
        {
            if (item is not IInternalSelect internalItem)
                throw new InvalidDefinitionException("Only for internal usage available");

            var path = internalItem.ToQuery(_propertyVisitor);

            if (isExclude)
                path = $"-{path}";

            yield return path;
        }
    }
}