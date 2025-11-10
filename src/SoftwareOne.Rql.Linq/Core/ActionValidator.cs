using SoftwareOne.Rql.Abstractions.Exception;

namespace SoftwareOne.Rql.Linq.Core;

internal class ActionValidator(IExternalServiceAccessor externalServices) : IActionValidator
{
    public bool Validate(RqlPropertyInfo propertyInfo, RqlActions action)
    {
        if (propertyInfo.ActionStrategy != null)
        {
            return externalServices.GetService(propertyInfo.ActionStrategy) is not IActionStrategy strategy
                ? throw new RqlInvalidActionStrategyException(
                    $"The instance of type {propertyInfo.ActionStrategy.FullName} defined as action strategy for property " +
                    $"({propertyInfo.Property!.DeclaringType!.FullName}).{propertyInfo.Property.Name} " +
                    $"cannot be found. Make sure that service has been registered.")
                : strategy.IsAllowed(action);
        }

        return propertyInfo.Actions.HasFlag(action);
    }
}
