
using SoftwareOne.Rql.Abstractions.Exception;

namespace SoftwareOne.Rql.Linq.Core
{
    internal class ActionValidator : IActionValidator
    {
        private readonly IServiceProvider _serviceProvider;

        public ActionValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool Validate(RqlPropertyInfo propertyInfo, RqlActions action)
        {
            if (propertyInfo.ActionStrategy != null)
            {
                return _serviceProvider.GetService(propertyInfo.ActionStrategy) is not IActionStrategy strategy
                    ? throw new RqlInvalidActionStrategyException(
                        $"The instance of type {propertyInfo.ActionStrategy.FullName} defined as action strategy for property " +
                        $"({propertyInfo.Property!.DeclaringType!.FullName}).{propertyInfo.Property.Name} " +
                        $"cannot be found. Make sure that service has been registered.")
                    : strategy.IsAllowed(action);
            }

            return propertyInfo.Actions.HasFlag(action);
        }
    }
}
