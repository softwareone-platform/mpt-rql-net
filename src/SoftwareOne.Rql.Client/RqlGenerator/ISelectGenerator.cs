using SoftwareOne.Rql.Client.Builder.Select;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public interface ISelectGenerator
{
    string Generate(SelectFields selectFields);
}