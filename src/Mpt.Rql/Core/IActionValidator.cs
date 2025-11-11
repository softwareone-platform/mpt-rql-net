namespace Mpt.Rql.Core;

internal interface IActionValidator
{
    bool Validate(RqlPropertyInfo propertyInfo, RqlActions action);
}
