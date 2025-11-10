namespace Mpt.Rql.Linq.Core;

internal interface IActionValidator
{
    bool Validate(RqlPropertyInfo propertyInfo, RqlActions action);
}
