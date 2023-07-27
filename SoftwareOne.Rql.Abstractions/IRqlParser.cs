using SoftwareOne.Rql.Abstractions.Group;

namespace SoftwareOne.Rql.Abstractions
{
    public interface IRqlParser
    {
        RqlGroup Parse(string expression);
    }
}
