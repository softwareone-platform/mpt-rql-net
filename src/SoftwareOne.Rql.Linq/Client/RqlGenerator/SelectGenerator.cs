using SoftwareOne.Rql.Linq.Client;

namespace SoftwareOne.Rql.Client;

internal class SelectGenerator : ISelectGenerator
{
    public string Generate(SelectFields selectFields)
    {
        var included = selectFields.Included.Select(x => x.ToQuery()).ToList();
        var excluded = selectFields.Excluded.Select(x => $"-{x.ToQuery()}").ToList();

        var all = included.Union(excluded).ToList();
        return all.Any() ? $"select={string.Join(',', all)}" : string.Empty;
    }
}