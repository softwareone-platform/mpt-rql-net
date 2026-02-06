using Mpt.Rql.Client.Builder.Select;

namespace Mpt.Rql.Client.Generator;

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

        if (concat.Count == 0)
            return default;

        return string.Join(',', concat);
    }

    private IEnumerable<string> ProcessList(IList<ISelectDefinition>? input, bool isExclude)
    {
        if (input == null)
            yield break;

        foreach (var item in input)
        {
            var path = item.ToQuery(_propertyVisitor);

            if (isExclude)
                path = $"-{path}";

            yield return path;
        }
    }
}