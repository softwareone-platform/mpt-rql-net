using Mpt.Rql.Abstractions.Group;

namespace Mpt.Rql.Abstractions;

public interface IRqlParser
{
    RqlGroup Parse(string expression);
}
