using SoftwareOne.Rql.Linq.Client.Builder.Select;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public interface ISelectGenerator
{
    string Generate(SelectFields selectFields);
}